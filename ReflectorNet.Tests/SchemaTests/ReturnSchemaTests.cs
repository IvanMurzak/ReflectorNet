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

        // Nullable return types
        private string? NullableStringMethod() => "test";
        private int? NullableIntMethod() => 42;
        private bool? NullableBoolMethod() => true;
        private double? NullableDoubleMethod() => 3.14;
        private DateTime? NullableDateTimeMethod() => DateTime.Now;
        private CustomReturnType? NullableCustomTypeMethod() => new CustomReturnType();
        private ComplexReturnType? NullableComplexTypeMethod() => new ComplexReturnType();
        private string[]? NullableStringArrayMethod() => new[] { "test" };

        // Task<T?> return types (nullable wrapped in Task)
        private Task<string?> TaskNullableStringMethod() => Task.FromResult<string?>("test");
        private Task<int?> TaskNullableIntMethod() => Task.FromResult<int?>(42);
        private Task<bool?> TaskNullableBoolMethod() => Task.FromResult<bool?>(true);
        private Task<CustomReturnType?> TaskNullableCustomTypeMethod() => Task.FromResult<CustomReturnType?>(new CustomReturnType());

        // ValueTask<T?> return types (nullable wrapped in ValueTask)
        private ValueTask<string?> ValueTaskNullableStringMethod() => ValueTask.FromResult<string?>("test");
        private ValueTask<int?> ValueTaskNullableIntMethod() => ValueTask.FromResult<int?>(42);
        private ValueTask<CustomReturnType?> ValueTaskNullableCustomTypeMethod() => ValueTask.FromResult<CustomReturnType?>(new CustomReturnType());

        // Task<T?>? return types (nullable T wrapped in nullable Task)
        private Task<string?>? NullableTaskNullableStringMethod() => Task.FromResult<string?>("test");
        private Task<int?>? NullableTaskNullableIntMethod() => Task.FromResult<int?>(42);
        private Task<CustomReturnType?>? NullableTaskNullableCustomTypeMethod() => Task.FromResult<CustomReturnType?>(new CustomReturnType());

        // NOTE: ValueTask<T?>? is not a valid scenario because ValueTask is a struct, not a class
        // So it cannot be made nullable in the reference type sense. We removed these tests.

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

        [Theory]
        [InlineData(nameof(VoidMethod))]
        [InlineData(nameof(TaskMethod))]
        [InlineData(nameof(ValueTaskMethod))]
        public void GetReturnSchema_VoidMethods_ReturnsNull(string methodName)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            Assert.Null(schema);
        }

        #endregion

        #region Primitive Return Type Tests

        [Theory]
        [InlineData(nameof(StringMethod), JsonSchema.String)]
        [InlineData(nameof(IntMethod), JsonSchema.Integer)]
        [InlineData(nameof(BoolMethod), JsonSchema.Boolean)]
        [InlineData(nameof(DoubleMethod), JsonSchema.Number)]
        public void GetReturnSchema_PrimitiveMethod_ReturnsCorrectSchema(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: true);
        }

        #endregion

        #region Nullable Primitive Return Type Tests

        [Theory]
        [InlineData(nameof(NullableStringMethod), JsonSchema.String)]
        [InlineData(nameof(NullableIntMethod), JsonSchema.Integer)]
        [InlineData(nameof(NullableBoolMethod), JsonSchema.Boolean)]
        [InlineData(nameof(NullableDoubleMethod), JsonSchema.Number)]
        [InlineData(nameof(NullableDateTimeMethod), JsonSchema.String)] // DateTime serialized as string
        public void GetReturnSchema_NullablePrimitiveMethod_ReturnsSchemaWithoutRequired(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: false);
        }

        #endregion

        #region Task<T> Unwrapping Tests

        [Theory]
        [InlineData(nameof(TaskStringMethod), JsonSchema.String)]
        [InlineData(nameof(TaskIntMethod), JsonSchema.Integer)]
        [InlineData(nameof(TaskBoolMethod), JsonSchema.Boolean)]
        public void GetReturnSchema_TaskPrimitive_UnwrapsCorrectly(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: true);
        }

        [Fact]
        public void GetReturnSchema_TaskCustomType_UnwrapsToCustomTypeSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: true);
        }

        #endregion

        #region Task<T?> Nullable Unwrapping Tests

        [Theory]
        [InlineData(nameof(TaskNullableStringMethod), JsonSchema.String)]
        [InlineData(nameof(TaskNullableIntMethod), JsonSchema.Integer)]
        [InlineData(nameof(TaskNullableBoolMethod), JsonSchema.Boolean)]
        public void GetReturnSchema_TaskNullablePrimitive_UnwrapsWithoutRequired(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: false);
        }

        [Fact]
        public void GetReturnSchema_TaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(TaskNullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
        }

        #endregion

        #region Nullable Task<T?> Unwrapping Tests

        [Theory]
        [InlineData(nameof(NullableTaskNullableStringMethod), JsonSchema.String)]
        [InlineData(nameof(NullableTaskNullableIntMethod), JsonSchema.Integer)]
        public void GetReturnSchema_NullableTaskNullablePrimitive_UnwrapsWithoutRequired(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: false);
        }

        [Fact]
        public void GetReturnSchema_NullableTaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableTaskNullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
        }

        #endregion

        #region ValueTask<T> Unwrapping Tests

        [Theory]
        [InlineData(nameof(ValueTaskStringMethod), JsonSchema.String)]
        [InlineData(nameof(ValueTaskIntMethod), JsonSchema.Integer)]
        public void GetReturnSchema_ValueTaskPrimitive_UnwrapsCorrectly(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: true);
        }

        [Fact]
        public void GetReturnSchema_ValueTaskCustomType_UnwrapsToCustomTypeSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: true);
        }

        #endregion

        #region ValueTask<T?> Nullable Unwrapping Tests

        [Theory]
        [InlineData(nameof(ValueTaskNullableStringMethod), JsonSchema.String)]
        [InlineData(nameof(ValueTaskNullableIntMethod), JsonSchema.Integer)]
        public void GetReturnSchema_ValueTaskNullablePrimitive_UnwrapsWithoutRequired(string methodName, string expectedType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertPrimitiveReturnSchema(schema!, expectedType, shouldBeRequired: false);
        }

        [Fact]
        public void GetReturnSchema_ValueTaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(ValueTaskNullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
        }

        #endregion

        // NOTE: Nullable ValueTask<T?> tests removed because ValueTask is a struct and cannot be made nullable
        // in the reference type sense (ValueTask<T?>? is not valid)

        #region Custom Type Tests

        [Fact]
        public void GetReturnSchema_CustomType_ReturnsValidObjectSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(CustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: true);
        }

        [Fact]
        public void GetReturnSchema_ComplexType_ReturnsValidNestedSchema()
        {
            var schema = GetReturnSchemaForMethod(nameof(ComplexTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "StringProperty", "IntProperty", "NestedObject", "StringArray" }, shouldBeRequired: true);
        }

        #endregion

        #region Nullable Custom Type Tests

        [Fact]
        public void GetReturnSchema_NullableCustomType_ReturnsValidObjectSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableCustomTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "Name", "Value" }, shouldBeRequired: false);
        }

        [Fact]
        public void GetReturnSchema_NullableComplexType_ReturnsValidNestedSchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableComplexTypeMethod));
            AssertCustomTypeReturnSchema(schema!, new[] { "StringProperty", "IntProperty", "NestedObject", "StringArray" }, shouldBeRequired: false);
        }

        #endregion

        #region Collection Tests

        [Theory]
        [InlineData(nameof(StringArrayMethod), JsonSchema.String)]
        [InlineData(nameof(ListIntMethod), JsonSchema.Integer)]
        public void GetReturnSchema_CollectionTypes_ReturnsArraySchema(string methodName, string expectedItemType)
        {
            var schema = GetReturnSchemaForMethod(methodName);
            AssertArrayReturnSchema(schema!, expectedItemType, shouldBeRequired: true);
        }

        #endregion

        #region Nullable Collection Tests

        [Fact]
        public void GetReturnSchema_NullableStringArray_ReturnsArraySchemaWithoutRequired()
        {
            var schema = GetReturnSchemaForMethod(nameof(NullableStringArrayMethod));
            AssertArrayReturnSchema(schema!, JsonSchema.String, shouldBeRequired: false);
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain a $ref to CustomReturnType
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.True(resultSchema.ContainsKey(JsonSchema.Ref));
            Assert.Contains("CustomReturnType", resultSchema[JsonSchema.Ref]?.ToString());

            // $defs should exist with the full type definition
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // Primitive types should be inlined even with justRef=true
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.Equal(JsonSchema.String, resultSchema[JsonSchema.Type]?.ToString());
            Assert.False(resultSchema.ContainsKey(JsonSchema.Ref));
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
