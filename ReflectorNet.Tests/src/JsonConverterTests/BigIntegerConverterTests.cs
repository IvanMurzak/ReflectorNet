using System;
using System.Numerics;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for BigInteger JSON converter
    /// </summary>
    public class BigIntegerConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public BigIntegerConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region BigIntegerJsonConverter Tests

        [Fact]
        public void BigInteger_Serialize_Value()
        {
            var value = BigInteger.Parse("123456789012345678901234567890");
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"BigInteger value: {json}");
            Assert.Contains("123456789012345678901234567890", json);
        }

        [Fact]
        public void BigInteger_Serialize_Zero()
        {
            var value = BigInteger.Zero;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"BigInteger zero: {json}");
            Assert.Equal("\"0\"", json);
        }

        [Fact]
        public void BigInteger_Serialize_Negative()
        {
            var value = BigInteger.Parse("-98765432109876543210");
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"BigInteger negative: {json}");
            Assert.Contains("-98765432109876543210", json);
        }

        [Fact]
        public void BigInteger_Deserialize_FromNumber()
        {
            var json = "12345678901234567890";
            var result = _reflector.JsonSerializer.Deserialize<BigInteger>(json);
            _output.WriteLine($"BigInteger from number: {result}");
            Assert.Equal(BigInteger.Parse("12345678901234567890"), result);
        }

        [Fact]
        public void BigInteger_Deserialize_FromString()
        {
            var json = "\"999999999999999999999999999999\"";
            var result = _reflector.JsonSerializer.Deserialize<BigInteger>(json);
            _output.WriteLine($"BigInteger from string: {result}");
            Assert.Equal(BigInteger.Parse("999999999999999999999999999999"), result);
        }

        [Fact]
        public void BigInteger_RoundTrip()
        {
            var original = BigInteger.Pow(2, 100);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<BigInteger>(json);
            _output.WriteLine($"BigInteger roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion
    }
}
