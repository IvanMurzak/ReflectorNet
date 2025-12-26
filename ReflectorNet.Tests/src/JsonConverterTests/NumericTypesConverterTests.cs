using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for standard numeric types JSON converters: Byte, SByte, Int16, Int32, Int64, Single, Double, Decimal
    /// </summary>
    public class NumericTypesConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public NumericTypesConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region ByteJsonConverter Tests

        [Fact]
        public void Byte_Serialize_Value()
        {
            byte value = 255;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Equal("255", json);
        }

        [Fact]
        public void Byte_Deserialize_Value()
        {
            var json = "128";
            var result = _reflector.JsonSerializer.Deserialize<byte>(json);
            Assert.Equal((byte)128, result);
        }

        #endregion

        #region SByteJsonConverter Tests

        [Fact]
        public void SByte_Serialize_Value()
        {
            sbyte value = -128;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Equal("-128", json);
        }

        [Fact]
        public void SByte_Deserialize_Value()
        {
            var json = "-50";
            var result = _reflector.JsonSerializer.Deserialize<sbyte>(json);
            Assert.Equal((sbyte)-50, result);
        }

        #endregion

        #region Int16JsonConverter Tests

        [Fact]
        public void Int16_Serialize_Value()
        {
            short value = -32000;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Equal("-32000", json);
        }

        [Fact]
        public void Int16_Deserialize_Value()
        {
            var json = "32000";
            var result = _reflector.JsonSerializer.Deserialize<short>(json);
            Assert.Equal((short)32000, result);
        }

        #endregion

        #region Int32JsonConverter Tests

        [Fact]
        public void Int32_Serialize_Value()
        {
            int value = -2000000000;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Equal("-2000000000", json);
        }

        [Fact]
        public void Int32_Deserialize_Value()
        {
            var json = "2000000000";
            var result = _reflector.JsonSerializer.Deserialize<int>(json);
            Assert.Equal(2000000000, result);
        }

        #endregion

        #region Int64JsonConverter Tests

        [Fact]
        public void Int64_Serialize_Value()
        {
            long value = -9000000000000000000;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Contains("-9000000000000000000", json);
        }

        [Fact]
        public void Int64_Deserialize_Value()
        {
            var json = "9000000000000000000";
            var result = _reflector.JsonSerializer.Deserialize<long>(json);
            Assert.Equal(9000000000000000000, result);
        }

        #endregion

        #region SingleJsonConverter Tests

        [Fact]
        public void Single_Serialize_Value()
        {
            float value = 3.14f;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Contains("3.14", json);
        }

        [Fact]
        public void Single_Deserialize_Value()
        {
            var json = "1.5";
            var result = _reflector.JsonSerializer.Deserialize<float>(json);
            Assert.Equal(1.5f, result);
        }

        #endregion

        #region DoubleJsonConverter Tests

        [Fact]
        public void Double_Serialize_Value()
        {
            double value = 3.14159265359;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Contains("3.14159265359", json);
        }

        [Fact]
        public void Double_Deserialize_Value()
        {
            var json = "2.71828";
            var result = _reflector.JsonSerializer.Deserialize<double>(json);
            Assert.Equal(2.71828, result);
        }

        #endregion

        #region DecimalJsonConverter Tests

        [Fact]
        public void Decimal_Serialize_Value()
        {
            decimal value = 123.456m;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Contains("123.456", json);
        }

        [Fact]
        public void Decimal_Deserialize_Value()
        {
            var json = "789.012";
            var result = _reflector.JsonSerializer.Deserialize<decimal>(json);
            Assert.Equal(789.012m, result);
        }

        #endregion
    }
}
