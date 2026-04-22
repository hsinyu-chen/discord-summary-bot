using System.Globalization;
using System.Text;

namespace Hcs.LightI18n.LocalizedString
{
    public abstract class Segment
    {
        public abstract void Append(StringBuilder sb, IDictionary<string, object> args, CultureInfo cultureInfo);
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();
        public static bool operator ==(Segment? left, Segment? right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }
        public static bool operator !=(Segment? left, Segment? right)
        {
            return !(left == right);
        }
    }
}
