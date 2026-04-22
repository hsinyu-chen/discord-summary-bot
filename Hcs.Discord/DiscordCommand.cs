namespace Hcs.Discord
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DiscordCommand<TRegister> : DiscordSlashCommand where TRegister : IDiscordCommandRegister
    {
        public DiscordCommand() : base(typeof(TRegister)) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DiscordSlashCommand(Type registerType) : Attribute
    {
        public Type RegisterType { get; } = registerType;
    }
}
