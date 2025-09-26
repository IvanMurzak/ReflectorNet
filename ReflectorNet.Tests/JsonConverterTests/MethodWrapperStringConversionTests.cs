using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    public class MethodWrapperStringConversionTests : BaseTest
    {
        private readonly Reflector _reflector;

        public MethodWrapperStringConversionTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        // Test methods for MethodWrapper to call
        public static bool TestBoolMethod(bool value) => value;
        public static bool? TestNullableBoolMethod(bool? value) => value;
        public static int TestIntMethod(int value) => value;
        public static int? TestNullableIntMethod(int? value) => value;
        public static double TestDoubleMethod(double value) => value;
        public static float TestFloatMethod(float value) => value;
        public static decimal TestDecimalMethod(decimal value) => value;
        public static DateTime TestDateTimeMethod(DateTime value) => value;
        public static DateTimeOffset TestDateTimeOffsetMethod(DateTimeOffset value) => value;
        public static TimeSpan TestTimeSpanMethod(TimeSpan value) => value;
        public static Guid TestGuidMethod(Guid value) => value;
        public static byte TestByteMethod(byte value) => value;
        public static sbyte TestSByteMethod(sbyte value) => value;
        public static short TestShortMethod(short value) => value;
        public static ushort TestUShortMethod(ushort value) => value;
        public static uint TestUIntMethod(uint value) => value;
        public static ulong TestULongMethod(ulong value) => value;
        public static long TestLongMethod(long value) => value;

        public enum TestEnum { Value1, Value2, Value3 }
        public static TestEnum TestEnumMethod(TestEnum value) => value;

        #region Boolean Parameter Tests

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("TRUE", true)]
        [InlineData("false", false)]
        [InlineData("False", false)]
        [InlineData("FALSE", false)]
        public async Task MethodWrapper_BoolParameter_StringValue_ShouldConvert(string stringValue, bool expected)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestBoolMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            // Create JsonElement with string value
            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing bool method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public async Task MethodWrapper_NullableBoolParameter_StringValue_ShouldConvert(string stringValue, bool expected)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestNullableBoolMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing nullable bool method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task MethodWrapper_NullableBoolParameter_NullValue_ShouldConvert()
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestNullableBoolMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("null").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine("Testing nullable bool method with null parameter");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("maybe")]
        [InlineData("1")]
        [InlineData("0")]
        [InlineData("")]
        public async Task MethodWrapper_BoolParameter_InvalidStringValue_ShouldThrow(string stringValue)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestBoolMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing bool method with invalid string parameter: \"{stringValue}\"");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => wrapper.InvokeDict(parameters));
        }

        #endregion

        #region Integer Parameter Tests

        [Theory]
        [InlineData("42", 42)]
        [InlineData("-42", -42)]
        [InlineData("0", 0)]
        [InlineData("2147483647", int.MaxValue)]
        [InlineData("-2147483648", int.MinValue)]
        public async Task MethodWrapper_IntParameter_StringValue_ShouldConvert(string stringValue, int expected)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestIntMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing int method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("2147483648")] // int.MaxValue + 1
        [InlineData("-2147483649")] // int.MinValue - 1
        [InlineData("abc")]
        [InlineData("42.5")]
        [InlineData("")]
        public async Task MethodWrapper_IntParameter_InvalidStringValue_ShouldThrow(string stringValue)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestIntMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing int method with invalid string parameter: \"{stringValue}\"");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => wrapper.InvokeDict(parameters));
        }

        #endregion

        #region Floating Point Parameter Tests

        [Theory]
        [InlineData("3.14159", 3.14159)]
        [InlineData("-3.14159", -3.14159)]
        [InlineData("0.0", 0.0)]
        [InlineData("123.456", 123.456)]
        public async Task MethodWrapper_DoubleParameter_StringValue_ShouldConvert(string stringValue, double expected)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestDoubleMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing double method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, (double)result!, precision: 8);
        }

        [Theory]
        [InlineData("3.14", 3.14f)]
        [InlineData("-3.14", -3.14f)]
        [InlineData("0.0", 0.0f)]
        public async Task MethodWrapper_FloatParameter_StringValue_ShouldConvert(string stringValue, float expected)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestFloatMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing float method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, (float)result!, precision: 6);
        }

        #endregion

        #region DateTime Parameter Tests

        [Theory]
        [InlineData("2023-12-25", "2023-12-25")]
        [InlineData("2023-12-25T10:30:00", "2023-12-25T10:30:00")]
        [InlineData("2023-12-25T10:30:00.123", "2023-12-25T10:30:00.123")]
        public async Task MethodWrapper_DateTimeParameter_StringValue_ShouldConvert(string stringValue, string expectedString)
        {
            // Arrange
            var expected = DateTime.Parse(expectedString, System.Globalization.CultureInfo.InvariantCulture);
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestDateTimeMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing DateTime method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Guid Parameter Tests

        [Theory]
        [InlineData("550e8400-e29b-41d4-a716-446655440000")]
        [InlineData("00000000-0000-0000-0000-000000000000")]
        public async Task MethodWrapper_GuidParameter_StringValue_ShouldConvert(string stringValue)
        {
            // Arrange
            var expected = Guid.Parse(stringValue);
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestGuidMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing Guid method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("invalid-guid")]
        [InlineData("")]
        [InlineData("123")]
        public async Task MethodWrapper_GuidParameter_InvalidStringValue_ShouldThrow(string stringValue)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestGuidMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing Guid method with invalid string parameter: \"{stringValue}\"");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => wrapper.InvokeDict(parameters));
        }

        #endregion

        #region Byte and Small Integer Parameter Tests

        [Theory]
        [InlineData("255", byte.MaxValue)]
        [InlineData("0", byte.MinValue)]
        [InlineData("128", (byte)128)]
        public async Task MethodWrapper_ByteParameter_StringValue_ShouldConvert(string stringValue, byte expected)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestByteMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing byte method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("32767", short.MaxValue)]
        [InlineData("-32768", short.MinValue)]
        [InlineData("0", (short)0)]
        public async Task MethodWrapper_ShortParameter_StringValue_ShouldConvert(string stringValue, short expected)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestShortMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing short method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Large Integer Parameter Tests

        [Theory]
        [InlineData("9223372036854775807", long.MaxValue)]
        [InlineData("-9223372036854775808", long.MinValue)]
        [InlineData("0", 0L)]
        public async Task MethodWrapper_LongParameter_StringValue_ShouldConvert(string stringValue, long expected)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestLongMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing long method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, result);
        }

        #endregion

        #region Enum Parameter Tests

        [Theory]
        [InlineData("Value1", TestEnum.Value1)]
        [InlineData("Value2", TestEnum.Value2)]
        [InlineData("Value3", TestEnum.Value3)]
        [InlineData("value1", TestEnum.Value1)] // Case insensitive
        [InlineData("VALUE2", TestEnum.Value2)] // Case insensitive
        public async Task MethodWrapper_EnumParameter_StringValue_ShouldConvert(string stringValue, TestEnum expected)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestEnumMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing enum method with string parameter: \"{stringValue}\" -> {expected}");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("InvalidValue")]
        [InlineData("")]
        [InlineData("999")]
        public async Task MethodWrapper_EnumParameter_InvalidStringValue_ShouldThrow(string stringValue)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestEnumMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{stringValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing enum method with invalid string parameter: \"{stringValue}\"");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => wrapper.InvokeDict(parameters));
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task MethodWrapper_EmptyStringParameter_ShouldThrow()
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestIntMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("\"\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine("Testing int method with empty string parameter");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => wrapper.InvokeDict(parameters));
        }

        [Fact]
        public async Task MethodWrapper_WhitespaceStringParameter_ShouldThrow()
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestIntMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("\"   \"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine("Testing int method with whitespace string parameter");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => wrapper.InvokeDict(parameters));
        }

        #endregion

        #region Multiple Parameter Tests

        public static string TestMultipleParametersMethod(bool boolParam, int intParam, string stringParam)
        {
            return $"Bool: {boolParam}, Int: {intParam}, String: {stringParam}";
        }

        [Fact]
        public async Task MethodWrapper_MultipleStringifiedParameters_ShouldConvertAll()
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestMultipleParametersMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var boolElement = JsonDocument.Parse("\"true\"").RootElement;
            var intElement = JsonDocument.Parse("\"42\"").RootElement;
            var stringElement = JsonDocument.Parse("\"hello\"").RootElement;

            var parameters = new Dictionary<string, object?>
            {
                { "boolParam", boolElement },
                { "intParam", intElement },
                { "stringParam", stringElement }
            };

            _output.WriteLine("Testing method with multiple stringified parameters");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal("Bool: True, Int: 42, String: hello", result);
        }

        #endregion

        #region Original Bug Reproduction Tests

        [Fact]
        public async Task MethodWrapper_BoolParameterFromJsonString_OriginalBugCase_ShouldWork()
        {
            // This test reproduces the exact scenario from the original bug report:
            // JsonElement with ValueKind.String and value "True" for a bool parameter

            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestBoolMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            // Create JsonElement exactly as described in the bug report
            var jsonElement = JsonDocument.Parse("\"True\"").RootElement;
            Assert.Equal(JsonValueKind.String, jsonElement.ValueKind);
            Assert.Equal("True", jsonElement.GetString());

            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine("Testing original bug case: JsonElement with ValueKind.String and value 'True' for bool parameter");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.True((bool)result!);
        }

        [Fact]
        public async Task MethodWrapper_BoolParameterFromJsonString_FalseCase_ShouldWork()
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestBoolMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("\"False\"").RootElement;
            Assert.Equal(JsonValueKind.String, jsonElement.ValueKind);
            Assert.Equal("False", jsonElement.GetString());

            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine("Testing JsonElement with ValueKind.String and value 'False' for bool parameter");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.False((bool)result!);
        }

        #endregion

        #region Edge Case Tests - Malformed JSON and Large Numbers

        [Fact]
        public void MethodWrapper_MalformedJsonElement_ShouldThrow()
        {
            // Create a malformed JSON element by manipulating raw JSON
            var malformedJson = "{ \"incomplete\": ";

            _output.WriteLine("Testing malformed JSON handling");

            // Act & Assert - Any JSON exception is acceptable (JsonException or its derived types)
            var exception = Assert.ThrowsAny<JsonException>(() =>
            {
                var jsonElement = JsonDocument.Parse(malformedJson).RootElement;
            });

            _output.WriteLine($"Exception thrown: {exception.GetType().Name}: {exception.Message}");
        }

        [Theory]
        [InlineData("92233720368547758080")] // Much larger than long.MaxValue
        [InlineData("-92233720368547758090")] // Much smaller than long.MinValue
        [InlineData("abc123")] // Non-numeric string for long conversion
        [InlineData("123.456")] // Decimal string for long conversion
        public async Task MethodWrapper_VeryLargeNumbers_ShouldThrow(string largeNumber)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestLongMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{largeNumber}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing invalid long number: \"{largeNumber}\"");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => wrapper.InvokeDict(parameters));
        }

        [Theory]
        [InlineData("3.4028236E+39")] // Greater than float.MaxValue
        [InlineData("-3.4028236E+39")] // Less than float.MinValue
        public async Task MethodWrapper_VeryLargeFloats_ShouldConvertToInfinity(string largeFloat)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestFloatMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{largeFloat}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing very large float: \"{largeFloat}\"");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert - very large floats should convert to infinity
            var floatResult = (float)result!;
            if (largeFloat.StartsWith("-"))
            {
                Assert.True(float.IsNegativeInfinity(floatResult));
            }
            else
            {
                Assert.True(float.IsPositiveInfinity(floatResult));
            }
        }

        [Theory]
        [InlineData("79228162514264337593543950336")] // Greater than decimal.MaxValue
        [InlineData("-79228162514264337593543950336")] // Less than decimal.MinValue
        public async Task MethodWrapper_VeryLargeDecimals_ShouldThrow(string largeDecimal)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestDecimalMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{largeDecimal}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing very large decimal: \"{largeDecimal}\"");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => wrapper.InvokeDict(parameters));
        }

        [Fact]
        public async Task MethodWrapper_NullJsonElement_ShouldHandleGracefully()
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestNullableIntMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse("null").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine("Testing null JSON element handling");

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("{\"malformed\": json}")]
        [InlineData("[incomplete array")]
        [InlineData("\"unterminated string")]
        public void MethodWrapper_InvalidJsonStrings_ShouldThrow(string invalidJson)
        {
            _output.WriteLine($"Testing invalid JSON string: {invalidJson}");

            // Act & Assert - Any JSON exception is acceptable (JsonException or its derived types)
            Assert.ThrowsAny<JsonException>(() =>
            {
                JsonDocument.Parse(invalidJson);
            });
        }

        [Theory]
        [InlineData("NaN")]
        [InlineData("Infinity")]
        [InlineData("-Infinity")]
        public async Task MethodWrapper_SpecialFloatValues_ShouldConvertOrThrow(string specialValue)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestDoubleMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{specialValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing special float value: \"{specialValue}\"");

            try
            {
                // Act
                var result = await wrapper.InvokeDict(parameters);

                // Assert - if no exception, verify the result is the expected special value
                var doubleResult = (double)result!;
                switch (specialValue)
                {
                    case "NaN":
                        Assert.True(double.IsNaN(doubleResult));
                        break;
                    case "Infinity":
                        Assert.True(double.IsPositiveInfinity(doubleResult));
                        break;
                    case "-Infinity":
                        Assert.True(double.IsNegativeInfinity(doubleResult));
                        break;
                }
            }
            catch (ArgumentException)
            {
                // Also acceptable if the conversion throws an ArgumentException
                _output.WriteLine($"Special value {specialValue} threw ArgumentException as expected");
            }
        }

        [Theory]
        [InlineData("4294967296")] // uint.MaxValue + 1
        [InlineData("-1")] // Less than uint.MinValue
        public async Task MethodWrapper_UnsignedIntOverflow_ShouldThrow(string overflowValue)
        {
            // Arrange
            var methodInfo = typeof(MethodWrapperStringConversionTests).GetMethod(nameof(TestUIntMethod));
            var wrapper = MethodWrapper.Create(_reflector, null, methodInfo!);

            var jsonElement = JsonDocument.Parse($"\"{overflowValue}\"").RootElement;
            var parameters = new Dictionary<string, object?> { { "value", jsonElement } };

            _output.WriteLine($"Testing uint overflow: \"{overflowValue}\"");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => wrapper.InvokeDict(parameters));
        }

        #endregion
    }
}