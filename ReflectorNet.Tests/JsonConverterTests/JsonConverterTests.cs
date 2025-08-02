using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class JsonConverterTests : BaseTest
    {
        public JsonConverterTests(ITestOutputHelper output) : base(output) { }

        void BackAndForthTest(object sourceInstance)
        {
            // Arrange
            var sourceType = sourceInstance.GetType();

            // Act
            var sourceJson = JsonUtils.ToJson(sourceInstance);
            _output.WriteLine($"Source {sourceType.GetTypeShortName()}: {sourceJson}");
            _output.WriteLine("------------------------------------------------------");

            var parsedInstance = JsonUtils.Deserialize(sourceJson, sourceType);
            var parsedJson = JsonUtils.ToJson(parsedInstance);
            _output.WriteLine($"Parsed {sourceType.GetTypeShortName()}: {parsedJson}");

            // Assert
            Assert.Equal(sourceJson, parsedJson);
        }

        [Fact]
        public void JsonConverter_Back_and_Forth__Field_Property()
        {
            BackAndForthTest(new WrapperClass<ParentClass.NestedClass[]>
            {
                ValueField = new[]
                {
                    new ParentClass.NestedClass { NestedField = "First Field", NestedProperty = "First Property" },
                    new ParentClass.NestedClass { NestedField = "Second Field", NestedProperty = "Second Property" }
                },
                ValueProperty = new[]
                 {
                     new ParentClass.NestedClass { NestedField = "Third Field", NestedProperty = "Third Property" },
                     new ParentClass.NestedClass { NestedField = "Fourth Field", NestedProperty = "Fourth Property" }
                 }
            });
        }

        [Fact]
        public void JsonConverter_Back_and_Forth__FieldNull_PropertyNull()
        {
            BackAndForthTest(new WrapperClass<ParentClass.NestedClass?>
            {
                ValueField = null,
                ValueProperty = null
            });
        }

        [Fact]
        public void JsonConverter_Back_and_Forth__Field_PropertyNull()
        {
            BackAndForthTest(new WrapperClass<ParentClass.NestedClass?>
            {
                ValueField = new ParentClass.NestedClass { NestedField = "Field", NestedProperty = "Property" },
                ValueProperty = null
            });
        }
    }
}
