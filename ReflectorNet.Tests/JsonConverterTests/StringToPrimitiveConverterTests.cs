#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    public class StringToPrimitiveConverterTests : BaseTest
    {
        private readonly Reflector _reflector;
        private readonly JsonSerializerOptions _options;

        public StringToPrimitiveConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
            _options = _reflector.JsonSerializerOptions;
        }

        #region Boolean Tests

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("TRUE", true)]
        [InlineData("false", false)]
        [InlineData("False", false)]
        [InlineData("FALSE", false)]
        public void StringToBool_ValidValues_ShouldConvert(string stringValue, bool expected)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<bool>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("null", null)]
        public void StringToBoolNullable_ValidValues_ShouldConvert(string stringValue, bool? expected)
        {
            // Arrange
            var json = stringValue == "null" ? "null" : $"\"{stringValue}\"";
            _output.WriteLine($"Testing nullable: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<bool?>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("maybe")]
        [InlineData("1")]
        [InlineData("0")]
        [InlineData("")]
        public void StringToBool_InvalidValues_ShouldThrow(string stringValue)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing invalid: {json}");

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<bool>(json, _options));
        }

        #endregion

        #region Integer Tests

        [Theory]
        [InlineData("42", 42)]
        [InlineData("-42", -42)]
        [InlineData("0", 0)]
        [InlineData("2147483647", int.MaxValue)]
        [InlineData("-2147483648", int.MinValue)]
        public void StringToInt_ValidValues_ShouldConvert(string stringValue, int expected)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<int>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("42", 42)]
        [InlineData("null", null)]
        public void StringToIntNullable_ValidValues_ShouldConvert(string stringValue, int? expected)
        {
            // Arrange
            var json = stringValue == "null" ? "null" : $"\"{stringValue}\"";
            _output.WriteLine($"Testing nullable: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<int?>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("2147483648")] // int.MaxValue + 1
        [InlineData("-2147483649")] // int.MinValue - 1
        [InlineData("abc")]
        [InlineData("42.5")]
        [InlineData("")]
        public void StringToInt_InvalidValues_ShouldThrow(string stringValue)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing invalid: {json}");

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(json, _options));
        }

        #endregion

        #region Long Tests

        [Theory]
        [InlineData("9223372036854775807", long.MaxValue)]
        [InlineData("-9223372036854775808", long.MinValue)]
        [InlineData("0", 0L)]
        public void StringToLong_ValidValues_ShouldConvert(string stringValue, long expected)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<long>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Floating Point Tests

        [Theory]
        [InlineData("3.14159", 3.14159)]
        [InlineData("-3.14159", -3.14159)]
        [InlineData("0.0", 0.0)]
        [InlineData("1.23456789", 1.23456789)]
        public void StringToDouble_ValidValues_ShouldConvert(string stringValue, double expected)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<double>(json, _options);

            // Assert
            Assert.Equal(expected, result, precision: 8);
        }

        [Theory]
        [InlineData("3.14", 3.14f)]
        [InlineData("-3.14", -3.14f)]
        [InlineData("0.0", 0.0f)]
        public void StringToFloat_ValidValues_ShouldConvert(string stringValue, float expected)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<float>(json, _options);

            // Assert
            Assert.Equal(expected, result, precision: 5);
        }

        [Theory]
        [InlineData("123.456789", 123.456789)]
        [InlineData("-123.456789", -123.456789)]
        [InlineData("0", 0)]
        public void StringToDecimal_ValidValues_ShouldConvert(string stringValue, double expectedDouble)
        {
            // Arrange
            var expected = (decimal)expectedDouble;
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<decimal>(json, _options);

            // Assert
            Assert.Equal(expected, result, precision: 6);
        }

        #endregion

        #region DateTime Tests

        [Theory]
        [InlineData("2023-12-25", "2023-12-25")]
        [InlineData("2023-12-25T10:30:00", "2023-12-25T10:30:00")]
        [InlineData("2023-12-25T10:30:00.123", "2023-12-25T10:30:00.123")]
        public void StringToDateTime_ValidValues_ShouldConvert(string stringValue, string expectedString)
        {
            // Arrange
            var expected = DateTime.Parse(expectedString, CultureInfo.InvariantCulture);
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<DateTime>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("2023-12-25T10:30:00+05:00")]
        [InlineData("2023-12-25T10:30:00-08:00")]
        [InlineData("2023-12-25T10:30:00Z")]
        public void StringToDateTimeOffset_ValidValues_ShouldConvert(string stringValue)
        {
            // Arrange
            var expected = DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture);
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<DateTimeOffset>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region TimeSpan Tests

        [Theory]
        [InlineData("10:30:00", "10:30:00")]
        [InlineData("1.10:30:00", "1.10:30:00")]
        [InlineData("00:00:00", "00:00:00")]
        public void StringToTimeSpan_ValidValues_ShouldConvert(string stringValue, string expectedString)
        {
            // Arrange
            var expected = TimeSpan.Parse(expectedString, CultureInfo.InvariantCulture);
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<TimeSpan>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Guid Tests

        [Theory]
        [InlineData("550e8400-e29b-41d4-a716-446655440000")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        public void StringToGuid_ValidValues_ShouldConvert(string stringValue)
        {
            // Arrange
            var expected = Guid.Parse(stringValue);
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<Guid>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("invalid-guid")]
        [InlineData("")]
        [InlineData("123")]
        public void StringToGuid_InvalidValues_ShouldThrow(string stringValue)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing invalid: {json}");

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Guid>(json, _options));
        }

        #endregion

        #region Byte Tests

        [Theory]
        [InlineData("255", byte.MaxValue)]
        [InlineData("0", byte.MinValue)]
        [InlineData("128", (byte)128)]
        public void StringToByte_ValidValues_ShouldConvert(string stringValue, byte expected)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing: {json} -> {expected}");

            // Act
            var result = JsonSerializer.Deserialize<byte>(json, _options);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("256")] // byte.MaxValue + 1
        [InlineData("-1")] // byte.MinValue - 1
        [InlineData("abc")]
        public void StringToByte_InvalidValues_ShouldThrow(string stringValue)
        {
            // Arrange
            var json = $"\"{stringValue}\"";
            _output.WriteLine($"Testing invalid: {json}");

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte>(json, _options));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void EmptyString_ShouldThrow()
        {
            // Arrange
            var json = "\"\"";
            _output.WriteLine($"Testing empty string: {json}");

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(json, _options));
        }

        [Fact]
        public void WhitespaceString_ShouldThrow()
        {
            // Arrange
            var json = "\"   \"";
            _output.WriteLine($"Testing whitespace string: {json}");

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(json, _options));
        }

        [Fact]
        public void NullString_ToNullableType_ShouldReturnNull()
        {
            // Arrange
            var json = "null";
            _output.WriteLine($"Testing null to nullable: {json}");

            // Act
            var result = JsonSerializer.Deserialize<int?>(json, _options);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void NullString_ToNonNullableType_ShouldThrow()
        {
            // Arrange
            var json = "null";
            _output.WriteLine($"Testing null to non-nullable: {json}");

            // Act & Assert
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(json, _options));
        }

        #endregion

        #region Serialization Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BoolSerialization_ShouldProduceCorrectJson(bool value)
        {
            // Act
            var json = JsonSerializer.Serialize(value, _options);
            _output.WriteLine($"Serialized {value} -> {json}");

            // Assert
            Assert.Equal(value.ToString().ToLowerInvariant(), json);
        }

        [Theory]
        [InlineData(42)]
        [InlineData(-42)]
        [InlineData(0)]
        public void IntSerialization_ShouldProduceCorrectJson(int value)
        {
            // Act
            var json = JsonSerializer.Serialize(value, _options);
            _output.WriteLine($"Serialized {value} -> {json}");

            // Assert
            Assert.Equal(value.ToString(), json);
        }

        [Fact]
        public void GuidSerialization_ShouldProduceQuotedString()
        {
            // Arrange
            var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

            // Act
            var json = JsonSerializer.Serialize(guid, _options);
            _output.WriteLine($"Serialized {guid} -> {json}");

            // Assert
            Assert.Equal($"\"{guid}\"", json);
        }

        #endregion

        #region Round-trip Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(42)]
        [InlineData(-42)]
        [InlineData(3.14159)]
        [InlineData(-3.14159)]
        public void RoundTrip_PrimitiveValues_ShouldMaintainValue<T>(T value)
        {
            // Act
            var json = JsonSerializer.Serialize(value, _options);
            var deserialized = JsonSerializer.Deserialize<T>(json, _options);
            _output.WriteLine($"Round-trip: {value} -> {json} -> {deserialized}");

            // Assert
            Assert.Equal<T>(
                expected: value,
                actual: deserialized,
                comparer: EqualityComparer<T>.Default);
        }

        [Fact]
        public void RoundTrip_NullableTypes_ShouldMaintainValue()
        {
            // Arrange
            int? nullableInt = 42;
            bool? nullableBool = true;

            // Act & Assert
            var intJson = JsonSerializer.Serialize(nullableInt, _options);
            var deserializedInt = JsonSerializer.Deserialize<int?>(intJson, _options);
            Assert.Equal(nullableInt, deserializedInt);

            var boolJson = JsonSerializer.Serialize(nullableBool, _options);
            var deserializedBool = JsonSerializer.Deserialize<bool?>(boolJson, _options);
            Assert.Equal(nullableBool, deserializedBool);

            _output.WriteLine($"Nullable round-trip successful");
        }

        #endregion

        #region Performance and Stress Tests

        [Fact]
        public void LargeNumberString_ShouldConvert()
        {
            // Arrange
            var largeNumber = "999999999999999999";
            var json = $"\"{largeNumber}\"";
            _output.WriteLine($"Testing large number: {json}");

            // Act
            var result = JsonSerializer.Deserialize<long>(json, _options);

            // Assert
            Assert.Equal(999999999999999999L, result);
        }

        [Fact]
        public void VeryLongDecimalString_ShouldConvert()
        {
            // Arrange
            var longDecimal = "123.456789123456789123456789";
            var json = $"\"{longDecimal}\"";
            _output.WriteLine($"Testing long decimal: {json}");

            // Act
            var result = JsonSerializer.Deserialize<decimal>(json, _options);

            // Assert
            Assert.True(result > 123m && result < 124m);
        }

        #endregion

        #region Complex Scenarios

        public class TestClass
        {
            public bool BoolValue { get; set; }
            public int IntValue { get; set; }
            public double DoubleValue { get; set; }
            public DateTime DateValue { get; set; }
            public Guid GuidValue { get; set; }
            public bool? NullableBoolValue { get; set; }
            public int? NullableIntValue { get; set; }
        }

        [Fact]
        public void ComplexObject_WithStringifiedPrimitives_ShouldDeserialize()
        {
            // Arrange
            var json = @"{
                ""boolValue"": ""true"",
                ""intValue"": ""42"",
                ""doubleValue"": ""3.14159"",
                ""dateValue"": ""2023-12-25T10:30:00"",
                ""guidValue"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""nullableBoolValue"": ""false"",
                ""nullableIntValue"": ""123""
            }";
            _output.WriteLine($"Testing complex object: {json}");

            // Act
            var result = JsonSerializer.Deserialize<TestClass>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.BoolValue);
            Assert.Equal(42, result.IntValue);
            Assert.Equal(3.14159, result.DoubleValue, precision: 5);
            Assert.Equal(new DateTime(2023, 12, 25, 10, 30, 0), result.DateValue);
            Assert.Equal(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), result.GuidValue);
            Assert.False(result.NullableBoolValue);
            Assert.Equal(123, result.NullableIntValue);
        }

        [Fact]
        public void ComplexObject_WithNullValues_ShouldDeserialize()
        {
            // Arrange
            var json = @"{
                ""boolValue"": ""false"",
                ""intValue"": ""0"",
                ""doubleValue"": ""0.0"",
                ""dateValue"": ""2023-01-01T00:00:00"",
                ""guidValue"": ""00000000-0000-0000-0000-000000000000"",
                ""nullableBoolValue"": null,
                ""nullableIntValue"": null
            }";
            _output.WriteLine($"Testing complex object with nulls: {json}");

            // Act
            var result = JsonSerializer.Deserialize<TestClass>(json, _options);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.BoolValue);
            Assert.Equal(0, result.IntValue);
            Assert.Equal(0.0, result.DoubleValue);
            Assert.Equal(new DateTime(2023, 1, 1), result.DateValue);
            Assert.Equal(Guid.Empty, result.GuidValue);
            Assert.Null(result.NullableBoolValue);
            Assert.Null(result.NullableIntValue);
        }

        #endregion
    }
}