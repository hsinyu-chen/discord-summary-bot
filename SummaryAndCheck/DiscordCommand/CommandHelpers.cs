using Discord.WebSocket;
using SummaryAndCheck.Services;

namespace SummaryAndCheck.DiscordCommand
{
    static class CommandHelpers
    {
        public static SummaryRequest CreateSummaryRequest(this SocketCommandBase socketCommandBase, string content, Models.UseType useType, Task<string?> messageLink)
        {
            var serverLocale = socketCommandBase.GuildLocale ?? socketCommandBase.UserLocale;
            return new SummaryRequest
            {
                OriginalResponseLink = messageLink,
                SocketCommand = socketCommandBase,
                SummaryContext = new()
                {
                    TargetLocale = serverLocale,
                    ShareIdentifier = socketCommandBase.ChannelId ?? 0,
                    Content = content,
                    Type = useType
                }
            };
        }
    }
}
