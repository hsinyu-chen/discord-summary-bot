using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Hcs.LightI18n.LocalizedString
{
    public class ParsedLocalizedString(string rawText)
    {
        public List<Segment> Segments { get; } = ParsedLocalizedStringHelpers.ParseText(rawText);

        public string Format(IDictionary<string, object> args, CultureInfo cultureInfo)
        {
            var sb = new StringBuilder();
            foreach (var segment in Segments)
            {
                segment.Append(sb, args, cultureInfo);
            }
            return sb.ToString();
        }
    }
}
