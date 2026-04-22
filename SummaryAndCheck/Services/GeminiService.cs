// GeminiService.cs
using System.Text;
using System.Globalization;
using Discord.Commands;
using Hcs.LightI18n;
using Hcs.LightI18n.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SummaryAndCheck.Models;
using SummaryAndCheck.Options;
using Google.GenAI;
using Google.GenAI.Types;
using Part = Google.GenAI.Types.Part;

namespace SummaryAndCheck.Services
{
    partial class GeminiService(ILogger<GeminiService> logger, ILocalizationService localizationService, WebCaptureService webCaptureService, IOptions<GeminiOptions> options)
    {
        private Google.GenAI.Client? _client;

        private Google.GenAI.Client Client
        {
            get
            {
                if (_client == null)
                {
                    if (!string.IsNullOrEmpty(options.Value.ApiKey))
                    {
                        System.Environment.SetEnvironmentVariable("GOOGLE_API_KEY", options.Value.ApiKey);
                    }
                    _client = new Google.GenAI.Client();
                }
                return _client;
            }
        }

        public async Task ProcessAsync(QueuedRequest request, CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(options.Value.ApiKey))
            {
                return;
            }
            var summaryRequest = request.SummaryRequest;

            logger.LogInformation("開始呼叫Gemini (Official SDK)");
            using var _ = L.GetScope(summaryRequest.SummaryContext.TargetLocale, "GeminiService");
            await Task.Delay(100, stoppingToken);
            var discordMessage = new DiscordMessageManager(summaryRequest.SocketCommand, stoppingToken);

            var monitor = new GeminiMonitor(() => discordMessage.DirectWriteAsync, summaryRequest.SocketCommand.UserLocale, stoppingToken);
            try
            {
                var waitS = 0;
                await discordMessage.DirectWriteAsync("Processing".T(new { waiting = waitS }));
                discordMessage.AppendToMessage("Declaimer".T() + "\n");
                monitor.ToggleRunning(true);
                var tempSb = new StringBuilder();
                var flashed = false;

                // Prepare the generation config
                var config = new GenerateContentConfig
                {
                    Temperature = (float?)options.Value.Temperature,
                    MaxOutputTokens = 50000
                };

                // Only set ThinkingConfig if ThinkingBudget is not -1 (which represents null/undefined)
                if (options.Value.ThinkingBudget != -1)
                {
                    config.ThinkingConfig = new ThinkingConfig
                    {
                        IncludeThoughts = true,
                        ThinkingBudget = options.Value.ThinkingBudget
                    };
                }

                var contents = await PrepareContentsAsync(summaryRequest, stoppingToken);
                if (contents == null)
                {
                    monitor.Stop();
                    return;
                }

                // Streaming call
                var responseStream = Client.Models.GenerateContentStreamAsync(
                    model: options.Value?.Model ?? "gemini-2.0-flash",
                    contents: contents,
                    config: config
                );

                GenerateContentResponse? finalResponse = null;

                await foreach (var response in responseStream)
                {
                    if (monitor.IsCancellationRequested)
                    {
                        monitor.Stop();
                        break;
                    }

                    finalResponse = response;
                    discordMessage.Start();

                    if (response.Candidates != null && response.Candidates.Count > 0)
                    {
                        var candidate = response.Candidates[0];
                        if (candidate.Content != null && candidate.Content.Parts != null)
                        {
                            foreach (var part in candidate.Content.Parts)
                            {
                                if (!string.IsNullOrEmpty(part.Text))
                                {
                                    var text = part.Text;
                                    if (text.Contains('\n'))
                                    {
                                        tempSb.Append(text);
                                        int lastNewlineIndex = tempSb.ToString().LastIndexOf('\n');
                                        var toDisplay = tempSb.ToString(0, lastNewlineIndex + 1);
                                        discordMessage.AppendToMessage(Format(toDisplay, summaryRequest.SummaryContext));
                                        tempSb.Remove(0, lastNewlineIndex + 1);
                                        flashed = true;
                                    }
                                    else
                                    {
                                        flashed = false;
                                        tempSb.Append(text);
                                    }
                                }
                            }
                        }
                    }
                }

                if (!flashed && tempSb.Length > 0)
                {
                    discordMessage.AppendToMessage(Format(tempSb.ToString(), summaryRequest.SummaryContext));
                    flashed = true;
                }

                monitor.Stop();
                await monitor.Task;

                // Handle completion
                if (finalResponse != null)
                {
                    if (finalResponse.UsageMetadata != null)
                    {
                        request.State = SummaryState.Success;
                        var totalPrice = CalculatePrice(finalResponse.UsageMetadata);

                        discordMessage.AppendToMessage("\n");
                        discordMessage.AppendToMessage("AICost".T(new
                        {
                            model = options.Value?.Model ?? "gemini-2.0-flash",
                            input = finalResponse.UsageMetadata.PromptTokenCount ?? 0,
                            output = finalResponse.UsageMetadata.CandidatesTokenCount ?? 0,
                            price = totalPrice
                        }));
                    }

                    if (finalResponse.Candidates != null && finalResponse.Candidates.Count > 0)
                    {
                        var finishReason = finalResponse.Candidates[0].FinishReason;
                        logger.LogInformation("Gemini Finished : {FinishReason}", finishReason);

                        // Check for Stop reason (converting to string to avoid enum naming issues)
                        var reasonStr = finishReason?.ToString()?.ToUpperInvariant();
                        if (reasonStr != "STOP")
                        {
                            if (reasonStr == "STOP")
                            {
                                request.State = SummaryState.Success;
                            }
                            else
                            {
                                request.State = SummaryState.Failed;
                                var reason = $"FinishReason:{finishReason}".T();
                                discordMessage.AppendToMessage("UnexpectedStop".T(new { reason }));
                            }
                        }
                        else
                        {
                            request.State = SummaryState.Success;
                        }
                    }
                }

                await discordMessage.FinishAsync();

                logger.LogInformation("Gemini 結束回應");
            }
            catch (Exception ex)
            {
                if (!monitor.IsCancellationRequested)
                {
                    monitor.Stop();
                }
                logger.LogError(ex, "Gemini Error");
                await discordMessage.DirectWriteAsync("GeminiError".T());
            }
        }

        private async Task<List<Content>?> PrepareContentsAsync(SummaryRequest request, CancellationToken stoppingToken)
        {
            var promptText = GetPrompt(request.SummaryContext.TargetLocale, request.SummaryContext);
            var mainContentParts = new List<Part>();

            mainContentParts.Add(new Part { Text = promptText });

            if (request.SummaryContext.Type == UseType.VideoSummary)
            {
                // Video
                mainContentParts.Add(new Part
                {
                    FileData = new FileData
                    {
                        FileUri = $"https://www.youtube.com/watch?v={request.SummaryContext.Content}",
                        MimeType = "video/mp4"
                    }
                });
            }
            else
            {
                // Web Summary
                var content = await webCaptureService.GetWebPageContent(request.SummaryContext.Content, request.SocketCommand.UserLocale, stoppingToken);
                if (string.IsNullOrWhiteSpace(content) || content.Length < 100)
                {
                    return null;
                }
                mainContentParts.Add(new Part { Text = content });
            }

            return new List<Content>
            {
                new Content
                {
                    Role = "user",
                    Parts = mainContentParts
                }
            };
        }

        private decimal CalculatePrice(GenerateContentResponseUsageMetadata usage)
        {
            const decimal Million = 1_000_000m;
            decimal priceMultiplier_Input_TextVisualVideo = options.Value.PricePerMillionTokens_Input_TextVisualVideo / Million;
            decimal PriceMultiplier_Input_Audio = options.Value.PricePerMillionTokens_Input_Audio / Million;
            decimal PriceMultiplier_Output = options.Value.PricePerMillionTokens_Output / Million;

            decimal inputCost = 0m;

            if (usage.PromptTokensDetails != null)
            {
                foreach (var detail in usage.PromptTokensDetails)
                {
                    var modality = detail.Modality?.ToString().ToUpperInvariant();

                    inputCost += modality switch
                    {
                        "TEXT" or "IMAGE" or "VIDEO" => (detail.TokenCount ?? 0) * priceMultiplier_Input_TextVisualVideo,
                        "AUDIO" => (detail.TokenCount ?? 0) * PriceMultiplier_Input_Audio,
                        _ => (detail.TokenCount ?? 0) * priceMultiplier_Input_TextVisualVideo,
                    };
                }
            }
            else
            {
                inputCost = (usage.PromptTokenCount ?? 0) * priceMultiplier_Input_TextVisualVideo;
            }

            var outputCandidatesTokenCount = (usage.CandidatesTokenCount ?? ((usage.TotalTokenCount ?? 0) - (usage.PromptTokenCount ?? 0)));
            var outputCost = outputCandidatesTokenCount * PriceMultiplier_Output;
            return inputCost + outputCost;
        }

        protected static string Format(string source, SummaryContext context)
        {
            source = repeatedText().Replace(source, m => $"`{m.Groups[1].Value}`");
            if (context.Type == UseType.VideoSummary)
            {
                string videoId = context.Content;
                return timeCodeRegex().Replace(source, (m) =>
                {
                    var timeParam = new StringBuilder();
                    var display = new StringBuilder();
                    if (m.Groups["h"].Success)
                    {
                        display.Append($"{m.Groups["h"].Value.PadLeft(2, '0')}:");
                        timeParam.Append($"{m.Groups["h"].Value.PadLeft(2, '0')}h");
                    }
                    if (m.Groups["m"].Success)
                    {
                        display.Append($"{m.Groups["m"].Value.PadLeft(2, '0')}:");
                        timeParam.Append($"{m.Groups["m"].Value.PadLeft(2, '0')}m");
                    }
                    if (m.Groups["s"].Success)
                    {
                        display.Append($"{m.Groups["s"].Value.PadLeft(2, '0')}");
                        timeParam.Append($"{m.Groups["s"].Value.PadLeft(2, '0')}s");
                    }
                    return $"- **[[{display}](<https://www.youtube.com/watch?v={videoId}&t={timeParam}>)]**";
                });
            }
            return source;
        }
        protected string GetPrompt(string locale, SummaryContext context)
        {
            var parameters = new { time = DateTimeOffset.UtcNow, locale };
            if (context.Type == UseType.VideoSummary)
            {
                return localizationService.ParseAndFormat(options.Value.Prompts ?? string.Empty, CultureInfo.GetCultureInfo(locale), parameters);
            }
            else
            {
                return localizationService.ParseAndFormat(options.Value.WebPrompts ?? string.Empty, CultureInfo.GetCultureInfo(locale), parameters);
            }
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"\[+(?<full>(?<h>\d{1,2}):(?<m>\d{1,2}):(?<s>\d{1,2})|(?<m>\d{1,2}):(?<s>\d{1,2})|(?<s>\d{1,2}))\]+")]
        private static partial System.Text.RegularExpressions.Regex timeCodeRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"([`《([{]?)((\d+(?:\.\d+)*))(?(1)(?:`|》|\)|\\]|\})?)\s*\(\s*\2\s*\)", System.Text.RegularExpressions.RegexOptions.Compiled)]
        private static partial System.Text.RegularExpressions.Regex repeatedText();
    }
}