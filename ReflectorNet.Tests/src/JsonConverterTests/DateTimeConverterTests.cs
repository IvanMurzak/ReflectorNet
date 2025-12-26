using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for date/time JSON converters: DateOnly, TimeOnly (NET6+)
    /// </summary>
    public class DateTimeConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public DateTimeConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

#if NET6_0_OR_GREATER

        #region DateOnlyJsonConverter Tests

        [Fact]
        public void DateOnly_Write_StandardDate()
        {
            var value = new DateOnly(2024, 12, 25);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateOnly standard: {json}");
            Assert.Equal("\"2024-12-25\"", json);
        }

        [Fact]
        public void DateOnly_Write_FirstDayOfYear()
        {
            var value = new DateOnly(2024, 1, 1);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateOnly first day: {json}");
            Assert.Equal("\"2024-01-01\"", json);
        }

        [Fact]
        public void DateOnly_Write_LastDayOfYear()
        {
            var value = new DateOnly(2024, 12, 31);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateOnly last day: {json}");
            Assert.Equal("\"2024-12-31\"", json);
        }

        [Fact]
        public void DateOnly_Write_LeapYearDate()
        {
            var value = new DateOnly(2024, 2, 29);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateOnly leap year: {json}");
            Assert.Equal("\"2024-02-29\"", json);
        }

        [Fact]
        public void DateOnly_Write_MinValue()
        {
            var value = DateOnly.MinValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateOnly min: {json}");
            Assert.Equal("\"0001-01-01\"", json);
        }

        [Fact]
        public void DateOnly_Write_MaxValue()
        {
            var value = DateOnly.MaxValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"DateOnly max: {json}");
            Assert.Equal("\"9999-12-31\"", json);
        }

        [Fact]
        public void DateOnly_Read_StandardDate()
        {
            var json = "\"2024-06-15\"";
            var result = _reflector.JsonSerializer.Deserialize<DateOnly>(json);
            _output.WriteLine($"DateOnly from standard: {result}");
            Assert.Equal(new DateOnly(2024, 6, 15), result);
        }

        [Fact]
        public void DateOnly_Read_SingleDigitMonthDay()
        {
            var json = "\"2024-01-05\"";
            var result = _reflector.JsonSerializer.Deserialize<DateOnly>(json);
            _output.WriteLine($"DateOnly single digit: {result}");
            Assert.Equal(new DateOnly(2024, 1, 5), result);
        }

        [Fact]
        public void DateOnly_Read_InvalidFormat_Throws()
        {
            var json = "\"25-12-2024\""; // wrong format
            Assert.ThrowsAny<Exception>(() => _reflector.JsonSerializer.Deserialize<DateOnly>(json));
        }

        [Fact]
        public void DateOnly_Read_WrongTokenType_Throws()
        {
            var json = "12345";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<DateOnly>(json));
        }

        [Fact]
        public void DateOnly_Read_Null_Throws()
        {
            var json = "null";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<DateOnly>(json));
        }

        [Fact]
        public void DateOnly_RoundTrip()
        {
            var original = new DateOnly(2025, 7, 4);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<DateOnly>(json);
            _output.WriteLine($"DateOnly roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        [Fact]
        public void DateOnly_RoundTrip_Various()
        {
            var dates = new[]
            {
                new DateOnly(2000, 1, 1),
                new DateOnly(2024, 2, 29),
                new DateOnly(1999, 12, 31),
                new DateOnly(2050, 6, 15)
            };

            foreach (var original in dates)
            {
                var json = _reflector.JsonSerializer.Serialize(original);
                var deserialized = _reflector.JsonSerializer.Deserialize<DateOnly>(json);
                _output.WriteLine($"DateOnly roundtrip: {original} -> {json} -> {deserialized}");
                Assert.Equal(original, deserialized);
            }
        }

        #endregion

        #region TimeOnlyJsonConverter Tests

        [Fact]
        public void TimeOnly_Write_Morning()
        {
            var value = new TimeOnly(9, 30, 0);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeOnly morning: {json}");
            Assert.Equal("\"09:30:00.0000000\"", json);
        }

        [Fact]
        public void TimeOnly_Write_Afternoon()
        {
            var value = new TimeOnly(14, 45, 30);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeOnly afternoon: {json}");
            Assert.Equal("\"14:45:30.0000000\"", json);
        }

        [Fact]
        public void TimeOnly_Write_Midnight()
        {
            var value = new TimeOnly(0, 0, 0);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeOnly midnight: {json}");
            Assert.Equal("\"00:00:00.0000000\"", json);
        }

        [Fact]
        public void TimeOnly_Write_LastSecond()
        {
            var value = new TimeOnly(23, 59, 59);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeOnly last second: {json}");
            Assert.Equal("\"23:59:59.0000000\"", json);
        }

        [Fact]
        public void TimeOnly_Write_WithMilliseconds()
        {
            var value = new TimeOnly(12, 30, 45, 123);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeOnly with ms: {json}");
            Assert.Contains("12:30:45", json);
        }

        [Fact]
        public void TimeOnly_Write_WithTicks()
        {
            var value = new TimeOnly(10, 20, 30).Add(TimeSpan.FromTicks(5000));
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeOnly with ticks: {json}");
            Assert.Contains("10:20:30", json);
        }

        [Fact]
        public void TimeOnly_Write_MinValue()
        {
            var value = TimeOnly.MinValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeOnly min: {json}");
            Assert.Equal("\"00:00:00.0000000\"", json);
        }

        [Fact]
        public void TimeOnly_Write_MaxValue()
        {
            var value = TimeOnly.MaxValue;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"TimeOnly max: {json}");
            Assert.Contains("23:59:59.9999999", json);
        }

        [Fact]
        public void TimeOnly_Read_Standard()
        {
            var json = "\"15:30:45.0000000\"";
            var result = _reflector.JsonSerializer.Deserialize<TimeOnly>(json);
            _output.WriteLine($"TimeOnly from standard: {result}");
            Assert.Equal(15, result.Hour);
            Assert.Equal(30, result.Minute);
            Assert.Equal(45, result.Second);
        }

        [Fact]
        public void TimeOnly_Read_Midnight()
        {
            var json = "\"00:00:00.0000000\"";
            var result = _reflector.JsonSerializer.Deserialize<TimeOnly>(json);
            _output.WriteLine($"TimeOnly from midnight: {result}");
            Assert.Equal(TimeOnly.MinValue, result);
        }

        [Fact]
        public void TimeOnly_Read_WrongTokenType_Throws()
        {
            var json = "12345";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<TimeOnly>(json));
        }

        [Fact]
        public void TimeOnly_Read_InvalidFormat_Throws()
        {
            var json = "\"3:30 PM\""; // wrong format
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<TimeOnly>(json));
        }

        [Fact]
        public void TimeOnly_RoundTrip()
        {
            var original = new TimeOnly(16, 45, 30, 250);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<TimeOnly>(json);
            _output.WriteLine($"TimeOnly roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original.Hour, deserialized.Hour);
            Assert.Equal(original.Minute, deserialized.Minute);
            Assert.Equal(original.Second, deserialized.Second);
        }

        [Fact]
        public void TimeOnly_RoundTrip_Various()
        {
            var times = new[]
            {
                new TimeOnly(0, 0, 0),
                new TimeOnly(12, 0, 0),
                new TimeOnly(23, 59, 59),
                new TimeOnly(6, 30, 15)
            };

            foreach (var original in times)
            {
                var json = _reflector.JsonSerializer.Serialize(original);
                var deserialized = _reflector.JsonSerializer.Deserialize<TimeOnly>(json);
                _output.WriteLine($"TimeOnly roundtrip: {original} -> {json} -> {deserialized}");
                Assert.Equal(original.Hour, deserialized.Hour);
                Assert.Equal(original.Minute, deserialized.Minute);
                Assert.Equal(original.Second, deserialized.Second);
            }
        }

        #endregion

#endif
    }
}
