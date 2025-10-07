using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// Tests for GetReturnSchema method that generates JSON schemas for method return types
    /// </summary>
    public class ReturnSchemaTests : SchemaTestBase
    {
        public ReturnSchemaTests(ITestOutputHelper output) : base(output) { }

        #region Test Helper Methods

        // Void method
        private void VoidMethod() { }

        // Task method (async void equivalent)
        private Task TaskMethod() => Task.CompletedTask;

        // ValueTask method (async void equivalent)
        private ValueTask ValueTaskMethod() => ValueTask.CompletedTask;

        // Primitive return types
        private string StringMethod() => "test";
        private int IntMethod() => 42;
        private bool BoolMethod() => true;
        private double DoubleMethod() => 3.14;
        private DateTime DateTimeMethod() => DateTime.Now;

        // Task<T> return types (should be unwrapped)
        private Task<string> TaskStringMethod() => Task.FromResult("test");
        private Task<int> TaskIntMethod() => Task.FromResult(42);
        private Task<bool> TaskBoolMethod() => Task.FromResult(true);
        private Task<CustomReturnType> TaskCustomTypeMethod() => Task.FromResult(new CustomReturnType());

        // ValueTask<T> return types (should be unwrapped)
        private ValueTask<string> ValueTaskStringMethod() => ValueTask.FromResult("test");
        private ValueTask<int> ValueTaskIntMethod() => ValueTask.FromResult(42);
        private ValueTask<CustomReturnType> ValueTaskCustomTypeMethod() => ValueTask.FromResult(new CustomReturnType());

        // Custom types
        private CustomReturnType CustomTypeMethod() => new CustomReturnType();
        private ComplexReturnType ComplexTypeMethod() => new ComplexReturnType();

        // Collections
        private string[] StringArrayMethod() => new[] { "test" };
        private System.Collections.Generic.List<int> ListIntMethod() => new System.Collections.Generic.List<int>();
        private System.Collections.Generic.Dictionary<string, int> DictionaryMethod() => new System.Collections.Generic.Dictionary<string, int>();

        #endregion

        #region Test Classes

        public class CustomReturnType
        {
            public string Name { get; set; } = "Test";
            public int Value { get; set; } = 123;
        }

        public class ComplexReturnType
        {
            public string StringProperty { get; set; } = "test";
            public int IntProperty { get; set; } = 42;
            public CustomReturnType NestedObject { get; set; } = new CustomReturnType();
            public string[] StringArray { get; set; } = new[] { "a", "b" };
        }

        #endregion

        #region Void Return Type Tests

        [Fact]
        public void GetReturnSchema_VoidMethod_ReturnsNull()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(VoidMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.Null(schema);
        }

        [Fact]
        public void GetReturnSchema_TaskMethod_ReturnsNull()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(TaskMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.Null(schema);
        }

        [Fact]
        public void GetReturnSchema_ValueTaskMethod_ReturnsNull()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(ValueTaskMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.Null(schema);
        }

        #endregion

        #region Primitive Return Type Tests

        [Fact]
        public void GetReturnSchema_StringMethod_ReturnsStringSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(StringMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.String, schema[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_IntMethod_ReturnsIntegerSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(IntMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Integer, schema[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_BoolMethod_ReturnsBooleanSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(BoolMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Boolean, schema[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_DoubleMethod_ReturnsNumberSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(DoubleMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Number, schema[JsonSchema.Type]?.ToString());
        }

        #endregion

        #region Task<T> Unwrapping Tests

        [Fact]
        public void GetReturnSchema_TaskString_UnwrapsToStringSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(TaskStringMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.String, schema[JsonSchema.Type]?.ToString());

            // Verify it's not a schema for Task<string>, but for string
            Assert.False(schema.AsObject().ContainsKey(JsonSchema.Properties));
        }

        [Fact]
        public void GetReturnSchema_TaskInt_UnwrapsToIntegerSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(TaskIntMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Integer, schema[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_TaskBool_UnwrapsToBooleanSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(TaskBoolMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Boolean, schema[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_TaskCustomType_UnwrapsToCustomTypeSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(TaskCustomTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("Name"));
            Assert.True(properties.ContainsKey("Value"));
        }

        #endregion

        #region ValueTask<T> Unwrapping Tests

        [Fact]
        public void GetReturnSchema_ValueTaskString_UnwrapsToStringSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(ValueTaskStringMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.String, schema[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_ValueTaskInt_UnwrapsToIntegerSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(ValueTaskIntMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Integer, schema[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_ValueTaskCustomType_UnwrapsToCustomTypeSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(ValueTaskCustomTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("Name"));
            Assert.True(properties.ContainsKey("Value"));
        }

        #endregion

        #region Custom Type Tests

        [Fact]
        public void GetReturnSchema_CustomType_ReturnsValidObjectSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(CustomTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("Name"));
            Assert.True(properties.ContainsKey("Value"));

            // Verify property types
            Assert.Equal(JsonSchema.String, properties["Name"]![JsonSchema.Type]?.ToString());
            Assert.Equal(JsonSchema.Integer, properties["Value"]![JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_ComplexType_ReturnsValidNestedSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(ComplexTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey("StringProperty"));
            Assert.True(properties.ContainsKey("IntProperty"));
            Assert.True(properties.ContainsKey("NestedObject"));
            Assert.True(properties.ContainsKey("StringArray"));
        }

        #endregion

        #region Collection Tests

        [Fact]
        public void GetReturnSchema_StringArray_ReturnsArraySchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(StringArrayMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Array, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Items));

            var items = schema[JsonSchema.Items]!;
            Assert.Equal(JsonSchema.String, items[JsonSchema.Type]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_ListInt_ReturnsArraySchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(ListIntMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Array, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Items));

            var items = schema[JsonSchema.Items]!;
            Assert.Equal(JsonSchema.Integer, items[JsonSchema.Type]?.ToString());
        }

        #endregion

        #region JustRef Parameter Tests

        [Fact]
        public void GetReturnSchema_CustomType_WithJustRef_ReturnsRefSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(CustomTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo, justRef: true);

            // Assert
            Assert.NotNull(schema);
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Ref));
            Assert.Contains("CustomReturnType", schema[JsonSchema.Ref]?.ToString());
        }

        [Fact]
        public void GetReturnSchema_PrimitiveType_WithJustRef_ReturnsFullSchema()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(StringMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo, justRef: true);

            // Assert
            Assert.NotNull(schema);
            // Primitive types should be inlined even with justRef=true
            Assert.Equal(JsonSchema.String, schema[JsonSchema.Type]?.ToString());
            Assert.False(schema.AsObject().ContainsKey(JsonSchema.Ref));
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void GetReturnSchema_NullMethodInfo_ThrowsArgumentNullException()
        {
            // Arrange
            var reflector = new Reflector();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => reflector.GetReturnSchema(null!));
        }

        #endregion
    }
}
