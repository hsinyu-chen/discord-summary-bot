using System.Reflection;

namespace Hcs.Discord
{
    public static class DiscordCommandTypes
    {
        public static (Type commandType, DiscordSlashCommand attribute)[] CommandTypes { get; } = [.. AppDomain.CurrentDomain.GetAssemblies()
            .Where(x=>!x.GetName().FullName.StartsWith(nameof(System)))
            .SelectMany(x => x.GetTypes().Where(x => x.IsClass && !x.IsAbstract && (x.IsAssignableTo(typeof(IDiscordMessageCommand))||x.IsAssignableTo(typeof(IDiscordSlashCommand)))))
            .Select(x => (x, x.GetCustomAttribute<DiscordSlashCommand>()!))
            .Where(static x => x.Item2 != null)];
    }
}
