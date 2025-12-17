using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class MultiDimensionalArrayTests : SchemaTestBase
    {
        public MultiDimensionalArrayTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GetSchema_TwoDimensionalArray_ShouldReturnArrayOfArrays()
        {
            var reflector = new Reflector();
            var type = typeof(int[,]);
            var schema = reflector.GetSchema(type);

            _output.WriteLine(schema.ToString());

            Assert.Equal(JsonSchema.Array, schema[JsonSchema.Type]?.ToString());

            var items = schema[JsonSchema.Items];
            Assert.NotNull(items);

            // Current behavior (likely): items type is integer
            // Desired behavior: items type is array (of integers)

            Assert.Equal(JsonSchema.Array, items[JsonSchema.Type]?.ToString());

            var innerItems = items[JsonSchema.Items];
            Assert.NotNull(innerItems);
            Assert.Equal(JsonSchema.Integer, innerItems[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetSchema_ThreeDimensionalArray_ShouldReturnArrayOfArrayOfArrays()
        {
            var reflector = new Reflector();
            var type = typeof(int[,,]);
            var schema = reflector.GetSchema(type);

            _output.WriteLine(schema.ToString());

            Assert.Equal(JsonSchema.Array, schema[JsonSchema.Type]?.ToString());

            var items1 = schema[JsonSchema.Items]; // Rank 2 array
            Assert.NotNull(items1);
            Assert.Equal(JsonSchema.Array, items1[JsonSchema.Type]?.ToString());

            var items2 = items1[JsonSchema.Items]; // Rank 1 array
            Assert.NotNull(items2);
            Assert.Equal(JsonSchema.Array, items2[JsonSchema.Type]?.ToString());

            var items3 = items2[JsonSchema.Items]; // Element
            Assert.NotNull(items3);
            Assert.Equal(JsonSchema.Integer, items3[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetSchema_FourDimensionalArray_ShouldReturnArrayOfArrayOfArrayOfArrays()
        {
            var reflector = new Reflector();
            var type = typeof(int[,,,]);
            var schema = reflector.GetSchema(type);

            _output.WriteLine(schema.ToString());

            Assert.Equal(JsonSchema.Array, schema[JsonSchema.Type]?.ToString());

            var items1 = schema[JsonSchema.Items]; // Rank 3 array
            Assert.NotNull(items1);
            Assert.Equal(JsonSchema.Array, items1[JsonSchema.Type]?.ToString());

            var items2 = items1[JsonSchema.Items]; // Rank 2 array
            Assert.NotNull(items2);
            Assert.Equal(JsonSchema.Array, items2[JsonSchema.Type]?.ToString());

            var items3 = items2[JsonSchema.Items]; // Rank 1 array
            Assert.NotNull(items3);
            Assert.Equal(JsonSchema.Array, items3[JsonSchema.Type]?.ToString());

            var items4 = items3[JsonSchema.Items]; // Element
            Assert.NotNull(items4);
            Assert.Equal(JsonSchema.Integer, items4[JsonSchema.Type]?.ToString());
        }
    }
}
