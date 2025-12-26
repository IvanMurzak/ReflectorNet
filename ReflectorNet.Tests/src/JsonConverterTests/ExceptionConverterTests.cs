using System;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for Exception JSON converter
    /// </summary>
    public class ExceptionConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public ExceptionConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region ExceptionJsonConverter Tests

        [Fact]
        public void Exception_Serialize_Simple()
        {
            var value = new Exception("Test error message");
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Exception));
            _output.WriteLine($"Exception simple: {json}");
            Assert.Contains("Test error message", json);
            Assert.Contains("type", json);
        }

        [Fact]
        public void Exception_Serialize_WithInnerException()
        {
            var inner = new InvalidOperationException("Inner error");
            var value = new Exception("Outer error", inner);
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Exception));
            _output.WriteLine($"Exception with inner: {json}");
            Assert.Contains("Outer error", json);
            Assert.Contains("innerException", json);
        }

        [Fact]
        public void Exception_Serialize_Null()
        {
            Exception? value = null;
            var json = _reflector.JsonSerializer.Serialize(value, typeof(Exception));
            _output.WriteLine($"Exception null: {json}");
            Assert.Equal("null", json);
        }

        [Fact]
        public void Exception_Deserialize_Simple()
        {
            var json = "{\"type\":\"System.Exception\",\"message\":\"Test message\"}";
            var result = _reflector.JsonSerializer.Deserialize<Exception>(json);
            _output.WriteLine($"Exception from json: {result?.Message}");
            Assert.NotNull(result);
            Assert.Contains("Test message", result.Message);
        }

        [Fact]
        public void Exception_Deserialize_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<Exception>(json);
            _output.WriteLine($"Exception from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Exception_RoundTrip()
        {
            var original = new ArgumentException("Invalid argument");
            var json = _reflector.JsonSerializer.Serialize(original, typeof(Exception));
            var deserialized = _reflector.JsonSerializer.Deserialize<Exception>(json);
            _output.WriteLine($"Exception roundtrip: {original.Message} -> {json}");
            Assert.NotNull(deserialized);
            Assert.Contains("Invalid argument", deserialized.Message);
        }

        #endregion
    }
}
