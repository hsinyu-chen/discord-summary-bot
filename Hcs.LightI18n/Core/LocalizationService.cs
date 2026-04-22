using Hcs.LightI18n.LocalizedString;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Hcs.LightI18n.Core
{
    public class LocalizationService : ILocalizationService, IDisposable
    {
        private static readonly ConcurrentDictionary<Type, Action<object, Dictionary<string, object>>> _objectToDictionaryConverters = new();
        private readonly ConcurrentDictionary<string, string> _stringCache = new();
        private readonly IConfiguration configuration;
        private readonly IParsedStringCache parsedStringCache;
        private readonly ILogger<LocalizationService> logger;
        private readonly IDisposable changeDetection;
        public LocalizationService(IConfiguration configuration, IParsedStringCache parsedStringCache, ILogger<LocalizationService>? logger = null)
        {
            this.configuration = configuration;
            this.parsedStringCache = parsedStringCache;
            this.logger = logger ?? NullLogger<LocalizationService>.Instance;
            changeDetection = ChangeToken.OnChange(configuration.GetReloadToken, () =>
            {
                logger?.LogInformation("reset locale cache");
                allLocaleId = new([]);
                _stringCache.Clear();
            });
        }
        private string pathPrefix = string.Empty;
        public string PathPrefix
        {
            get { return pathPrefix; }
            set
            {
                pathPrefix = value;
                allLocaleId = new(() =>
                {
                    return [.. configuration.GetSection(PathPrefix).GetChildren().Select(x => x.Key?.Trim('"', '\'') ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x))];
                });
            }
        }
        private Lazy<string[]> allLocaleId = new([]);
        public string[] GetAllLocalIds() => allLocaleId.Value;
        const string staticDefault = "en";
        public string GetLocalId(string expect)
        {
            if (string.IsNullOrWhiteSpace(expect))
            {
                return allLocaleId.Value.FirstOrDefault() ?? staticDefault;
            }

            if (allLocaleId.Value.Contains(expect))
            {
                return expect;
            }

            var parts = expect.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                var langPart = parts[0];
                if (allLocaleId.Value.Contains(langPart))
                {
                    return langPart;
                }
                var parentMatch = allLocaleId.Value.FirstOrDefault(x => x.StartsWith(langPart, StringComparison.OrdinalIgnoreCase));
                if (parentMatch != null)
                {
                    return parentMatch;
                }
            }
            return allLocaleId.Value.FirstOrDefault() ?? staticDefault;
        }
        public string Get(string lang, string path, params object[] args)
        {
            var localId = GetLocalId(lang);
            var fullPath = $"{PathPrefix}:{localId}:{path}";
            var text = _stringCache.GetOrAdd(fullPath, key => configuration[key] ?? string.Empty);

            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(localId);
            }
            catch (CultureNotFoundException ex)
            {
                logger?.LogWarning(ex, "Culture '{localId}' not found. Using InvariantCulture for formatting.", localId);
                culture = CultureInfo.InvariantCulture;
            }
            if (string.IsNullOrEmpty(text))
            {
                logger?.LogWarning("Missing translation for key '{fullPath}'.", fullPath);
                return path;
            }
            return ParseAndFormat(text, culture, args);
        }

        public string ParseAndFormat(string text, CultureInfo culture, params object[] args)
        {
            var argsDictionary = args.Aggregate(new Dictionary<string, object>(), (p, c) =>
            {
                AppendObjectToDictionary(c, p);
                return p;
            });

            return parsedStringCache.GetOrCreate(text, key => new ParsedLocalizedString(key)).Format(argsDictionary, culture);
        }

        public IDictionary<string, string> GetMap(string path, params object[] args)
        {
            return GetAllLocalIds().ToDictionary(x => x, x => Get(x, path, args));
        }
        internal static void AppendObjectToDictionary(object obj, Dictionary<string, object> d)
        {
            if (obj == null) return;

            var type = obj.GetType();

            _objectToDictionaryConverters.GetOrAdd(type, typeToCache =>
            {
                var dictionaryType = typeof(Dictionary<string, object>);

                var param = Expression.Parameter(typeof(object), "obj");
                var dictParam = Expression.Parameter(dictionaryType, "dict");
                var indexerProperty = dictionaryType.GetProperty("Item")!;
                var unboxedVar = Expression.Variable(typeToCache, "unboxed");
                return Expression.Lambda<Action<object, Dictionary<string, object>>>(Expression.Block([unboxedVar], [
                    Expression.Assign(unboxedVar,Expression.Convert(param,typeToCache)),
                    ..typeToCache.GetProperties().Where(x=>x.CanRead).Select(prop=>Expression.Assign(Expression.MakeIndex(dictParam,indexerProperty,[Expression.Constant(prop.Name)]),Expression.Convert(Expression.Property(unboxedVar,prop.Name),typeof(object)))),
                    Expression.Default(typeof(void))
                ]), param, dictParam).Compile();
            })(obj, d);
        }

        public void Dispose()
        {
            changeDetection.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
