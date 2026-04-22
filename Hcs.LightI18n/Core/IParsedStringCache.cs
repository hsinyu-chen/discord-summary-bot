using Hcs.LightI18n.LocalizedString;

namespace Hcs.LightI18n.Core
{
    public interface IParsedStringCache
    {
        ParsedLocalizedString GetOrCreate(string key, Func<string, ParsedLocalizedString> factory);
    }
}
