
using Hcs.LightI18n.Core;
namespace Hcs.LightI18n.Tests
{
    public class LocalizationServiceStaticTests
    {
        // 測試用物件類別
        private class TestObject(string stringProp, int intProp, bool boolProp)
        {
            public string StringProp { get; set; } = stringProp;
            public int IntProp { get; set; } = intProp;
            public bool BoolProp { get; } = boolProp;
            public object? NullProp { get; set; }
            public decimal DecimalProp { get; } = 123.45m;
            private string PrivateProp { get; set; } = "私有值";
            internal string InternalProp { get; set; } = "內部值";
        }

        // 測試用空物件類別 (無任何屬性)
        private class EmptyObject { }

        // 測試用物件類別 (只有私有成員)
        private class ObjectWithOnlyPrivateMembers
        {
            private readonly string _name = "僅私有成員";
            public string GetName() => _name; // 方法，非屬性
        }

        [Xunit.Fact(DisplayName = "GetObjectAsDictionary 應能將簡單物件的公共屬性轉換為字典 (Static Method)")]
        public void GetObjectAsDictionary_ShouldConvertSimplePublicPropertiesToDictionary()
        {
            // Arrange
            var testObj = new TestObject("哈囉", 123, true)
            {
                NullProp = null // 設定一個 null 屬性值
            };

            // Act
            // 直接透過類別名稱呼叫 static 方法
            var result = new Dictionary<string, object>();
            LocalizationService.AppendObjectToDictionary(testObj, result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count); // StringProp, IntProp, BoolProp, NullProp, DecimalProp

            Assert.True(result.ContainsKey("StringProp"));
            Assert.Equal("哈囉", result["StringProp"]);

            Assert.True(result.ContainsKey("IntProp"));
            Assert.Equal(123, result["IntProp"]);

            Assert.True(result.ContainsKey("BoolProp"));
            Assert.Equal(true, result["BoolProp"]);

            Assert.True(result.ContainsKey("NullProp"));
            Assert.Null(result["NullProp"]); // 應正確處理 null 屬性值

            Assert.True(result.ContainsKey("DecimalProp"));
            Assert.Equal(123.45m, result["DecimalProp"]);

            // 確保私有和 Internal 屬性被忽略
            Assert.False(result.ContainsKey("PrivateProp"));
            Assert.False(result.ContainsKey("InternalProp"));
        }

        [Xunit.Fact(DisplayName = "GetObjectAsDictionary 應處理 Null 輸入並返回空字典 (Static Method)")]
        public void GetObjectAsDictionary_ShouldReturnEmptyDictionaryForNullInput()
        {
            // Arrange
            object? nullObj = null;

            // Act
            var result = new Dictionary<string, object>();
            LocalizationService.AppendObjectToDictionary(nullObj, result);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // 對於 null 輸入，應返回空字典
        }

        [Xunit.Fact(DisplayName = "GetObjectAsDictionary 應處理沒有公共屬性的物件並返回空字典 (Static Method)")]
        public void GetObjectAsDictionary_ShouldReturnEmptyDictionaryForObjectWithNoPublicProperties()
        {
            // Arrange
            var emptyObj = new EmptyObject();

            // Act
            var result = new Dictionary<string, object>();
            LocalizationService.AppendObjectToDictionary(emptyObj, result);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // 對於沒有公共屬性的物件，應返回空字典
        }

        [Xunit.Fact(DisplayName = "GetObjectAsDictionary 應處理只有私有成員的物件並返回空字典 (Static Method)")]
        public void GetObjectAsDictionary_ShouldReturnEmptyDictionaryForObjectWithOnlyPrivateMembers()
        {
            // Arrange
            var obj = new ObjectWithOnlyPrivateMembers();

            // Act
            var result = new Dictionary<string, object>();
            LocalizationService.AppendObjectToDictionary(obj, result);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result); // 對於只有私有成員的物件，應返回空字典
        }

        // 測試用繼承物件類別
        private class DerivedTestObject : TestObject
        {
            public string DerivedProp { get; set; } // 衍生類別特有的公共屬性
            public DerivedTestObject(string stringProp, int intProp, bool boolProp, string derivedProp)
                : base(stringProp, intProp, boolProp)
            {
                DerivedProp = derivedProp;
            }
        }

        [Xunit.Fact(DisplayName = "GetObjectAsDictionary 應處理繼承物件並包含所有公共屬性 (Static Method)")]
        public void GetObjectAsDictionary_ShouldHandleInheritedObjectsAndIncludeAllPublicProperties()
        {
            // Arrange
            var derivedObj = new DerivedTestObject("衍生字串", 456, false, "額外屬性")
            {
                NullProp = "非空值" // 覆寫基底類別的屬性值
            };

            // Act
            var result = new Dictionary<string, object>();
            LocalizationService.AppendObjectToDictionary(derivedObj, result);

            // Assert
            Assert.NotNull(result);
            // 來自基底類別的 5 個公共屬性 (StringProp, IntProp, BoolProp, NullProp, DecimalProp)
            // 加上來自衍生類別的 1 個公共屬性 (DerivedProp)
            Assert.Equal(6, result.Count);

            Assert.True(result.ContainsKey("StringProp"));
            Assert.Equal("衍生字串", result["StringProp"]);

            Assert.True(result.ContainsKey("IntProp"));
            Assert.Equal(456, result["IntProp"]);

            Assert.True(result.ContainsKey("BoolProp"));
            Assert.Equal(false, result["BoolProp"]);

            Assert.True(result.ContainsKey("NullProp"));
            Assert.Equal("非空值", result["NullProp"]);

            Assert.True(result.ContainsKey("DecimalProp"));
            Assert.Equal(123.45m, result["DecimalProp"]);

            Assert.True(result.ContainsKey("DerivedProp"));
            Assert.Equal("額外屬性", result["DerivedProp"]);
        }

        // 測試不同資料型別的屬性
        private class VariousTypesObject
        {
            public float FloatProp { get; set; }
            public double DoubleProp { get; set; }
            public Guid GuidProp { get; set; }
            public DateTime DateTimeProp { get; set; }
            public TimeSpan TimeSpanProp { get; set; }
        }

        [Xunit.Fact(DisplayName = "GetObjectAsDictionary 應正確處理不同資料型別的公共屬性 (Static Method)")]
        public void GetObjectAsDictionary_ShouldHandleVariousDataTypesCorrectly()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var dateTime = DateTime.Now;
            var timeSpan = TimeSpan.FromHours(2);
            var obj = new VariousTypesObject
            {
                FloatProp = 1.23f,
                DoubleProp = 4.56,
                GuidProp = guid,
                DateTimeProp = dateTime,
                TimeSpanProp = timeSpan
            };

            // Act
            var result = new Dictionary<string, object>();
            LocalizationService.AppendObjectToDictionary(obj, result);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);

            Assert.True(result.ContainsKey("FloatProp"));
            Assert.Equal(1.23f, result["FloatProp"]);

            Assert.True(result.ContainsKey("DoubleProp"));
            Assert.Equal(4.56, result["DoubleProp"]);

            Assert.True(result.ContainsKey("GuidProp"));
            Assert.Equal(guid, result["GuidProp"]);

            Assert.True(result.ContainsKey("DateTimeProp"));
            Assert.Equal(dateTime, result["DateTimeProp"]);

            Assert.True(result.ContainsKey("TimeSpanProp"));
            Assert.Equal(timeSpan, result["TimeSpanProp"]);
        }

        [Xunit.Fact(DisplayName = "GetObjectAsDictionary 應正確合併值")]
        public void GetObjectAsDictionary_ShouldMergeValues()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var dateTime = DateTime.Now;
            var timeSpan = TimeSpan.FromHours(2);
            // Act
            var result = new Dictionary<string, object>();
            LocalizationService.AppendObjectToDictionary(new
            {
                FloatProp = 1.23f
            }, result);
            LocalizationService.AppendObjectToDictionary(new
            {
                DoubleProp = 4.56
            }, result);
            LocalizationService.AppendObjectToDictionary(new
            {
                GuidProp = guid,
                DateTimeProp = dateTime,
                TimeSpanProp = timeSpan
            }, result);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);

            Assert.True(result.ContainsKey("FloatProp"));
            Assert.Equal(1.23f, result["FloatProp"]);

            Assert.True(result.ContainsKey("DoubleProp"));
            Assert.Equal(4.56, result["DoubleProp"]);

            Assert.True(result.ContainsKey("GuidProp"));
            Assert.Equal(guid, result["GuidProp"]);

            Assert.True(result.ContainsKey("DateTimeProp"));
            Assert.Equal(dateTime, result["DateTimeProp"]);

            Assert.True(result.ContainsKey("TimeSpanProp"));
            Assert.Equal(timeSpan, result["TimeSpanProp"]);
        }
    }
}