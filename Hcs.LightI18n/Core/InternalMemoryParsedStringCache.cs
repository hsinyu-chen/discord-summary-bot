using Hcs.LightI18n.LocalizedString;
using Microsoft.Extensions.Caching.Memory;

namespace Hcs.LightI18n.Core
{
    public sealed class InternalMemoryParsedStringCache : IParsedStringCache, IDisposable
    {
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());
        public ParsedLocalizedString GetOrCreate(string key, Func<string, ParsedLocalizedString> factory)
        {
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromDays(30);
                return factory(key);
            })!;
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
