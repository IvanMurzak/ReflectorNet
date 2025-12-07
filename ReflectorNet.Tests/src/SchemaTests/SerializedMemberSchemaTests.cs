using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// Tests to validate that SerializedMember JSON schema correctly represents
    /// the actual serialized JSON for various value types.
    /// </summary>
    public class SerializedMemberSchemaTests : BaseTest
    {
        public SerializedMemberSchemaTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SerializedMember_Schema_Value_Should_Accept_Any_JsonType()
        {
            // Arrange
            var reflector = new Reflector();
            var schema = reflector.GetSchema<SerializedMember>();

            // Assert - Check that schema was generated
            Assert.NotNull(schema);

            // Get the value property schema
            var schemaObj = schema.AsObject();
            Assert.True(schemaObj.TryGetPropertyValue(JsonSchema.Properties, out var propertiesNode));
            var properties = propertiesNode!.AsObject();
            Assert.True(properties.TryGetPropertyValue(SerializedMember.ValueName, out var valueSchemaNode));
            var valueSchema = valueSchemaNode!.AsObject();

            // The value schema should NOT have a "type" constraint
            // This allows it to accept any JSON value type
            Assert.False(valueSchema.TryGetPropertyValue(JsonSchema.Type, out _),
                "The 'value' property schema should not have a 'type' constraint to allow any JSON value type");

            // It should have a description
            Assert.True(valueSchema.TryGetPropertyValue(JsonSchema.Description, out var descriptionNode));
            Assert.NotNull(descriptionNode);

            _output.WriteLine($"✓ SerializedMember schema allows 'value' to be any JSON type");
            _output.WriteLine($"Schema:\n{schema.ToJsonString()}");
        }

        [Theory]
        [InlineData("test string", "string")]
        [InlineData(42, "number")]
        [InlineData(3.14, "number")]
        [InlineData(true, "boolean")]
        [InlineData(false, "boolean")]
        public void SerializedMember_Should_Serialize_PrimitiveTypes_Correctly(object value, string expectedJsonType)
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(value, name: "testValue");
            var json = serialized.ToJson(reflector);

            // Parse and validate JSON structure
            var jsonNode = JsonNode.Parse(json);
            Assert.NotNull(jsonNode);

            var jsonObj = jsonNode!.AsObject();
            Assert.True(jsonObj.TryGetPropertyValue(SerializedMember.ValueName, out var valueNode));
            Assert.NotNull(valueNode);

            // Validate the value type matches expected
            var actualJsonType = GetJsonValueType(valueNode);
            _output.WriteLine($"Value: {value}, Expected type: {expectedJsonType}, Actual type: {actualJsonType}");
            _output.WriteLine($"Serialized JSON: {json}");

            Assert.Equal(expectedJsonType, actualJsonType);
        }

        [Fact]
        public void SerializedMember_Should_Serialize_Null_Correctly()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(null, typeof(string), name: "testValue");
            var json = serialized.ToJson(reflector);

            // Parse and validate JSON structure
            var jsonNode = JsonNode.Parse(json);
            Assert.NotNull(jsonNode);

            var jsonObj = jsonNode!.AsObject();

            // For null values, the value property should be absent or null
            if (jsonObj.TryGetPropertyValue(SerializedMember.ValueName, out var valueNode))
            {
                Assert.Null(valueNode);
            }

            _output.WriteLine($"Serialized null value JSON: {json}");
        }

        [Fact]
        public void SerializedMember_Should_Serialize_Array_Correctly()
        {
            // Arrange
            var reflector = new Reflector();
            var array = new[] { 1, 2, 3 };

            // Act
            var serialized = reflector.Serialize(array, name: "testArray");
            var json = serialized.ToJson(reflector);

            // Parse and validate JSON structure
            var jsonNode = JsonNode.Parse(json);
            Assert.NotNull(jsonNode);

            var jsonObj = jsonNode!.AsObject();
            Assert.True(jsonObj.TryGetPropertyValue(SerializedMember.ValueName, out var valueNode));
            Assert.NotNull(valueNode);

            // The value should be a JSON array
            Assert.IsType<JsonArray>(valueNode);

            _output.WriteLine($"Serialized array JSON: {json}");
        }

        [Fact]
        public void SerializedMember_Should_Serialize_Object_Correctly()
        {
            // Arrange
            var reflector = new Reflector();
            var obj = new TestClass { Name = "Test", Value = 123 };

            // Act
            var serialized = reflector.Serialize(obj, name: "testObject");
            var json = serialized.ToJson(reflector);

            // Parse and validate JSON structure
            var jsonNode = JsonNode.Parse(json);
            Assert.NotNull(jsonNode);

            var jsonObj = jsonNode!.AsObject();
            Assert.True(jsonObj.TryGetPropertyValue(SerializedMember.ValueName, out var valueNode));
            Assert.NotNull(valueNode);

            // For complex objects, the value is typically an empty object {}
            // and the actual data is in props/fields
            Assert.IsType<JsonObject>(valueNode);

            _output.WriteLine($"Serialized object JSON: {json}");
        }

        [Fact]
        public void SerializedMember_RoundTrip_Should_Preserve_AllValueTypes()
        {
            // Arrange
            var reflector = new Reflector();
            var testValues = new object[]
            {
                "string value",
                42,
                3.14159,
                true,
                false
            };

            foreach (var originalValue in testValues)
            {
                // Act
                var serialized = reflector.Serialize(originalValue, name: "test");
                var deserialized = reflector.Deserialize(serialized);

                // Assert
                var originalJson = originalValue.ToJson(reflector);
                var deserializedJson = deserialized.ToJson(reflector);

                _output.WriteLine($"Original: {originalValue} ({originalValue.GetType().Name})");
                _output.WriteLine($"Original JSON: {originalJson}");
                _output.WriteLine($"Deserialized JSON: {deserializedJson}");

                Assert.Equal(originalJson, deserializedJson);
            }
        }

        /// <summary>
        /// Helper method to determine the JSON value type from a JsonNode
        /// </summary>
        private string GetJsonValueType(JsonNode node)
        {
            return node switch
            {
                JsonObject => "object",
                JsonArray => "array",
                JsonValue value when value.TryGetValue<string>(out _) => "string",
                JsonValue value when value.TryGetValue<bool>(out _) => "boolean",
                JsonValue value when IsNumericJsonValue(value) => "number",
                _ => "unknown"
            };
        }

        /// <summary>
        /// Helper method to check if a JsonValue represents a numeric type
        /// </summary>
        private bool IsNumericJsonValue(JsonValue value)
        {
            return value.TryGetValue<int>(out _) ||
                   value.TryGetValue<long>(out _) ||
                   value.TryGetValue<float>(out _) ||
                   value.TryGetValue<double>(out _) ||
                   value.TryGetValue<decimal>(out _) ||
                   value.TryGetValue<byte>(out _) ||
                   value.TryGetValue<short>(out _) ||
                   value.TryGetValue<uint>(out _) ||
                   value.TryGetValue<ulong>(out _) ||
                   value.TryGetValue<ushort>(out _) ||
                   value.TryGetValue<sbyte>(out _);
        }

        private class TestClass
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }
    }
}
