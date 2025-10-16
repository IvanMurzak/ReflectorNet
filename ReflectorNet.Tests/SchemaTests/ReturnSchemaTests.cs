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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Required));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.String, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            var required = schema[JsonSchema.Required]!.AsArray();
            Assert.Single(required);
            Assert.Equal(JsonSchema.Result, required[0]?.ToString());
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Integer, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Boolean, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Number, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());
        }

        #endregion

        #region Nullable Primitive Return Type Tests

        [Fact]
        public void GetReturnSchema_NullableStringMethod_ReturnsStringSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableStringMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.String, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_NullableIntMethod_ReturnsIntegerSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableIntMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Integer, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_NullableBoolMethod_ReturnsBooleanSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableBoolMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Boolean, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_NullableDoubleMethod_ReturnsNumberSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableDoubleMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Number, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_NullableDateTimeMethod_ReturnsStringSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableDateTimeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            // DateTime is typically serialized as string in JSON
            Assert.Equal(JsonSchema.String, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.String, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Integer, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Boolean, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());
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
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain a $ref to the CustomReturnType in $defs
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.True(resultSchema.ContainsKey(JsonSchema.Ref) || resultSchema.ContainsKey(JsonSchema.Properties));

            // If it's a ref, verify $defs exists
            if (resultSchema.ContainsKey(JsonSchema.Ref))
            {
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            // If it's not a ref, verify it has the expected properties
            else
            {
                var nestedProperties = resultSchema[JsonSchema.Properties]!.AsObject();
                Assert.True(nestedProperties.ContainsKey("Name"));
                Assert.True(nestedProperties.ContainsKey("Value"));
            }
        }

        #endregion

        #region Task<T?> Nullable Unwrapping Tests

        [Fact]
        public void GetReturnSchema_TaskNullableString_UnwrapsToStringSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(TaskNullableStringMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.String, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_TaskNullableInt_UnwrapsToIntegerSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(TaskNullableIntMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Integer, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_TaskNullableBool_UnwrapsToBooleanSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(TaskNullableBoolMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Boolean, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_TaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(TaskNullableCustomTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain a $ref to the CustomReturnType in $defs
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.True(resultSchema.ContainsKey(JsonSchema.Ref) || resultSchema.ContainsKey(JsonSchema.Properties));

            // If it's a ref, verify $defs exists
            if (resultSchema.ContainsKey(JsonSchema.Ref))
            {
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            // If it's not a ref, verify it has the expected properties
            else
            {
                var nestedProperties = resultSchema[JsonSchema.Properties]!.AsObject();
                Assert.True(nestedProperties.ContainsKey("Name"));
                Assert.True(nestedProperties.ContainsKey("Value"));
            }

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        #endregion

        #region Nullable Task<T?> Unwrapping Tests

        [Fact]
        public void GetReturnSchema_NullableTaskNullableString_UnwrapsToStringSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableTaskNullableStringMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.String, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_NullableTaskNullableInt_UnwrapsToIntegerSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableTaskNullableIntMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Integer, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_NullableTaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableTaskNullableCustomTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain a $ref to the CustomReturnType in $defs
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.True(resultSchema.ContainsKey(JsonSchema.Ref) || resultSchema.ContainsKey(JsonSchema.Properties));

            // If it's a ref, verify $defs exists
            if (resultSchema.ContainsKey(JsonSchema.Ref))
            {
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            // If it's not a ref, verify it has the expected properties
            else
            {
                var nestedProperties = resultSchema[JsonSchema.Properties]!.AsObject();
                Assert.True(nestedProperties.ContainsKey("Name"));
                Assert.True(nestedProperties.ContainsKey("Value"));
            }

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.String, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Integer, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());
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
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain a $ref to the CustomReturnType in $defs
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.True(resultSchema.ContainsKey(JsonSchema.Ref) || resultSchema.ContainsKey(JsonSchema.Properties));
        }

        #endregion

        #region ValueTask<T?> Nullable Unwrapping Tests

        [Fact]
        public void GetReturnSchema_ValueTaskNullableString_UnwrapsToStringSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(ValueTaskNullableStringMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.String, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_ValueTaskNullableInt_UnwrapsToIntegerSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(ValueTaskNullableIntMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));
            Assert.Equal(JsonSchema.Integer, properties[JsonSchema.Result]![JsonSchema.Type]?.ToString());

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_ValueTaskNullableCustomType_UnwrapsToCustomTypeSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(ValueTaskNullableCustomTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain a $ref to the CustomReturnType in $defs
            var resultSchema = properties[JsonSchema.Result]!.AsObject();
            Assert.True(resultSchema.ContainsKey(JsonSchema.Ref) || resultSchema.ContainsKey(JsonSchema.Properties));

            // If it's a ref, verify $defs exists
            if (resultSchema.ContainsKey(JsonSchema.Ref))
            {
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            // If it's not a ref, verify it has the expected properties
            else
            {
                var nestedProperties = resultSchema[JsonSchema.Properties]!.AsObject();
                Assert.True(nestedProperties.ContainsKey("Name"));
                Assert.True(nestedProperties.ContainsKey("Value"));
            }

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        #endregion

        // NOTE: Nullable ValueTask<T?> tests removed because ValueTask is a struct and cannot be made nullable
        // in the reference type sense (ValueTask<T?>? is not valid)

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
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain the CustomReturnType schema (either inline or ref)
            var resultSchema = properties[JsonSchema.Result]!.AsObject();

            // It could be a $ref or an inline schema
            if (resultSchema.ContainsKey(JsonSchema.Ref))
            {
                // If it's a ref, $defs should exist with the type definition
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else
            {
                // If it's inline, verify the properties
                Assert.True(resultSchema.ContainsKey(JsonSchema.Properties));
                var customTypeProperties = resultSchema[JsonSchema.Properties]!.AsObject();
                Assert.True(customTypeProperties.ContainsKey("Name"));
                Assert.True(customTypeProperties.ContainsKey("Value"));
            }
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
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain the ComplexReturnType schema
            var resultSchema = properties[JsonSchema.Result]!.AsObject();

            // It could be a $ref or an inline schema
            if (resultSchema.ContainsKey(JsonSchema.Ref))
            {
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else
            {
                Assert.True(resultSchema.ContainsKey(JsonSchema.Properties));
                var complexTypeProperties = resultSchema[JsonSchema.Properties]!.AsObject();
                Assert.True(complexTypeProperties.ContainsKey("StringProperty"));
                Assert.True(complexTypeProperties.ContainsKey("IntProperty"));
                Assert.True(complexTypeProperties.ContainsKey("NestedObject"));
                Assert.True(complexTypeProperties.ContainsKey("StringArray"));
            }
        }

        #endregion

        #region Nullable Custom Type Tests

        [Fact]
        public void GetReturnSchema_NullableCustomType_ReturnsValidObjectSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableCustomTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain the CustomReturnType schema (either inline or ref)
            var resultSchema = properties[JsonSchema.Result]!.AsObject();

            // It could be a $ref or an inline schema
            if (resultSchema.ContainsKey(JsonSchema.Ref))
            {
                // If it's a ref, $defs should exist with the type definition
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else
            {
                // If it's inline, verify the properties
                Assert.True(resultSchema.ContainsKey(JsonSchema.Properties));
                var customTypeProperties = resultSchema[JsonSchema.Properties]!.AsObject();
                Assert.True(customTypeProperties.ContainsKey("Name"));
                Assert.True(customTypeProperties.ContainsKey("Value"));
            }

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
        }

        [Fact]
        public void GetReturnSchema_NullableComplexType_ReturnsValidNestedSchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableComplexTypeMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should contain the ComplexReturnType schema
            var resultSchema = properties[JsonSchema.Result]!.AsObject();

            // It could be a $ref or an inline schema
            if (resultSchema.ContainsKey(JsonSchema.Ref))
            {
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else
            {
                Assert.True(resultSchema.ContainsKey(JsonSchema.Properties));
                var complexTypeProperties = resultSchema[JsonSchema.Properties]!.AsObject();
                Assert.True(complexTypeProperties.ContainsKey("StringProperty"));
                Assert.True(complexTypeProperties.ContainsKey("IntProperty"));
                Assert.True(complexTypeProperties.ContainsKey("NestedObject"));
                Assert.True(complexTypeProperties.ContainsKey("StringArray"));
            }

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should be an array schema (either inline or ref)
            var resultNode = properties[JsonSchema.Result]!;

            if (resultNode is JsonObject resultSchema && resultSchema.ContainsKey(JsonSchema.Ref))
            {
                // If it's a ref, verify $defs contains the array schema
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else if (resultNode is JsonObject resultInlineSchema)
            {
                // If it's inline, verify it's an array schema
                Assert.Equal(JsonSchema.Array, resultInlineSchema[JsonSchema.Type]?.ToString());
                Assert.True(resultInlineSchema.ContainsKey(JsonSchema.Items));

                var items = resultInlineSchema[JsonSchema.Items]!;
                Assert.Equal(JsonSchema.String, items[JsonSchema.Type]?.ToString());
            }
            else
            {
                Assert.Fail("Expected result to be a schema object");
            }
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
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should be an array schema (either inline or ref)
            var resultNode = properties[JsonSchema.Result]!;

            if (resultNode is JsonObject resultSchema && resultSchema.ContainsKey(JsonSchema.Ref))
            {
                // If it's a ref, verify $defs contains the array schema
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else if (resultNode is JsonObject resultInlineSchema)
            {
                // If it's inline, verify it's an array schema
                Assert.Equal(JsonSchema.Array, resultInlineSchema[JsonSchema.Type]?.ToString());
                Assert.True(resultInlineSchema.ContainsKey(JsonSchema.Items));

                var items = resultInlineSchema[JsonSchema.Items]!;
                Assert.Equal(JsonSchema.Integer, items[JsonSchema.Type]?.ToString());
            }
            else
            {
                Assert.Fail("Expected result to be a schema object");
            }
        }

        #endregion

        #region Nullable Collection Tests

        [Fact]
        public void GetReturnSchema_NullableStringArray_ReturnsArraySchemaWithoutRequired()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = GetType().GetMethod(nameof(NullableStringArrayMethod), BindingFlags.NonPublic | BindingFlags.Instance)!;

            // Act
            var schema = reflector.GetReturnSchema(methodInfo);

            // Assert
            Assert.NotNull(schema);
            Assert.Equal(JsonSchema.Object, schema[JsonSchema.Type]?.ToString());
            Assert.True(schema.AsObject().ContainsKey(JsonSchema.Properties));

            var properties = schema[JsonSchema.Properties]!.AsObject();
            Assert.True(properties.ContainsKey(JsonSchema.Result));

            // The result property should be an array schema (either inline or ref)
            var resultNode = properties[JsonSchema.Result]!;

            if (resultNode is JsonObject resultSchema && resultSchema.ContainsKey(JsonSchema.Ref))
            {
                // If it's a ref, verify $defs contains the array schema
                Assert.True(schema.AsObject().ContainsKey(JsonSchema.Defs));
            }
            else if (resultNode is JsonObject resultInlineSchema)
            {
                // If it's inline, verify it's an array schema
                Assert.Equal(JsonSchema.Array, resultInlineSchema[JsonSchema.Type]?.ToString());
                Assert.True(resultInlineSchema.ContainsKey(JsonSchema.Items));

                var items = resultInlineSchema[JsonSchema.Items]!;
                Assert.Equal(JsonSchema.String, items[JsonSchema.Type]?.ToString());
            }
            else
            {
                Assert.Fail("Expected result to be a schema object");
            }

            // Verify that "result" is NOT in the required array for nullable return types
            if (schema.AsObject().ContainsKey(JsonSchema.Required))
            {
                var required = schema[JsonSchema.Required]!.AsArray();
                Assert.DoesNotContain(required, r => r?.ToString() == JsonSchema.Result);
            }
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
