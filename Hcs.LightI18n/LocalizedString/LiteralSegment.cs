using System.Globalization;
using System.Text;

namespace Hcs.LightI18n.LocalizedString
{
    public class LiteralSegment(string text) : Segment
    {
        public string Text { get; } = text;
        public override void Append(StringBuilder sb, IDictionary<string, object> args, CultureInfo cultureInfo)
        {
            sb.Append(Text);
        }
        public override bool Equals(object? obj)
        {
            if (obj is not LiteralSegment otherSegment)
            {
                return false;
            }
            return Text == otherSegment.Text;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text);
        }
        public override string ToString()
        {
            return $"Literal[{Text}]";
        }
    }
}
