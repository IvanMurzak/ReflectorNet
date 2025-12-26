using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for Bool and Char JSON converters
    /// </summary>
    public class BasicTypesConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public BasicTypesConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region BoolJsonConverter Tests

        [Fact]
        public void Bool_Serialize_True()
        {
            var value = true;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Equal("true", json);
        }

        [Fact]
        public void Bool_Serialize_False()
        {
            var value = false;
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Equal("false", json);
        }

        [Fact]
        public void Bool_Deserialize_True()
        {
            var json = "true";
            var result = _reflector.JsonSerializer.Deserialize<bool>(json);
            Assert.True(result);
        }

        [Fact]
        public void Bool_Deserialize_False()
        {
            var json = "false";
            var result = _reflector.JsonSerializer.Deserialize<bool>(json);
            Assert.False(result);
        }

        [Fact]
        public void Bool_Deserialize_FromString()
        {
            var json = "\"true\"";
            var result = _reflector.JsonSerializer.Deserialize<bool>(json);
            Assert.True(result);
        }

        #endregion

        #region CharJsonConverter Tests

        [Fact]
        public void Char_Serialize_Value()
        {
            var value = 'A';
            var json = _reflector.JsonSerializer.Serialize(value);
            Assert.Equal("\"A\"", json);
        }

        [Fact]
        public void Char_Deserialize_Value()
        {
            var json = "\"B\"";
            var result = _reflector.JsonSerializer.Deserialize<char>(json);
            Assert.Equal('B', result);
        }

        [Fact]
        public void Char_Deserialize_FromNumber()
        {
            var json = "65";
            var result = _reflector.JsonSerializer.Deserialize<char>(json);
            Assert.Equal('A', result);
        }

        #endregion
    }
}
