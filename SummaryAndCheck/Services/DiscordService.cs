using Discord;
using Discord.WebSocket;
using Hcs.Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SummaryAndCheck.Options;
using System.Collections.Concurrent;

namespace SummaryAndCheck.Services
{
    internal class DiscordService(ILogger<DiscordService> logger, IOptions<DiscordOptions> options, IServiceProvider serviceProvider) : IHostedService
    {
        readonly DiscordSocketClient discordClient = new();
        readonly ConcurrentDictionary<string, Type> commandMapping = [];
        readonly ConcurrentDictionary<string, Type> messageCommandMapping = [];
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("discord service starting..");
            discordClient.LoggedIn += async () =>
            {
                await Task.CompletedTask;
                logger.LogInformation("discord logged in.");
            };
            discordClient.LoggedOut += async () =>
            {
                await Task.CompletedTask;
                logger.LogInformation("discord logged out.");
            };
            discordClient.Connected += async () =>
            {
                await Task.CompletedTask;
                logger.LogInformation("discord connected.");
            };
            discordClient.Ready += async () =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                logger.LogInformation("discord client ready.");

                commandMapping.Clear();
                messageCommandMapping.Clear();
                foreach (var (cmdType, attr) in DiscordCommandTypes.CommandTypes)
                {
                    try
                    {
                        if (scope.ServiceProvider.GetService(attr.RegisterType) is IDiscordCommandRegister register)
                        {
                            var creaded = await register.OnCreate(discordClient);
                            if (cmdType.IsAssignableTo(typeof(IDiscordSlashCommand)))
                            {
                                commandMapping.TryAdd(creaded.Name, cmdType);
                                logger.LogInformation("command [{cmd}] is created", creaded.Name);
                            }
                            else if (cmdType.IsAssignableTo(typeof(IDiscordMessageCommand)))
                            {
                                messageCommandMapping.TryAdd(creaded.Name, cmdType);
                                logger.LogInformation("message command [{cmd}] is created", creaded.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "create command error");
                    }
                }

            };
            discordClient.SlashCommandExecuted += DiscordClient_SlashCommandExecuted;
            discordClient.MessageCommandExecuted += DiscordClient_MessageCommandExecuted;
            logger.LogInformation("logging discord ..");
            await discordClient.LoginAsync(TokenType.Bot, options.Value.ApiKey);
            await discordClient.StartAsync();
        }

        private async Task DiscordClient_MessageCommandExecuted(SocketMessageCommand arg)
        {
            logger.LogInformation("discord message command actived [{CommandName}] User [{user}]", arg.CommandName, arg.User.Id);
            try
            {
                if (messageCommandMapping.TryGetValue(arg.CommandName, out var cmdType))
                {
                    await using var scope = serviceProvider.CreateAsyncScope();
                    if (scope.ServiceProvider.GetRequiredService(cmdType) is IDiscordMessageCommand command)
                    {
                        await command.Excute(arg);
                    }
                }
                else
                {
                    await arg.RespondAsync("Bot initializing , please try later");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Command [{command}] excution error", arg.CommandName);
            }
        }

        private async Task DiscordClient_SlashCommandExecuted(SocketSlashCommand arg)
        {
            logger.LogInformation("discord slash command actived [{CommandName}] User [{user}]", arg.CommandName, arg.User.Id);
            try
            {
                if (commandMapping.TryGetValue(arg.CommandName, out var cmdType))
                {
                    await using var scope = serviceProvider.CreateAsyncScope();
                    if (scope.ServiceProvider.GetRequiredService(cmdType) is IDiscordSlashCommand command)
                    {
                        await command.Excute(arg);
                    }
                }
                else
                {
                    await arg.RespondAsync("Bot initializing , please try later");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Command [{command}] excution error", arg.CommandName);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("discord service stopping..");
            await discordClient.DisposeAsync();
            logger.LogInformation("discord service stopped.");
        }

    }
}
