using Discord.WebSocket;

namespace Hcs.Discord
{
    public interface IDiscordCommandRegister
    {
        Task<SocketApplicationCommand> OnCreate(DiscordSocketClient discordClient);
    }
}