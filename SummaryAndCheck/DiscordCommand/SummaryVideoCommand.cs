using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Hcs.Discord;
using Hcs.LightI18n;
using SummaryAndCheck.Models;
using SummaryAndCheck.Services;
using System.Text.RegularExpressions;

namespace SummaryAndCheck.DiscordCommand
{
    [DiscordCommand<SummaryVideoCommandRegister>]
    class SummaryVideoCommand(SummaryService summary) : IDiscordMessageCommand
    {
        public async Task Excute(SocketMessageCommand arg)
        {
            var match = RegexHelpers.matchYoutube().Match(arg.Data.Message.Content);
            if (!match.Success)
            {
                foreach (var e in arg.Data.Message.Embeds)
                {
                    if (!string.IsNullOrWhiteSpace(e.Url))
                    {
                        match = RegexHelpers.matchWebUrl().Match(e.Url);
                        if (match.Success)
                        {
                            break;
                        }
                    }
                    if(e.Video.HasValue && !string.IsNullOrWhiteSpace(e.Video.Value.Url))
                    {
                        match = RegexHelpers.matchWebUrl().Match(e.Video.Value.Url);
                        if (match.Success)
                        {
                            break;
                        }
                    }
                }
            }
            if (match.Success)
            {
                var serverLocale = arg.GuildLocale ?? arg.UserLocale;
                var tcs = new TaskCompletionSource<string?>();
                try
                {
                    var enqueueResult = await summary.EnqueueAsync(arg, arg.Data.Message.Id, arg.CreateSummaryRequest(match.Groups[1].Value, UseType.VideoSummary, tcs.Task));
                    if (enqueueResult.Success)
                    {
                        using var _ = L.GetScope(serverLocale, "SummaryMessageCommand");
                        if (enqueueResult.EnqueueResult!.IsNewRequest)
                        {
                            await arg.RespondAsync("Queued".T(new { queueNumber = enqueueResult.QueueNumber }, enqueueResult.MessageArgs), ephemeral: false);
                            var message = await arg.GetOriginalResponseAsync();
                            tcs.SetResult(message.GetJumpUrl());
                        }
                        else
                        {
                            await arg.RespondAsync("Common:Cached".T(arg.UserLocale, new { link = await enqueueResult.EnqueueResult!.ActiveRequest.OriginalResponseLink }), ephemeral: true);
                        }
                    }
                    else
                    {
                        using var _ = L.GetScope(arg.UserLocale, "SummaryMessageCommand");
                        await arg.RespondAsync(enqueueResult.Message.T(new { userId = arg.User.Id }, enqueueResult.MessageArgs), ephemeral: true);
                    }
                }
                finally
                {
                    tcs.TrySetResult(null);
                }
            }
            else
            {
                using var _ = L.GetScope(arg.UserLocale, "SummaryMessageCommand");
                await arg.RespondAsync("NotSupported".T(), ephemeral: true);
            }
        }
    }
    class SummaryVideoCommandRegister : IDiscordCommandRegister
    {
        public async Task<SocketApplicationCommand> OnCreate(DiscordSocketClient discordClient)
        {
            var messageCommand = new MessageCommandBuilder().WithName("Summary").WithNameLocalizations(L.GetMap("SummaryContextCommand:Name"));
            return await discordClient.CreateGlobalApplicationCommandAsync(messageCommand.Build());
        }
    }
}
