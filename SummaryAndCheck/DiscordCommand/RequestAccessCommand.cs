using Discord;
using Discord.WebSocket;
using Hcs.Discord;
using Hcs.LightI18n;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SummaryAndCheck.Models;

namespace SummaryAndCheck.DiscordCommand
{

    class RequestAccessCommandRegister : IDiscordCommandRegister
    {
        public async Task<SocketApplicationCommand> OnCreate(DiscordSocketClient discordClient)
        {
            var globalCommand = new SlashCommandBuilder();
            globalCommand.WithName("requestaccess").WithDescription("Request Bot access")
            .WithDescriptionLocalizations(L.GetMap("RequestAccessCommand:Description"));
            return await discordClient.CreateGlobalApplicationCommandAsync(globalCommand.Build());
        }
    }
    [DiscordCommand<RequestAccessCommandRegister>]
    class RequestAccessCommand(SummaryAndCheckDbContext dbContext, ILogger<RequestAccessCommand> logger) : IDiscordSlashCommand
    {
        public async Task Excute(SocketSlashCommand arg)
        {
            await arg.DeferAsync(true);
            using var _ = L.GetScope(arg.UserLocale, "RequestAccessCommand");
            var userSet = dbContext.Set<DiscordUser>();
            try
            {
                if (!await userSet.AnyAsync(x => x.Id == arg.User.Id))
                {
                    await userSet.AddAsync(new DiscordUser { Name = arg.User.GlobalName, Id = arg.User.Id });
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    await userSet.Where(x => x.Id == arg.User.Id).ExecuteUpdateAsync(settings => settings.SetProperty(x => x.Name, arg.User.GlobalName));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "add while add/update user");
            }
            finally
            {
                var state = await userSet.Where(x => x.Id == arg.User.Id).Select(x => x.IsEnabled).FirstOrDefaultAsync();
                await arg.ModifyOriginalResponseAsync(msg =>
                {
                    msg.Content = "Success".T(new { state = (state ? "Enabled" : "NotEnabled").T() });
                });
            }

        }

    }
}
