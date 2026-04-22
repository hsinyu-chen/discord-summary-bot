using Hcs.LightI18n.LocalizedString;
using System.Text.RegularExpressions;

namespace Hcs.LightI18n
{
    public static partial class ParsedLocalizedStringHelpers
    {
        /// <summary>
        /// 使用狀態機解析原始字串，將其分解為 LiteralSegment 和 ParameterSegment。
        /// </summary>
        /// <param name="rawText">待解析的原始字串。</param>
        /// <returns>包含解析後區段的列表。</returns>
        internal static List<Segment> ParseText(string rawText)
        {
            var segments = new List<Segment>();
            var matches = VarRegex().Matches(rawText);
            int lastIndex = 0;

            void AddOrMergeLiteral(string text)
            {
                if (segments.Count > 0 && segments[^1] is LiteralSegment lastLiteral)
                {
                    segments[^1] = new LiteralSegment(lastLiteral.Text + (text ?? string.Empty));
                }
                else
                {
                    segments.Add(new LiteralSegment(text ?? string.Empty));
                }
            }

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    AddOrMergeLiteral(rawText[lastIndex..match.Index]);
                }

                if (match.Groups["Escaped"].Success)
                {
                    AddOrMergeLiteral(match.Value[1..]);
                }
                else
                {
                    segments.Add(new ParameterSegment(match.Groups["ParameterName"].Value, match.Groups["FormatString"].Success ? match.Groups["FormatString"].Value : null));
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < rawText.Length)
            {
                AddOrMergeLiteral(rawText[lastIndex..]);
            }


            if (segments.Count == 0)
            {
                AddOrMergeLiteral(rawText ?? string.Empty);
            }

            return segments;
        }
        [GeneratedRegex(@"((?<Escaped>\\)?\{\{(?<ParameterName>(?:(?!\}\})[\p{L}\p{N}_])+)(?:\:(?<FormatString>[^\}]+))?\}\})", RegexOptions.Compiled)]
        private static partial Regex VarRegex();
    }
}