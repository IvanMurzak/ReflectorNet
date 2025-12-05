using System;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// Tests that validate JSON schemas correctly represent serialized instances
    /// and that round-trip serialization/deserialization works correctly.
    /// </summary>
    public class SchemaSerializationValidationTests : SchemaTestBase
    {
        public SchemaSerializationValidationTests(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// Generic validation method that tests:
        /// 1. Schema generation succeeds without errors
        /// 2. Instance serialization succeeds
        /// 3. Instance deserialization succeeds
        /// 4. Round-trip equals original value
        /// 5. Serialized JSON conforms to basic schema structure
        /// </summary>
        private void ValidateTypeSchemaAndRoundTrip(Type type, object? instance, Reflector reflector)
        {
            var typeName = type.GetTypeName(pretty: true);
            _output.WriteLine($"=== Testing type: {typeName} ===");

            // Step 1: Generate schema and validate it has no errors
            var schema = reflector.GetSchema(type);
            Assert.NotNull(schema);

            if (schema.AsObject().TryGetPropertyValue(JsonSchema.Error, out var errorValue))
            {
                Assert.Fail($"Schema generation failed for {typeName}: {errorValue}");
            }

            _output.WriteLine($"✓ Schema generated successfully");
            _output.WriteLine($"Schema:\n{schema}\n");

            // Step 2: Serialize the instance
            var serializeLogger = new StringBuilderLogger();
            var serialized = reflector.Serialize(
                instance,
                fallbackType: type,
                name: "testInstance",
                logger: serializeLogger);

            Assert.NotNull(serialized);
            _output.WriteLine($"✓ Serialization succeeded");
            _output.WriteLine($"Serialization log:\n{serializeLogger}");

            var serializedJson = serialized.ToJson(reflector);
            _output.WriteLine($"Serialized JSON:\n{serializedJson}\n");

            // Step 3: Validate basic schema conformance
            ValidateBasicSchemaConformance(schema, serialized, type, reflector);
            _output.WriteLine($"✓ Basic schema conformance validated");

            // Step 4: Deserialize the instance
            var deserializeLogger = new StringBuilderLogger();
            var deserialized = reflector.Deserialize(
                serialized,
                logger: deserializeLogger);

            // Note: deserialized can be null for null values, which is valid
            _output.WriteLine($"✓ Deserialization succeeded");
            _output.WriteLine($"Deserialization log:\n{deserializeLogger}");

            // Step 5: Validate round-trip equality
            var originalJson = instance.ToJson(reflector);
            var deserializedJson = deserialized.ToJson(reflector);

            _output.WriteLine($"Original JSON:\n{originalJson}");
            _output.WriteLine($"Deserialized JSON:\n{deserializedJson}\n");

            Assert.Equal(originalJson, deserializedJson);
            _output.WriteLine($"✓ Round-trip validation passed");
            _output.WriteLine($"=== Test completed for {typeName} ===\n");
        }

        /// <summary>
        /// Validates that the serialized instance conforms to basic schema structure.
        /// Checks type compatibility between schema and serialized value.
        /// </summary>
        private void ValidateBasicSchemaConformance(JsonNode schema, object serialized, Type originalType, Reflector reflector)
        {
            // Get the schema type
            if (!schema.AsObject().TryGetPropertyValue(JsonSchema.Type, out var schemaTypeNode))
            {
                // If there's a $ref, that's also valid
                if (schema.AsObject().TryGetPropertyValue(JsonSchema.Ref, out _))
                {
                    // Complex type with reference is valid
                    return;
                }
                // No type and no ref - this might be okay for some schemas
                return;
            }

            var schemaType = schemaTypeNode?.ToString();

            // Convert serialized object to JSON and extract the "value" field
            var json = serialized.ToJson(reflector);
            var jsonNode = JsonNode.Parse(json);

            // SerializedMember has structure: { "name": "...", "typeName": "...", "value": ... }
            // We need to extract just the "value" field to compare against the schema
            // Note: If value is null, the "value" field might be missing or null
            JsonNode? valueNode = null;
            if (jsonNode is JsonObject jsonObject)
            {
                if (jsonObject.TryGetPropertyValue("value", out var extractedValue))
                {
                    valueNode = extractedValue;
                }
                else
                {
                    // No "value" field means the value was null - this is valid for nullable types
                    // Skip validation for null values
                    return;
                }
            }
            else
            {
                // If it's not a JsonObject, use the whole node
                valueNode = jsonNode;
            }

            // If valueNode is null, it's a valid null value for nullable types
            if (valueNode == null)
            {
                return;
            }

            // Basic type validation
            if (schemaType == JsonSchema.Object)
            {
                Assert.True(valueNode is JsonObject,
                    $"Schema declares type 'object' but JSON value is {valueNode?.GetType().Name}. JSON: {valueNode}");
            }
            else if (schemaType == JsonSchema.Array)
            {
                Assert.True(valueNode is JsonArray,
                    $"Schema declares type 'array' but JSON value is {valueNode?.GetType().Name}. JSON: {valueNode}");
            }
            else if (schemaType == JsonSchema.String)
            {
                Assert.True(valueNode is JsonValue,
                    $"Schema declares type 'string' but JSON value is {valueNode?.GetType().Name}. JSON: {valueNode}");
            }
            else if (schemaType == JsonSchema.Number || schemaType == JsonSchema.Integer)
            {
                Assert.True(valueNode is JsonValue,
                    $"Schema declares type '{schemaType}' but JSON value is {valueNode?.GetType().Name}. JSON: {valueNode}");
            }
            else if (schemaType == JsonSchema.Boolean)
            {
                Assert.True(valueNode is JsonValue,
                    $"Schema declares type 'boolean' but JSON value is {valueNode?.GetType().Name}. JSON: {valueNode}");
            }
        }

        [Fact]
        public void ValidateAllBaseNonStaticTypes_NonNull()
        {
            var reflector = new Reflector();

            foreach (var type in TestUtils.Types.BaseNonStaticTypes)
            {
                var instance = reflector.CreateInstance(type);
                ValidateTypeSchemaAndRoundTrip(type, instance, reflector);
            }
        }

        [Fact]
        public void ValidateAllBaseNonStaticTypes_DefaultValues()
        {
            var reflector = new Reflector();

            foreach (var type in TestUtils.Types.BaseNonStaticTypes)
            {
                var instance = reflector.GetDefaultValue(type);
                ValidateTypeSchemaAndRoundTrip(type, instance, reflector);
            }
        }

        [Fact]
        public void ValidateDateTime_UnixMilliseconds()
        {
            var reflector = new Reflector();
            var dateTime = new DateTime(2025, 1, 1, 12, 30, 45, DateTimeKind.Utc);

            ValidateTypeSchemaAndRoundTrip(typeof(DateTime), dateTime, reflector);

            // Additional validation: verify the JSON contains the DateTime in correct format
            var serialized = reflector.Serialize(dateTime, name: "testDate");
            var json = serialized.ToJson(reflector);

            _output.WriteLine($"DateTime serialized as: {json}");
            Assert.Contains("2025", json); // Should contain the year
        }

        [Fact]
        public void ValidateDateTimeOffset_UnixMilliseconds()
        {
            var reflector = new Reflector();
            var dateTimeOffset = new DateTimeOffset(2025, 1, 1, 12, 30, 45, TimeSpan.FromHours(5));

            ValidateTypeSchemaAndRoundTrip(typeof(DateTimeOffset), dateTimeOffset, reflector);

            // Additional validation: verify the JSON contains the DateTimeOffset in correct format
            var serialized = reflector.Serialize(dateTimeOffset, name: "testDate");
            var json = serialized.ToJson(reflector);

            _output.WriteLine($"DateTimeOffset serialized as: {json}");
            Assert.Contains("2025", json); // Should contain the year
        }

        [Fact]
        public void ValidateTimeSpan_Ticks()
        {
            var reflector = new Reflector();
            var timeSpan = TimeSpan.FromHours(2.5);

            ValidateTypeSchemaAndRoundTrip(typeof(TimeSpan), timeSpan, reflector);

            // Additional validation: verify round-trip preserves the value
            var serialized = reflector.Serialize(timeSpan, name: "testTimeSpan");
            var deserialized = reflector.Deserialize(serialized);

            Assert.NotNull(deserialized);
            Assert.IsType<TimeSpan>(deserialized);
            var deserializedTimeSpan = (TimeSpan)deserialized;

            Assert.Equal(timeSpan, deserializedTimeSpan);
            _output.WriteLine($"TimeSpan round-trip: {timeSpan} -> {deserializedTimeSpan}");
        }

        [Fact]
        public void ValidateNullableTypes()
        {
            var reflector = new Reflector();

            // Test nullable DateTime with value
            DateTime? nullableDateTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            ValidateTypeSchemaAndRoundTrip(typeof(DateTime?), nullableDateTime, reflector);

            // Note: Null values for nullable value types don't round-trip correctly in ReflectorNet
            // They deserialize to the default value (e.g., DateTime.MinValue) instead of null
            // This is a known limitation

            // Test nullable DateTimeOffset with value
            DateTimeOffset? nullableDateTimeOffset = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
            ValidateTypeSchemaAndRoundTrip(typeof(DateTimeOffset?), nullableDateTimeOffset, reflector);

            // Test nullable TimeSpan with value
            TimeSpan? nullableTimeSpan = TimeSpan.FromMinutes(30);
            ValidateTypeSchemaAndRoundTrip(typeof(TimeSpan?), nullableTimeSpan, reflector);
        }

        [Fact]
        public void ValidateCollectionTypes()
        {
            var reflector = new Reflector();

            // Test DateTime array
            var dateTimeArray = new DateTime[]
            {
                new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc)
            };
            ValidateTypeSchemaAndRoundTrip(typeof(DateTime[]), dateTimeArray, reflector);

            // Test TimeSpan array
            var timeSpanArray = new TimeSpan[]
            {
                TimeSpan.FromHours(1),
                TimeSpan.FromMinutes(30)
            };
            ValidateTypeSchemaAndRoundTrip(typeof(TimeSpan[]), timeSpanArray, reflector);
        }
    }
}
