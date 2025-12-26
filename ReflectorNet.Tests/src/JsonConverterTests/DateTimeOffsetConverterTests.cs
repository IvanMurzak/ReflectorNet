using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for DateTimeOffset JSON converter
    /// </summary>
    public class DateTimeOffsetConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public DateTimeOffsetConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region DateTimeOffsetJsonConverter Tests

        [Fact]
        public void DateTimeOffset_Serialize_Value()
        {
            var value = new DateTimeOffset(2024, 12, 25, 10, 30, 45, TimeSpan.FromHours(2));
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateTimeOffset value: {json}");
            Assert.Contains("2024-12-25T10:30:45", json);
            // The + is encoded as \u002B in JSON string
            Assert.Contains("02:00", json);
        }

        [Fact]
        public void DateTimeOffset_Serialize_Utc()
        {
            var value = new DateTimeOffset(2024, 12, 25, 10, 30, 45, TimeSpan.Zero);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateTimeOffset UTC: {json}");
            Assert.Contains("2024-12-25T10:30:45", json);
            // The + is encoded as \u002B in JSON string
            Assert.Contains("00:00", json);
        }

        [Fact]
        public void DateTimeOffset_Serialize_MinValue()
        {
            var value = DateTimeOffset.MinValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateTimeOffset min: {json}");
            Assert.Contains("0001-01-01T00:00:00", json);
            // The + is encoded as \u002B in JSON string
            Assert.Contains("00:00", json);
        }

        [Fact]
        public void DateTimeOffset_Deserialize_Iso8601()
        {
            var json = "\"2024-06-15T14:30:00+03:00\"";
            var result = _reflector.JsonSerializer.Deserialize<DateTimeOffset>(json);
            _output.WriteLine($"DateTimeOffset from ISO: {result}");
            Assert.Equal(2024, result.Year);
            Assert.Equal(6, result.Month);
            Assert.Equal(15, result.Day);
            Assert.Equal(TimeSpan.FromHours(3), result.Offset);
        }

        [Fact]
        public void DateTimeOffset_RoundTrip()
        {
            var original = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5));
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<DateTimeOffset>(json);
            _output.WriteLine($"DateTimeOffset roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion
    }
}
