using System.Text.RegularExpressions;

namespace SummaryAndCheck.DiscordCommand
{
    internal static partial class RegexHelpers
    {

        [GeneratedRegex(@"(?:https?:\/\/)?(?:www\.)?(?:m\.)?(?:youtube\.com|youtu\.be)\/(?:watch\?v=|embed\/|v\/|)([\w-]{11})(?:\S+)?")]
        public static partial Regex matchYoutube();

        [GeneratedRegex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)")]
        public static partial Regex matchWebUrl();
    }
}