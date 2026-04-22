using System.Globalization;
using Hcs.LightI18n.LocalizedString;

namespace Hcs.LightI18n.Tests
{
    // --- TheoryData for Parse_CorrectlyParsesText ---
    public class ParseTestData : TheoryData<string, List<Segment>>
    {
        public ParseTestData()
        {
            Add("Hello World",
            [
                new LiteralSegment("Hello World")
            ]);
            Add("Hello {{Name}}!",
            [
                new LiteralSegment("Hello "),
                new ParameterSegment("Name", null),
                new LiteralSegment("!")
            ]);
            Add("{{Greeting}} World",
            [
                new ParameterSegment("Greeting", null),
                new LiteralSegment(" World")
            ]);
            Add("Hello {{Name}} and {{Age}}!",
            [
                new LiteralSegment("Hello "),
                new ParameterSegment("Name", null),
                new LiteralSegment(" and "),
                new ParameterSegment("Age", null),
                new LiteralSegment("!")
            ]);
            Add("No parameters here.",
            [
                new LiteralSegment("No parameters here.")
            ]);
            Add("",
            [
                new LiteralSegment("")
            ]);
            Add("\\{{EscapedBrace}}",
            [
                new LiteralSegment("{{EscapedBrace}}")
            ]);
            Add("some \\{{EscapedBrace}} and some {{var}}",
            [
                new LiteralSegment("some {{EscapedBrace}} and some "),
                new ParameterSegment("var", null)
            ]);
            Add("{{}}",
            [
                new LiteralSegment("{{}}")
            ]); // 確保無效參數名被視為字面量
            Add("{{param_name}}",
            [
                new ParameterSegment("param_name", null)
            ]);
            Add("{{paramName123}}",
            [
                new ParameterSegment("paramName123", null)
            ]);
            Add("Text with {{param}} and {{another_param}}.",
            [
                new LiteralSegment("Text with "),
                new ParameterSegment("param", null),
                new LiteralSegment(" and "),
                new ParameterSegment("another_param", null),
                new LiteralSegment(".")
            ]);
            Add("Only {{param}}",
            [
                new LiteralSegment("Only "),
                new ParameterSegment("param", null)
            ]);
            Add("{{param}} only",
            [
                new ParameterSegment("param", null),
                new LiteralSegment(" only")
            ]);
            Add("{{",
            [
                new LiteralSegment("{{" )
            ]);
            Add("{",
            [
                new LiteralSegment("{")
            ]);
            Add("}}",
            [
                new LiteralSegment("}}")
            ]);
            Add("Text with {single brace}",
            [
                new LiteralSegment("Text with {single brace}")
            ]);
            Add("Text with {{param",
            [
                new LiteralSegment("Text with {{param")
            ]);
            Add("Text with {{param}} and {",
            [
                new LiteralSegment("Text with "),
                new ParameterSegment("param", null),
                new LiteralSegment(" and {")
            ]);
            Add("Text with {{param}} and }}",
            [
                new LiteralSegment("Text with "),
                new ParameterSegment("param", null),
                new LiteralSegment(" and }}")
            ]);
            Add("Text with {{{param}}}",
            [
                new LiteralSegment("Text with {"),
                new ParameterSegment("param", null),
                new LiteralSegment("}")
            ]);
            Add("Text with {{{{param}}}}",
            [
                new LiteralSegment("Text with {{"),
                new ParameterSegment("param", null),
                new LiteralSegment("}}")
            ]);

            Add("Price: {{amount:c}}",
            [
                new LiteralSegment("Price: "),
                new ParameterSegment("amount", "c")
            ]);
            Add("Date: {{eventDate:yyyy-MM-dd}}",
            [
                new LiteralSegment("Date: "),
                new ParameterSegment("eventDate", "yyyy-MM-dd")
            ]);
            Add("Mixed: {{num:N2}} and {{text}}",
            [
                new LiteralSegment("Mixed: "),
                new ParameterSegment("num", "N2"),
                new LiteralSegment(" and "),
                new ParameterSegment("text", null)
            ]);
            Add("Only {{param:format}}",
            [
                new LiteralSegment("Only "),
                new ParameterSegment("param", "format")
            ]);
            Add("{{param:format}} only",
            [
                new ParameterSegment("param", "format"),
                new LiteralSegment(" only")
            ]);
            Add("{{param_name:F3}}",
            [
                new ParameterSegment("param_name", "F3")
            ]);
            Add("Multiple formats: {{item:N2|custom}}",
            [
                new LiteralSegment("Multiple formats: "),
                new ParameterSegment("item", "N2|custom")
            ]);
        }
    }

    // --- TheoryData for Format_CorrectlyFormatsString ---
    public class FormatTestData : TheoryData<string, string, string, Dictionary<string, object>>
    {
        public FormatTestData()
        {
            // rawText, expectedFormattedText, cultureString, paramDict
            Add("Hello World", "Hello World", "en-US", []); // 無參數
            Add("Hello {{Name}}!", "Hello John!", "en-US", new Dictionary<string, object> { { "Name", "John" } });
            Add("{{Greeting}} World", "Hi World", "en-US", new Dictionary<string, object> { { "Greeting", "Hi" } });
            Add("Hello {{Name}} and {{Age}}!", "Hello Alice and 30!", "en-US", new Dictionary<string, object> { { "Name", "Alice" }, { "Age", 30 } });
            Add("No parameters here.", "No parameters here.", "en-US", []);
            Add("", "", "en-US", []);
            Add("Missing param: {{Missing}}", "Missing param: {{Missing}}", "en-US", []);
            Add("Value is {{Value}}.", "Value is .", "en-US", new Dictionary<string, object> { { "Value", null } }); // Null 值應該是空字串
            Add("Value is {{Value}}.", "Value is .", "en-US", new Dictionary<string, object> { { "Value", string.Empty } }); // 空字串值

            // 新增帶格式的測試案例
            // 數字格式
            Add("Price: {{amount:C2}}", "Price: $1,234.56", "en-US", new Dictionary<string, object> { { "amount", 1234.56m } }); // decimal 類型
            Add("Price: {{amount:C2}}", "Price: $1,234.56", "zh-TW", new Dictionary<string, object> { { "amount", 1234.56m } });
            Add("Value: {{num:N0}}", "Value: 1,235", "en-US", new Dictionary<string, object> { { "num", 1234.56 } }); // double 類型
            Add("Int: {{val:D5}}", "Int: 00123", "en-US", new Dictionary<string, object> { { "val", 123 } }); // int 類型

            // 日期時間格式
            Add("Date: {{eventDate:yyyy/MM/dd}}", "Date: 2025/07/13", "en-US", new Dictionary<string, object> { { "eventDate", new DateTime(2025, 7, 13) } });
            Add("Date: {{eventDate:D}}", "Date: Sunday, July 13, 2025", "en-US", new Dictionary<string, object> { { "eventDate", new DateTime(2025, 7, 13) } });
            Add("Date: {{eventDate:D}}", "Date: 2025年7月13日 星期日", "zh-TW", new Dictionary<string, object> { { "eventDate", new DateTime(2025, 7, 13) } });
            Add("Time: {{eventTime:t}}", "Time: 1:30 PM", "en-US", new Dictionary<string, object> { { "eventTime", new DateTime(2025, 1, 1, 13, 30, 0) } });

            // 不支援格式/型別的回退測試
            Add("Bool: {{flag:G}}", "Bool: True", "en-US", new Dictionary<string, object> { { "flag", true } }); // bool, G 格式
            Add("Unsupported Format: {{text:invalid}}", "Unsupported Format: Some Text", "en-US", new Dictionary<string, object> { { "text", "Some Text" } }); // 字串帶不支援格式，預期回退到 ToString()
            Add("Unsupported Type Format: {{obj:C}}", "Unsupported Type Format: System.Object", "en-US", new Dictionary<string, object> { { "obj", new object() } }); // 泛型物件帶格式，預期回退
        }
    }

    public class ParsedLocalizedStringTests
    {
        [Theory]
        [ClassData(typeof(ParseTestData))]
        public void Parse_CorrectlyParsesText(string rawText, List<Segment> expectedSegments)
        {
            // Arrange
            var parsedString = new ParsedLocalizedString(rawText);

            // Act
            var actualSegments = parsedString.Segments;

            // Assert
            Assert.Equal(expectedSegments.Count, actualSegments.Count);

            for (int i = 0; i < expectedSegments.Count; i++)
            {
                Assert.True(expectedSegments[i] == actualSegments[i],
                    $"Segment at index {i} does not match expected value.\nExpected: {expectedSegments[i]}\nActual: {actualSegments[i]}");
            }
        }

        [Theory]
        [ClassData(typeof(FormatTestData))]
        public void Format_CorrectlyFormatsString(string rawText, string expectedFormattedText, string cultureString, Dictionary<string, object> argsDictionary)
        {
            // Arrange
            var parsedString = new ParsedLocalizedString(rawText);
            CultureInfo culture = CultureInfo.GetCultureInfo(cultureString);

            // Act
            string result = parsedString.Format(argsDictionary, culture);

            // Assert
            Assert.Equal(expectedFormattedText, result);
        }
    }
}