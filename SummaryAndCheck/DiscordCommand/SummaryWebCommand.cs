using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Hcs.Discord;
using Hcs.LightI18n;
using Microsoft.Extensions.Logging;
using SummaryAndCheck.Models;
using SummaryAndCheck.Services;
using System.Text.RegularExpressions;

namespace SummaryAndCheck.DiscordCommand
{
    // 將 DiscordCommand 類型改為針對訊息命令的註冊類別
    [DiscordCommand<SummaryWebContextCommandRegister>]
    class SummaryWebCommand(SummaryService summaryService) : IDiscordMessageCommand
    {
        public async Task Excute(SocketMessageCommand arg)
        {
            string messageContent = arg.Data.Message.Content;
            Match match = RegexHelpers.matchWebUrl().Match(messageContent);
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
                    if(!string.IsNullOrWhiteSpace(e.Description))
                    {
                        match = RegexHelpers.matchWebUrl().Match(e.Description);
                        if (match.Success)
                        {
                            break;
                        }
                    }
                }
            }
            if (!match.Success)
            {
                using var _ = L.GetScope(arg.UserLocale, "SummaryWebCommand");
                await arg.RespondAsync(text: "NoUrlFoundInMessage".T(), ephemeral: true);
                return;
            }
            var serverLocale = arg.GuildLocale ?? arg.UserLocale;
            var tcs = new TaskCompletionSource<string?>();
            try
            {
                var enqueueResult = await summaryService.EnqueueAsync(arg, arg.Data.Message.Id, arg.CreateSummaryRequest(match.Value, UseType.WebSummary, tcs.Task));

                if (enqueueResult.Success)
                {
                    using var _ = L.GetScope(serverLocale, "SummaryWebCommand");
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
                    using var _ = L.GetScope(arg.UserLocale, "SummaryWebCommand");
                    await arg.RespondAsync(text: enqueueResult.Message.T(new { userId = arg.User.Id }, enqueueResult.MessageArgs), ephemeral: true);
                }
            }
            finally
            {
                tcs.TrySetResult(null);
            }

        }
    }

    // 新增訊息上下文命令註冊類別
    class SummaryWebContextCommandRegister : IDiscordCommandRegister
    {
        public async Task<SocketApplicationCommand> OnCreate(DiscordSocketClient discordClient)
        {
            var messageCommand = new MessageCommandBuilder()
                .WithName("Summary Web Page")
                .WithNameLocalizations(L.GetMap("SummaryWebCommand:CommandName"));

            return await discordClient.CreateGlobalApplicationCommandAsync(messageCommand.Build());
        }
    }
}