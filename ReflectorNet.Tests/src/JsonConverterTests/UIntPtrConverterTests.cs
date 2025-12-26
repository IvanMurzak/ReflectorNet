using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for UIntPtr JSON converter
    /// </summary>
    public class UIntPtrConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public UIntPtrConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region UIntPtrJsonConverter Tests

        [Fact]
        public void UIntPtr_Serialize_Value()
        {
            var value = new UIntPtr(12345);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UIntPtr value: {json}");
            Assert.Equal("12345", json);
        }

        [Fact]
        public void UIntPtr_Serialize_Zero()
        {
            var value = UIntPtr.Zero;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UIntPtr zero: {json}");
            Assert.Equal("0", json);
        }

        [Fact]
        public void UIntPtr_Deserialize_FromNumber()
        {
            var json = "67890";
            var result = _reflector.JsonSerializer.Deserialize<UIntPtr>(json);
            _output.WriteLine($"UIntPtr from number: {result}");
            Assert.Equal(new UIntPtr(67890), result);
        }

        [Fact]
        public void UIntPtr_Deserialize_FromString()
        {
            var json = "\"98765\"";
            var result = _reflector.JsonSerializer.Deserialize<UIntPtr>(json);
            _output.WriteLine($"UIntPtr from string: {result}");
            Assert.Equal(new UIntPtr(98765), result);
        }

        [Fact]
        public void UIntPtr_RoundTrip()
        {
            var original = new UIntPtr(123456789);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<UIntPtr>(json);
            _output.WriteLine($"UIntPtr roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion
    }
}
