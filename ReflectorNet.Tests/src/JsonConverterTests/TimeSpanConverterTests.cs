using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for TimeSpan JSON converter
    /// </summary>
    public class TimeSpanConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public TimeSpanConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region TimeSpanJsonConverter Tests

        [Fact]
        public void TimeSpan_Serialize_Value()
        {
            var value = new TimeSpan(1, 2, 3, 4, 5);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeSpan value: {json}");
            Assert.Contains("1.02:03:04.0050000", json);
        }

        [Fact]
        public void TimeSpan_Serialize_Zero()
        {
            var value = TimeSpan.Zero;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeSpan zero: {json}");
            Assert.Contains("00:00:00", json);
        }

        [Fact]
        public void TimeSpan_Serialize_MinValue()
        {
            var value = TimeSpan.MinValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeSpan min: {json}");
            Assert.Contains("-10675199.02:48:05.4775808", json);
        }

        [Fact]
        public void TimeSpan_Serialize_MaxValue()
        {
            var value = TimeSpan.MaxValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeSpan max: {json}");
            Assert.Contains("10675199.02:48:05.4775807", json);
        }

        [Fact]
        public void TimeSpan_Deserialize_FromString()
        {
            var json = "\"1.02:03:04.0050000\"";
            var result = _reflector.JsonSerializer.Deserialize<TimeSpan>(json);
            _output.WriteLine($"TimeSpan from string: {result}");
            Assert.Equal(new TimeSpan(1, 2, 3, 4, 5), result);
        }

        [Fact]
        public void TimeSpan_Deserialize_Null_ToNullable()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<TimeSpan?>(json);
            _output.WriteLine($"TimeSpan null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void TimeSpan_RoundTrip()
        {
            var original = new TimeSpan(1234567890);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<TimeSpan>(json);
            _output.WriteLine($"TimeSpan roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion
    }
}
