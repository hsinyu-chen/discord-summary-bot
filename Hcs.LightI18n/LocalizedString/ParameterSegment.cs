using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Hcs.LightI18n.LocalizedString
{
    public class ParameterSegment(string paramName, string? formatString) : Segment
    {
        public string ParameterName { get; } = paramName;
        public string? FormatString { get; } = formatString;
        public override bool Equals(object? obj)
        {
            if (obj is not ParameterSegment otherSegment)
            {
                return false;
            }
            return ParameterName == otherSegment.ParameterName && FormatString == otherSegment.FormatString;
        }
        public override void Append(StringBuilder sb, IDictionary<string, object> args, CultureInfo cultureInfo)
        {
            if (args.TryGetValue(ParameterName, out var value))
            {
                if (string.IsNullOrEmpty(FormatString) || value == null)
                {
                    sb.Append(value?.ToString() ?? string.Empty);
                }
                else
                {
                    if (value is IFormattable formattableValue)
                    {
                        try
                        {
                            sb.Append(formattableValue.ToString(FormatString, cultureInfo));
                        }
                        catch (FormatException)
                        {
                            sb.Append(value.ToString());
                        }
                    }
                    else
                    {
                        sb.Append(value.ToString());
                    }
                }
            }
            else
            {
                sb.Append("{{").Append(ParameterName);
                if (!string.IsNullOrEmpty(FormatString))
                {
                    sb.Append(':').Append(FormatString);
                }
                sb.Append("}}");
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ParameterName, FormatString);
        }
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(FormatString))
            {
                return $"Parameter [{{{{{ParameterName}:{FormatString}}}}}]";
            }
            return $"Parameter [{{{{{ParameterName}}}}}]";
        }
    }
}
