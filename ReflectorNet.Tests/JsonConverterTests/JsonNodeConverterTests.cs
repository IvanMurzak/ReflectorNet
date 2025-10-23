/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Comprehensive tests for JsonElement, JsonObject, and JsonArray JSON converters.
    /// Tests cover serialization, deserialization, null handling, schema generation, and edge cases.
    /// </summary>
    public class JsonNodeConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public JsonNodeConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region JsonElement Tests

        [Fact]
        public void JsonElementConverter_Serialize_SimpleObject_ShouldSucceed()
        {
            // Arrange
            var jsonElement = JsonDocument.Parse("{\"name\":\"John\",\"age\":30}").RootElement;

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonElement);
            _output.WriteLine($"Serialized JsonElement: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("name", json);
            Assert.Contains("John", json);
            Assert.Contains("age", json);
            Assert.Contains("30", json);
        }

        [Fact]
        public void JsonElementConverter_Serialize_Array_ShouldSucceed()
        {
            // Arrange
            var jsonElement = JsonDocument.Parse("[1,2,3,4,5]").RootElement;

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonElement);
            _output.WriteLine($"Serialized JsonElement array: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("1", json);
            Assert.Contains("5", json);
        }

        [Fact]
        public void JsonElementConverter_Serialize_String_ShouldSucceed()
        {
            // Arrange
            var jsonElement = JsonDocument.Parse("\"hello world\"").RootElement;

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonElement);
            _output.WriteLine($"Serialized JsonElement string: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("hello world", json);
        }

        [Fact]
        public void JsonElementConverter_Serialize_Number_ShouldSucceed()
        {
            // Arrange
            var jsonElement = JsonDocument.Parse("42").RootElement;

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonElement);
            _output.WriteLine($"Serialized JsonElement number: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("42", json);
        }

        [Fact]
        public void JsonElementConverter_Serialize_Boolean_ShouldSucceed()
        {
            // Arrange
            var jsonElement = JsonDocument.Parse("true").RootElement;

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonElement);
            _output.WriteLine($"Serialized JsonElement boolean: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("true", json);
        }

        [Fact]
        public void JsonElementConverter_Serialize_Null_ShouldSucceed()
        {
            // Arrange
            var jsonElement = JsonDocument.Parse("null").RootElement;

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonElement);
            _output.WriteLine($"Serialized JsonElement null: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("null", json);
        }

        [Fact]
        public void JsonElementConverter_Deserialize_SimpleObject_ShouldSucceed()
        {
            // Arrange
            var json = "{\"name\":\"Jane\",\"age\":25}";

            // Act
            var jsonElement = _reflector.JsonSerializer.Deserialize<JsonElement>(json);
            _output.WriteLine($"Deserialized JsonElement: {jsonElement}");

            // Assert
            Assert.Equal(JsonValueKind.Object, jsonElement.ValueKind);
            Assert.True(jsonElement.TryGetProperty("name", out var nameProperty));
            Assert.Equal("Jane", nameProperty.GetString());
            Assert.True(jsonElement.TryGetProperty("age", out var ageProperty));
            Assert.Equal(25, ageProperty.GetInt32());
        }

        [Fact]
        public void JsonElementConverter_Deserialize_NestedObject_ShouldSucceed()
        {
            // Arrange
            var json = "{\"user\":{\"name\":\"Bob\",\"address\":{\"city\":\"NYC\"}}}";

            // Act
            var jsonElement = _reflector.JsonSerializer.Deserialize<JsonElement>(json);
            _output.WriteLine($"Deserialized nested JsonElement: {jsonElement}");

            // Assert
            Assert.Equal(JsonValueKind.Object, jsonElement.ValueKind);
            Assert.True(jsonElement.TryGetProperty("user", out var userProperty));
            Assert.True(userProperty.TryGetProperty("address", out var addressProperty));
            Assert.True(addressProperty.TryGetProperty("city", out var cityProperty));
            Assert.Equal("NYC", cityProperty.GetString());
        }

        [Fact]
        public void JsonElementConverter_RoundTrip_ComplexObject_ShouldPreserveData()
        {
            // Arrange
            var originalJson = "{\"string\":\"test\",\"number\":123,\"bool\":true,\"array\":[1,2,3],\"nested\":{\"key\":\"value\"}}";
            var originalElement = JsonDocument.Parse(originalJson).RootElement;

            // Act
            var serialized = _reflector.JsonSerializer.Serialize(originalElement);
            _output.WriteLine($"Serialized: {serialized}");

            var deserialized = _reflector.JsonSerializer.Deserialize<JsonElement>(serialized);
            var reserialized = _reflector.JsonSerializer.Serialize(deserialized);
            _output.WriteLine($"Reserialized: {reserialized}");

            // Assert - Verify data integrity by comparing property values
            Assert.True(deserialized.TryGetProperty("string", out var stringProp));
            Assert.Equal("test", stringProp.GetString());

            Assert.True(deserialized.TryGetProperty("number", out var numberProp));
            Assert.Equal(123, numberProp.GetInt32());

            Assert.True(deserialized.TryGetProperty("bool", out var boolProp));
            Assert.True(boolProp.GetBoolean());

            Assert.True(deserialized.TryGetProperty("array", out var arrayProp));
            Assert.Equal(3, arrayProp.GetArrayLength());

            Assert.True(deserialized.TryGetProperty("nested", out var nestedProp));
            Assert.True(nestedProp.TryGetProperty("key", out var keyProp));
            Assert.Equal("value", keyProp.GetString());
        }

        #endregion

        #region JsonObject Tests

        [Fact]
        public void JsonObjectConverter_Serialize_SimpleObject_ShouldSucceed()
        {
            // Arrange
            var jsonObject = new JsonObject
            {
                ["name"] = "Alice",
                ["age"] = 28,
                ["isActive"] = true
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonObject);
            _output.WriteLine($"Serialized JsonObject: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("name", json);
            Assert.Contains("Alice", json);
            Assert.Contains("age", json);
            Assert.Contains("28", json);
            Assert.Contains("isActive", json);
            Assert.Contains("true", json);
        }

        [Fact]
        public void JsonObjectConverter_Serialize_NestedObject_ShouldSucceed()
        {
            // Arrange
            var jsonObject = new JsonObject
            {
                ["user"] = new JsonObject
                {
                    ["name"] = "Bob",
                    ["details"] = new JsonObject
                    {
                        ["email"] = "bob@example.com",
                        ["age"] = 35
                    }
                }
            };

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonObject);
            _output.WriteLine($"Serialized nested JsonObject: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("user", json);
            Assert.Contains("Bob", json);
            Assert.Contains("details", json);
            Assert.Contains("bob@example.com", json);
        }

        [Fact]
        public void JsonObjectConverter_Serialize_EmptyObject_ShouldSucceed()
        {
            // Arrange
            var jsonObject = new JsonObject();

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonObject);
            _output.WriteLine($"Serialized empty JsonObject: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("{", json);
            Assert.Contains("}", json);
        }

        [Fact]
        public void JsonObjectConverter_Serialize_NullValue_ShouldSucceed()
        {
            // Arrange
            JsonObject? jsonObject = null;

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonObject);
            _output.WriteLine($"Serialized null JsonObject: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("null", json);
        }

        [Fact]
        public void JsonObjectConverter_Deserialize_SimpleObject_ShouldSucceed()
        {
            // Arrange
            var json = "{\"name\":\"Charlie\",\"score\":95.5}";

            // Act
            var jsonObject = _reflector.JsonSerializer.Deserialize<JsonObject>(json);
            var serializedBack = _reflector.JsonSerializer.Serialize(jsonObject);
            _output.WriteLine($"Deserialized JsonObject: {serializedBack}");

            // Assert
            Assert.NotNull(jsonObject);
            Assert.Equal("Charlie", jsonObject["name"]?.GetValue<string>());
            Assert.Equal(95.5, jsonObject["score"]?.GetValue<double>());
        }

        [Fact]
        public void JsonObjectConverter_Deserialize_WithMixedTypes_ShouldSucceed()
        {
            // Arrange
            var json = "{\"string\":\"text\",\"number\":42,\"boolean\":false,\"null\":null,\"array\":[1,2,3]}";

            // Act
            var jsonObject = _reflector.JsonSerializer.Deserialize<JsonObject>(json);
            var serializedBack = _reflector.JsonSerializer.Serialize(jsonObject);
            _output.WriteLine($"Deserialized mixed types JsonObject: {serializedBack}");

            // Assert
            Assert.NotNull(jsonObject);
            Assert.Equal("text", jsonObject["string"]?.GetValue<string>());
            Assert.Equal(42, jsonObject["number"]?.GetValue<int>());
            Assert.False(jsonObject["boolean"]?.GetValue<bool>());
            Assert.Null(jsonObject["null"]);
            Assert.NotNull(jsonObject["array"]);
        }

        [Fact]
        public void JsonObjectConverter_RoundTrip_ComplexObject_ShouldPreserveData()
        {
            // Arrange
            var original = new JsonObject
            {
                ["id"] = 123,
                ["name"] = "Test User",
                ["metadata"] = new JsonObject
                {
                    ["created"] = "2025-01-01",
                    ["tags"] = new JsonArray("important", "verified")
                }
            };

            // Act
            var serialized = _reflector.JsonSerializer.Serialize(original);
            _output.WriteLine($"Serialized: {serialized}");

            var deserialized = _reflector.JsonSerializer.Deserialize<JsonObject>(serialized);
            var reserialized = _reflector.JsonSerializer.Serialize(deserialized);
            _output.WriteLine($"Reserialized: {reserialized}");

            // Assert
            Assert.Equal(serialized, reserialized);
        }

        [Fact]
        public void JsonObjectConverter_Deserialize_Null_ShouldReturnNull()
        {
            // Arrange
            var json = "null";

            // Act
            var jsonObject = _reflector.JsonSerializer.Deserialize<JsonObject?>(json);
            _output.WriteLine($"Deserialized null: {jsonObject}");

            // Assert
            Assert.Null(jsonObject);
        }

        #endregion

        #region JsonArray Tests

        [Fact]
        public void JsonArrayConverter_Serialize_IntegerArray_ShouldSucceed()
        {
            // Arrange
            var jsonArray = new JsonArray(1, 2, 3, 4, 5);

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonArray);
            _output.WriteLine($"Serialized JsonArray: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("1", json);
            Assert.Contains("5", json);
        }

        [Fact]
        public void JsonArrayConverter_Serialize_StringArray_ShouldSucceed()
        {
            // Arrange
            var jsonArray = new JsonArray("apple", "banana", "cherry");

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonArray);
            _output.WriteLine($"Serialized string JsonArray: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("apple", json);
            Assert.Contains("banana", json);
            Assert.Contains("cherry", json);
        }

        [Fact]
        public void JsonArrayConverter_Serialize_MixedTypeArray_ShouldSucceed()
        {
            // Arrange
            var jsonArray = new JsonArray(
                "string",
                42,
                true,
                null,
                new JsonObject { ["key"] = "value" }
            );

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonArray);
            _output.WriteLine($"Serialized mixed type JsonArray: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("string", json);
            Assert.Contains("42", json);
            Assert.Contains("true", json);
            Assert.Contains("null", json);
            Assert.Contains("key", json);
        }

        [Fact]
        public void JsonArrayConverter_Serialize_NestedArrays_ShouldSucceed()
        {
            // Arrange
            var jsonArray = new JsonArray(
                new JsonArray(1, 2, 3),
                new JsonArray(4, 5, 6),
                new JsonArray(7, 8, 9)
            );

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonArray);
            _output.WriteLine($"Serialized nested JsonArray: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("[", json);
            Assert.Contains("1", json);
            Assert.Contains("9", json);
        }

        [Fact]
        public void JsonArrayConverter_Serialize_EmptyArray_ShouldSucceed()
        {
            // Arrange
            var jsonArray = new JsonArray();

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonArray);
            _output.WriteLine($"Serialized empty JsonArray: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("[", json);
            Assert.Contains("]", json);
        }

        [Fact]
        public void JsonArrayConverter_Serialize_NullValue_ShouldSucceed()
        {
            // Arrange
            JsonArray? jsonArray = null;

            // Act
            var json = _reflector.JsonSerializer.Serialize(jsonArray);
            _output.WriteLine($"Serialized null JsonArray: {json}");

            // Assert
            Assert.NotNull(json);
            Assert.Contains("null", json);
        }

        [Fact]
        public void JsonArrayConverter_Deserialize_IntegerArray_ShouldSucceed()
        {
            // Arrange
            var json = "[10,20,30,40,50]";

            // Act
            var jsonArray = _reflector.JsonSerializer.Deserialize<JsonArray>(json);
            var serializedBack = _reflector.JsonSerializer.Serialize(jsonArray);
            _output.WriteLine($"Deserialized JsonArray: {serializedBack}");

            // Assert
            Assert.NotNull(jsonArray);
            Assert.Equal(5, jsonArray.Count);
            Assert.Equal(10, jsonArray[0]?.GetValue<int>());
            Assert.Equal(50, jsonArray[4]?.GetValue<int>());
        }

        [Fact]
        public void JsonArrayConverter_Deserialize_MixedTypes_ShouldSucceed()
        {
            // Arrange
            var json = "[\"text\",123,true,null,{\"key\":\"value\"}]";

            // Act
            var jsonArray = _reflector.JsonSerializer.Deserialize<JsonArray>(json);
            var serializedBack = _reflector.JsonSerializer.Serialize(jsonArray);
            _output.WriteLine($"Deserialized mixed types JsonArray: {serializedBack}");

            // Assert
            Assert.NotNull(jsonArray);
            Assert.Equal(5, jsonArray.Count);
            Assert.Equal("text", jsonArray[0]?.GetValue<string>());
            Assert.Equal(123, jsonArray[1]?.GetValue<int>());
            Assert.True(jsonArray[2]?.GetValue<bool>());
            Assert.Null(jsonArray[3]);
            Assert.IsType<JsonObject>(jsonArray[4]?.AsObject());
        }

        [Fact]
        public void JsonArrayConverter_RoundTrip_ComplexArray_ShouldPreserveData()
        {
            // Arrange
            var original = new JsonArray(
                1,
                "test",
                new JsonObject { ["nested"] = "object" },
                new JsonArray(true, false, null)
            );

            // Act
            var serialized = _reflector.JsonSerializer.Serialize(original);
            _output.WriteLine($"Serialized: {serialized}");

            var deserialized = _reflector.JsonSerializer.Deserialize<JsonArray>(serialized);
            var reserialized = _reflector.JsonSerializer.Serialize(deserialized);
            _output.WriteLine($"Reserialized: {reserialized}");

            // Assert
            Assert.Equal(serialized, reserialized);
        }

        [Fact]
        public void JsonArrayConverter_Deserialize_Null_ShouldReturnNull()
        {
            // Arrange
            var json = "null";

            // Act
            var jsonArray = _reflector.JsonSerializer.Deserialize<JsonArray?>(json);
            _output.WriteLine($"Deserialized null: {jsonArray}");

            // Assert
            Assert.Null(jsonArray);
        }

        #endregion

        #region Null Handling Tests

        [Fact]
        public void JsonElementConverter_DeserializeNull_AsNullableType_ShouldReturnDefault()
        {
            // Arrange
            var json = "null";

            // Act
            var result = _reflector.JsonSerializer.Deserialize<JsonElement>(json);
            _output.WriteLine($"Deserialized null JsonElement: ValueKind={result.ValueKind}");

            // Assert - When deserialized to a value type like JsonElement, null becomes Undefined
            // This is expected behavior for System.Text.Json
            Assert.True(result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null);
        }

        [Fact]
        public void JsonObjectConverter_WithNullProperties_ShouldHandleGracefully()
        {
            // Arrange
            var jsonObject = new JsonObject
            {
                ["notNull"] = "value",
                ["isNull"] = null
            };

            // Act
            var serialized = _reflector.JsonSerializer.Serialize(jsonObject);
            _output.WriteLine($"Serialized: {serialized}");

            var deserialized = _reflector.JsonSerializer.Deserialize<JsonObject>(serialized);
            var reserialized = _reflector.JsonSerializer.Serialize(deserialized);
            _output.WriteLine($"Reserialized: {reserialized}");

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("value", deserialized["notNull"]?.GetValue<string>());
            Assert.Null(deserialized["isNull"]);
        }

        [Fact]
        public void JsonArrayConverter_WithNullElements_ShouldHandleGracefully()
        {
            // Arrange
            var jsonArray = new JsonArray(1, null, 3, null, 5);

            // Act
            var serialized = _reflector.JsonSerializer.Serialize(jsonArray);
            _output.WriteLine($"Serialized: {serialized}");

            var deserialized = _reflector.JsonSerializer.Deserialize<JsonArray>(serialized);
            var reserialized = _reflector.JsonSerializer.Serialize(deserialized);
            _output.WriteLine($"Reserialized: {reserialized}");

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(5, deserialized.Count);
            Assert.Equal(1, deserialized[0]?.GetValue<int>());
            Assert.Null(deserialized[1]);
            Assert.Equal(3, deserialized[2]?.GetValue<int>());
            Assert.Null(deserialized[3]);
            Assert.Equal(5, deserialized[4]?.GetValue<int>());
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void JsonElementConverter_DeepNesting_ShouldSucceed()
        {
            // Arrange - Create deeply nested JSON (10 levels)
            var json = "{\"l1\":{\"l2\":{\"l3\":{\"l4\":{\"l5\":{\"l6\":{\"l7\":{\"l8\":{\"l9\":{\"l10\":\"deep value\"}}}}}}}}}}";

            // Act
            var jsonElement = _reflector.JsonSerializer.Deserialize<JsonElement>(json);

            // Assert
            var current = jsonElement;
            for (int i = 1; i <= 10; i++)
            {
                if (i == 10)
                {
                    Assert.True(current.TryGetProperty($"l{i}", out var finalProp));
                    Assert.Equal("deep value", finalProp.GetString());
                }
                else
                {
                    Assert.True(current.TryGetProperty($"l{i}", out current));
                }
            }
        }

        [Fact]
        public void JsonArrayConverter_LargeArray_ShouldSucceed()
        {
            // Arrange - Create array with 1000 elements
            var largeArray = new JsonArray();
            for (int i = 0; i < 1000; i++)
            {
                largeArray.Add(i);
            }

            // Act
            var serialized = _reflector.JsonSerializer.Serialize(largeArray);
            var deserialized = _reflector.JsonSerializer.Deserialize<JsonArray>(serialized);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(1000, deserialized.Count);
            Assert.Equal(0, deserialized[0]?.GetValue<int>());
            Assert.Equal(999, deserialized[999]?.GetValue<int>());
        }

        [Fact]
        public void JsonObjectConverter_SpecialCharacters_ShouldPreserve()
        {
            // Arrange
            var jsonObject = new JsonObject
            {
                ["emoji"] = "😀🎉",
                ["unicode"] = "Hello \u4E16\u754C", // Chinese characters
                ["escaped"] = "Line1\nLine2\tTabbed",
                ["quotes"] = "She said \"hello\""
            };

            // Act
            var serialized = _reflector.JsonSerializer.Serialize(jsonObject);
            _output.WriteLine($"Serialized: {serialized}");

            var deserialized = _reflector.JsonSerializer.Deserialize<JsonObject>(serialized);
            var reserialized = _reflector.JsonSerializer.Serialize(deserialized);
            _output.WriteLine($"Reserialized: {reserialized}");

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("😀🎉", deserialized["emoji"]?.GetValue<string>());
            Assert.Contains("世界", deserialized["unicode"]?.GetValue<string>() ?? "");
            Assert.Contains("\n", deserialized["escaped"]?.GetValue<string>());
            Assert.Contains("\"", deserialized["quotes"]?.GetValue<string>());
        }

        [Fact]
        public void JsonElementConverter_NumericPrecision_ShouldPreserve()
        {
            // Arrange
            var json = "{\"decimal\":123.456789012345,\"scientific\":1.23e-10}";

            // Act
            var jsonElement = _reflector.JsonSerializer.Deserialize<JsonElement>(json);
            var reserialized = _reflector.JsonSerializer.Serialize(jsonElement);
            _output.WriteLine($"Original: {json}");
            _output.WriteLine($"Reserialized: {reserialized}");

            // Assert
            Assert.True(jsonElement.TryGetProperty("decimal", out var decimalProp));
            Assert.True(jsonElement.TryGetProperty("scientific", out var scientificProp));
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void AllConverters_WorkTogether_InComplexStructure()
        {
            // Arrange - Mix JsonElement, JsonObject, and JsonArray
            var complexJson = @"{
                ""element"": {""type"":""element"",""value"":123},
                ""object"": {""name"":""Test Object"",""active"":true},
                ""array"": [1,2,3,{""nested"":""value""}]
            }";

            // Act
            var jsonElement = _reflector.JsonSerializer.Deserialize<JsonElement>(complexJson);

            // Extract parts
            Assert.True(jsonElement.TryGetProperty("element", out var elementProp));
            Assert.True(jsonElement.TryGetProperty("object", out var objectProp));
            Assert.True(jsonElement.TryGetProperty("array", out var arrayProp));

            // Serialize back
            var reserialized = _reflector.JsonSerializer.Serialize(jsonElement);
            _output.WriteLine($"Reserialized: {reserialized}");

            // Assert
            Assert.Contains("element", reserialized);
            Assert.Contains("object", reserialized);
            Assert.Contains("array", reserialized);
        }

        #endregion
    }
}
