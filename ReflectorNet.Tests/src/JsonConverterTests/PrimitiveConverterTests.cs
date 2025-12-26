using System;
using System.Numerics;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for primitive type JSON converters: IntPtr, UIntPtr, Char, BigInteger, Complex, Half
    /// </summary>
    public class PrimitiveConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public PrimitiveConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region IntPtrJsonConverter Tests

        [Fact]
        public void IntPtr_Write_PositiveValue()
        {
            var value = new IntPtr(12345);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"IntPtr positive: {json}");
            Assert.Equal("12345", json);
        }

        [Fact]
        public void IntPtr_Write_NegativeValue()
        {
            var value = new IntPtr(-98765);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"IntPtr negative: {json}");
            Assert.Equal("-98765", json);
        }

        [Fact]
        public void IntPtr_Write_Zero()
        {
            var value = IntPtr.Zero;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"IntPtr zero: {json}");
            Assert.Equal("0", json);
        }

        [Fact]
        public void IntPtr_Write_MaxValue()
        {
            var value = new IntPtr(long.MaxValue);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"IntPtr max: {json}");
            Assert.Equal(long.MaxValue.ToString(), json);
        }

        [Fact]
        public void IntPtr_Write_MinValue()
        {
            var value = new IntPtr(long.MinValue);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"IntPtr min: {json}");
            Assert.Equal(long.MinValue.ToString(), json);
        }

        [Fact]
        public void IntPtr_Read_FromNumber()
        {
            var json = "42";
            var result = _reflector.JsonSerializer.Deserialize<IntPtr>(json);
            _output.WriteLine($"IntPtr from number: {result}");
            Assert.Equal(new IntPtr(42), result);
        }

        [Fact]
        public void IntPtr_Read_FromString()
        {
            var json = "\"99999\"";
            var result = _reflector.JsonSerializer.Deserialize<IntPtr>(json);
            _output.WriteLine($"IntPtr from string: {result}");
            Assert.Equal(new IntPtr(99999), result);
        }

        [Fact]
        public void IntPtr_Read_Null_AsNullable_ReturnsNull()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<IntPtr?>(json);
            _output.WriteLine($"IntPtr? from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void IntPtr_RoundTrip()
        {
            var original = new IntPtr(123456789);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<IntPtr>(json);
            _output.WriteLine($"IntPtr roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region UIntPtrJsonConverter Tests

        [Fact]
        public void UIntPtr_Write_PositiveValue()
        {
            var value = new UIntPtr(12345);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UIntPtr positive: {json}");
            Assert.Equal("12345", json);
        }

        [Fact]
        public void UIntPtr_Write_Zero()
        {
            var value = UIntPtr.Zero;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UIntPtr zero: {json}");
            Assert.Equal("0", json);
        }

        [Fact]
        public void UIntPtr_Write_MaxValue()
        {
            var value = new UIntPtr(ulong.MaxValue);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"UIntPtr max: {json}");
            Assert.Equal(ulong.MaxValue.ToString(), json);
        }

        [Fact]
        public void UIntPtr_Read_FromNumber()
        {
            var json = "42";
            var result = _reflector.JsonSerializer.Deserialize<UIntPtr>(json);
            _output.WriteLine($"UIntPtr from number: {result}");
            Assert.Equal(new UIntPtr(42), result);
        }

        [Fact]
        public void UIntPtr_Read_FromString()
        {
            var json = "\"99999\"";
            var result = _reflector.JsonSerializer.Deserialize<UIntPtr>(json);
            _output.WriteLine($"UIntPtr from string: {result}");
            Assert.Equal(new UIntPtr(99999), result);
        }

        [Fact]
        public void UIntPtr_Read_Null_AsNullable_ReturnsNull()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<UIntPtr?>(json);
            _output.WriteLine($"UIntPtr? from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void UIntPtr_RoundTrip()
        {
            var original = new UIntPtr(987654321);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<UIntPtr>(json);
            _output.WriteLine($"UIntPtr roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region CharJsonConverter Tests

        [Fact]
        public void Char_Write_Letter()
        {
            var value = 'A';
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Char letter: {json}");
            Assert.Equal("\"A\"", json);
        }

        [Fact]
        public void Char_Write_Digit()
        {
            var value = '7';
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Char digit: {json}");
            Assert.Equal("\"7\"", json);
        }

        [Fact]
        public void Char_Write_SpecialChar()
        {
            var value = '@';
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Char special: {json}");
            Assert.Equal("\"@\"", json);
        }

        [Fact]
        public void Char_Write_UnicodeChar()
        {
            var value = '\u00E9'; // é
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Char unicode: {json}");
            Assert.Contains("\\u00E9", json, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Char_Read_FromString()
        {
            var json = "\"X\"";
            var result = _reflector.JsonSerializer.Deserialize<char>(json);
            _output.WriteLine($"Char from string: {result}");
            Assert.Equal('X', result);
        }

        [Fact]
        public void Char_Read_FromNumber()
        {
            var json = "65"; // ASCII for 'A'
            var result = _reflector.JsonSerializer.Deserialize<char>(json);
            _output.WriteLine($"Char from number: {result}");
            Assert.Equal('A', result);
        }

        [Fact]
        public void Char_Read_EmptyString_ReturnsDefault()
        {
            var json = "\"\"";
            var result = _reflector.JsonSerializer.Deserialize<char>(json);
            _output.WriteLine($"Char from empty string: '{result}'");
            Assert.Equal(default(char), result);
        }

        [Fact]
        public void Char_Read_MultipleChars_Throws()
        {
            var json = "\"AB\"";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<char>(json));
        }

        [Fact]
        public void Char_RoundTrip()
        {
            var original = 'Z';
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<char>(json);
            _output.WriteLine($"Char roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region BigIntegerJsonConverter Tests

        [Fact]
        public void BigInteger_Write_PositiveValue()
        {
            var value = new BigInteger(123456789);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"BigInteger positive: {json}");
            Assert.Equal("\"123456789\"", json);
        }

        [Fact]
        public void BigInteger_Write_NegativeValue()
        {
            var value = new BigInteger(-987654321);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"BigInteger negative: {json}");
            Assert.Equal("\"-987654321\"", json);
        }

        [Fact]
        public void BigInteger_Write_Zero()
        {
            var value = BigInteger.Zero;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"BigInteger zero: {json}");
            Assert.Equal("\"0\"", json);
        }

        [Fact]
        public void BigInteger_Write_VeryLargeValue()
        {
            var value = BigInteger.Parse("123456789012345678901234567890");
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"BigInteger very large: {json}");
            Assert.Equal("\"123456789012345678901234567890\"", json);
        }

        [Fact]
        public void BigInteger_Read_FromString()
        {
            var json = "\"999999999999999999999\"";
            var result = _reflector.JsonSerializer.Deserialize<BigInteger>(json);
            _output.WriteLine($"BigInteger from string: {result}");
            Assert.Equal(BigInteger.Parse("999999999999999999999"), result);
        }

        [Fact]
        public void BigInteger_Read_FromNumber()
        {
            var json = "12345";
            var result = _reflector.JsonSerializer.Deserialize<BigInteger>(json);
            _output.WriteLine($"BigInteger from number: {result}");
            Assert.Equal(new BigInteger(12345), result);
        }

        [Fact]
        public void BigInteger_Read_Null_ReturnsDefault()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<BigInteger>(json);
            _output.WriteLine($"BigInteger from null: {result}");
            Assert.Equal(BigInteger.Zero, result);
        }

        [Fact]
        public void BigInteger_RoundTrip()
        {
            var original = BigInteger.Parse("12345678901234567890");
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<BigInteger>(json);
            _output.WriteLine($"BigInteger roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region ComplexJsonConverter Tests

        [Fact]
        public void Complex_Write_PositiveValues()
        {
            var value = new Complex(3.5, 4.5);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Complex positive: {json}");
            Assert.Contains("\"real\"", json);
            Assert.Contains("3.5", json);
            Assert.Contains("\"imaginary\"", json);
            Assert.Contains("4.5", json);
        }

        [Fact]
        public void Complex_Write_NegativeValues()
        {
            var value = new Complex(-1.5, -2.5);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Complex negative: {json}");
            Assert.Contains("\"real\"", json);
            Assert.Contains("-1.5", json);
            Assert.Contains("\"imaginary\"", json);
            Assert.Contains("-2.5", json);
        }

        [Fact]
        public void Complex_Write_Zero()
        {
            var value = Complex.Zero;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Complex zero: {json}");
            Assert.Contains("\"real\"", json);
            Assert.Contains("\"imaginary\"", json);
        }

        [Fact]
        public void Complex_Write_ImaginaryOnly()
        {
            var value = new Complex(0, 5.0);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Complex imaginary only: {json}");
            Assert.Contains("\"real\"", json);
            Assert.Contains("5", json);
        }

        [Fact]
        public void Complex_Read_ValidJson()
        {
            var json = "{\"real\":2.5,\"imaginary\":3.5}";
            var result = _reflector.JsonSerializer.Deserialize<Complex>(json);
            _output.WriteLine($"Complex from json: {result}");
            Assert.Equal(2.5, result.Real);
            Assert.Equal(3.5, result.Imaginary);
        }

        [Fact]
        public void Complex_Read_CaseInsensitive()
        {
            var json = "{\"REAL\":1.0,\"IMAGINARY\":2.0}";
            var result = _reflector.JsonSerializer.Deserialize<Complex>(json);
            _output.WriteLine($"Complex case insensitive: {result}");
            Assert.Equal(1.0, result.Real);
            Assert.Equal(2.0, result.Imaginary);
        }

        [Fact]
        public void Complex_Read_MissingProperty_Throws()
        {
            var json = "{\"real\":1.0}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<Complex>(json));
        }

        [Fact]
        public void Complex_Read_Null_Throws()
        {
            var json = "null";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<Complex>(json));
        }

        [Fact]
        public void Complex_RoundTrip()
        {
            var original = new Complex(7.25, -3.75);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<Complex>(json);
            _output.WriteLine($"Complex roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        [Fact]
        public void Complex_Read_ExtraProperties_Ignored()
        {
            var json = "{\"real\":1.0,\"imaginary\":2.0,\"extra\":\"ignored\"}";
            var result = _reflector.JsonSerializer.Deserialize<Complex>(json);
            _output.WriteLine($"Complex with extra props: {result}");
            Assert.Equal(1.0, result.Real);
            Assert.Equal(2.0, result.Imaginary);
        }

        #endregion

#if NET6_0_OR_GREATER
        #region HalfJsonConverter Tests

        [Fact]
        public void Half_Write_PositiveValue()
        {
            var value = (Half)3.14;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Half positive: {json}");
            Assert.Contains("3.14", json);
        }

        [Fact]
        public void Half_Write_NegativeValue()
        {
            var value = (Half)(-2.5);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Half negative: {json}");
            Assert.Contains("-2.5", json);
        }

        [Fact]
        public void Half_Write_Zero()
        {
            var value = (Half)0;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Half zero: {json}");
            Assert.Equal("0", json);
        }

        [Fact]
        public void Half_Read_FromNumber()
        {
            var json = "1.5";
            var result = _reflector.JsonSerializer.Deserialize<Half>(json);
            _output.WriteLine($"Half from number: {result}");
            Assert.Equal((Half)1.5, result);
        }

        [Fact]
        public void Half_Read_FromString()
        {
            var json = "\"2.25\"";
            var result = _reflector.JsonSerializer.Deserialize<Half>(json);
            _output.WriteLine($"Half from string: {result}");
            Assert.Equal((Half)2.25, result);
        }

        [Fact]
        public void Half_RoundTrip()
        {
            var original = (Half)5.5;
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<Half>(json);
            _output.WriteLine($"Half roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion
#endif
    }
}
