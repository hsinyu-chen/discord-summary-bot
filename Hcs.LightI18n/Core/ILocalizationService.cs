using System.Globalization;

namespace Hcs.LightI18n.Core
{
    public interface ILocalizationService
    {
        string PathPrefix { get; set; }
        string Get(string lang, string path, params object[] args);
        IDictionary<string, string> GetMap(string path, params object[] args);
        string ParseAndFormat(string text, CultureInfo culture, params object[] args);
    }


}