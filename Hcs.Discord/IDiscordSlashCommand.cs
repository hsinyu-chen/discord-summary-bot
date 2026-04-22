using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hcs.Discord
{
    public interface IDiscordSlashCommand
    {
        Task Excute(SocketSlashCommand arg);
    }
    public interface IDiscordMessageCommand
    {
        Task Excute(SocketMessageCommand arg);
    }
}
