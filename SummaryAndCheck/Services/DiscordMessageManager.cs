// DiscordMessageManager.cs
using Discord.Rest;
using Discord.WebSocket;
using System.Text;
using System.Threading.Channels;

namespace SummaryAndCheck.Services
{
    public class DiscordMessageManager
    {
        private const int DiscordTextLimit = 1950; // Discord's character limit for a single message, with some buffer.

        private readonly SocketCommandBase _socketCommand;
        private readonly CancellationToken _stoppingToken;
        private readonly Channel<string> _textChannel; // Using a Channel for thread-safe producer-consumer pattern.

        private Task? _sendingTask;
        private bool _isFinished = false;
        Func<string, Task> updater;
        public async Task DirectWriteAsync(string message)
        {
            if (updater != null)
            {
                await updater(message);
            }
        }
        public DiscordMessageManager(SocketCommandBase socketCommand, CancellationToken stoppingToken)
        {
            _socketCommand = socketCommand;
            _stoppingToken = stoppingToken;
            // Create an unbounded channel, allowing producers to write text freely.
            _textChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true, // Optimization since we only have one consumer task.
                SingleWriter = false // Can be called from multiple threads.
            });
            updater = async (message) =>
            {
                await _socketCommand.ModifyOriginalResponseAsync(prop => prop.Content = message);
            };
        }

        /// <summary>
        /// Appends text to the message buffer. This method is thread-safe.
        /// </summary>
        /// <param name="text">The text to append.</param>
        public void AppendToMessage(string text)
        {
            if (_isFinished)
            {
                // Don't accept new text if the manager has been marked as finished.
                return;
            }
            // TryWrite is non-blocking and thread-safe.
            _textChannel.Writer.TryWrite(text);
        }

        /// <summary>
        /// Starts the background task that processes the message queue.
        /// </summary>
        public void Start()
        {
            // Ensure the sending task is only started once.
            _sendingTask ??= Task.Run(ConsumeAndSendMessagesAsync, _stoppingToken);
        }

        /// <summary>
        /// Signals that no more text will be appended and waits for the final message to be sent.
        /// </summary>
        public async Task FinishAsync()
        {
            if (!_isFinished)
            {
                _isFinished = true;
                // Signal the channel that no more items will be written.
                // This allows the consumer loop to complete gracefully.
                _textChannel.Writer.Complete();
            }

            // Wait for the consumer task to process all remaining messages and finish.
            if (_sendingTask != null)
            {
                await _sendingTask;
            }
        }

        /// <summary>
        /// The core consumer loop that reads from the channel and sends messages to Discord.
        /// </summary>
        private async Task ConsumeAndSendMessagesAsync()
        {
            // These variables are local to the consumer task, avoiding concurrency issues.
            var messageBuffer = new StringBuilder();
            RestFollowupMessage? currentFollowupMessage = null;

            // This function will be used to update the original response or a followup message.


            // Asynchronously wait for data to become available in the channel.
            // The loop will exit gracefully when the channel is marked as complete and empty.
            while (await _textChannel.Reader.WaitToReadAsync(_stoppingToken))
            {
                // Batch read all available text from the channel to reduce UI updates.
                while (_textChannel.Reader.TryRead(out var text))
                {
                    messageBuffer.Append(text);
                }

                // If there's content in the buffer, process and send it.
                if (messageBuffer.Length > 0)
                {
                    // If the buffer exceeds the Discord limit, send a full chunk.
                    if (messageBuffer.Length >= DiscordTextLimit)
                    {
                        string chunkToSend = GetChunk(messageBuffer);

                        // The updater sends the chunk. For the first chunk, it modifies the original response.
                        await updater(chunkToSend);

                        // After the first message, subsequent updates must be new followup messages.
                        currentFollowupMessage = null; // Reset to create a new followup.
                        updater = async (message) =>
                        {
                            if (currentFollowupMessage == null)
                            {
                                currentFollowupMessage = await _socketCommand.FollowupAsync(message);
                            }
                            else
                            {
                                await currentFollowupMessage.ModifyAsync(prop => prop.Content = message);
                            }
                        };
                    }
                    else // If buffer is below the limit, just update the current message.
                    {
                        await updater(messageBuffer.ToString());
                    }
                }
                // A small delay to batch messages and prevent API rate limiting.
                await Task.Delay(500, _stoppingToken);
            }

            // Final flush: After the loop finishes, there might be remaining text in the buffer.
            if (messageBuffer.Length > 0)
            {
                await updater(messageBuffer.ToString());
            }
        }

        /// <summary>
        /// Extracts a chunk of text from the StringBuilder, respecting the Discord character limit
        /// and preferring to split at a newline character.
        /// </summary>
        private static string GetChunk(StringBuilder buffer)
        {
            string currentContent = buffer.ToString();
            string chunk;

            int searchRangeEndIndex = DiscordTextLimit - 1;
            int splitIndex = currentContent.LastIndexOf('\n', searchRangeEndIndex);

            if (splitIndex != -1)
            {
                // Found a newline. The chunk is from the start up to and including the newline.
                chunk = currentContent.Substring(0, splitIndex + 1);
                buffer.Remove(0, splitIndex + 1);
            }
            else
            {
                // No newline found within the limit. Force a split at the character limit.
                chunk = currentContent.Substring(0, DiscordTextLimit);
                buffer.Remove(0, DiscordTextLimit);
            }

            return chunk;
        }
    }
}