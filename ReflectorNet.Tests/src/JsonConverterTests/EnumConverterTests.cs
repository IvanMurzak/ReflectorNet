using System;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for Enum JSON converter
    /// </summary>
    public class EnumConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public EnumConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        // Sample enum for testing
        public enum TestStatus
        {
            Unknown = 0,
            Active = 1,
            Inactive = 2,
            Pending = 3
        }

        #region EnumJsonConverter Tests

        [Fact]
        public void Enum_Serialize_Value()
        {
            var value = TestStatus.Active;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Enum value: {json}");
            Assert.Contains("Active", json);
        }

        [Fact]
        public void Enum_Serialize_Null()
        {
            TestStatus? value = null;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Enum null: {json}");
            Assert.Equal("null", json);
        }

        [Fact]
        public void Enum_Deserialize_FromString()
        {
            var json = "\"Active\"";
            var result = _reflector.JsonSerializer.Deserialize<TestStatus>(json);
            _output.WriteLine($"Enum from string: {result}");
            Assert.Equal(TestStatus.Active, result);
        }

        [Fact]
        public void Enum_Deserialize_FromString_CaseInsensitive()
        {
            var json = "\"active\"";
            var result = _reflector.JsonSerializer.Deserialize<TestStatus>(json);
            _output.WriteLine($"Enum case insensitive: {result}");
            Assert.Equal(TestStatus.Active, result);
        }

        [Fact]
        public void Enum_Deserialize_FromNumber()
        {
            var json = "2";
            var result = _reflector.JsonSerializer.Deserialize<TestStatus>(json);
            _output.WriteLine($"Enum from number: {result}");
            Assert.Equal(TestStatus.Inactive, result);
        }

        [Fact]
        public void Enum_Deserialize_Null_ToNullable()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<TestStatus?>(json);
            _output.WriteLine($"Enum null to nullable: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Enum_Deserialize_InvalidString_Throws()
        {
            var json = "\"NotAValidValue\"";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<TestStatus>(json));
        }

        [Fact]
        public void Enum_RoundTrip()
        {
            var original = TestStatus.Pending;
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<TestStatus>(json);
            _output.WriteLine($"Enum roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion
    }
}
