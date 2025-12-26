using System;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for common type JSON converters: Version, Uri
    /// </summary>
    public class CommonTypesConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public CommonTypesConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region VersionJsonConverter Tests

        [Fact]
        public void Version_Write_TwoComponents()
        {
            var value = new Version(1, 0);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Version 2 components: {json}");
            Assert.Equal("\"1.0\"", json);
        }

        [Fact]
        public void Version_Write_ThreeComponents()
        {
            var value = new Version(1, 2, 3);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Version 3 components: {json}");
            Assert.Equal("\"1.2.3\"", json);
        }

        [Fact]
        public void Version_Write_FourComponents()
        {
            var value = new Version(1, 2, 3, 4);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Version 4 components: {json}");
            Assert.Equal("\"1.2.3.4\"", json);
        }

        [Fact]
        public void Version_Write_LargeNumbers()
        {
            var value = new Version(999, 888, 777, 666);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Version large numbers: {json}");
            Assert.Equal("\"999.888.777.666\"", json);
        }

        [Fact]
        public void Version_Write_Null()
        {
            Version? value = null;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Version null: {json}");
            Assert.Equal("null", json);
        }

        [Fact]
        public void Version_Read_TwoComponents()
        {
            var json = "\"2.0\"";
            var result = _reflector.JsonSerializer.Deserialize<Version>(json);
            _output.WriteLine($"Version from 2 components: {result}");
            Assert.NotNull(result);
            Assert.Equal(2, result.Major);
            Assert.Equal(0, result.Minor);
        }

        [Fact]
        public void Version_Read_ThreeComponents()
        {
            var json = "\"3.2.1\"";
            var result = _reflector.JsonSerializer.Deserialize<Version>(json);
            _output.WriteLine($"Version from 3 components: {result}");
            Assert.NotNull(result);
            Assert.Equal(3, result.Major);
            Assert.Equal(2, result.Minor);
            Assert.Equal(1, result.Build);
        }

        [Fact]
        public void Version_Read_FourComponents()
        {
            var json = "\"4.3.2.1\"";
            var result = _reflector.JsonSerializer.Deserialize<Version>(json);
            _output.WriteLine($"Version from 4 components: {result}");
            Assert.NotNull(result);
            Assert.Equal(4, result.Major);
            Assert.Equal(3, result.Minor);
            Assert.Equal(2, result.Build);
            Assert.Equal(1, result.Revision);
        }

        [Fact]
        public void Version_Read_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<Version>(json);
            _output.WriteLine($"Version from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Version_Read_EmptyString_ReturnsNull()
        {
            var json = "\"\"";
            var result = _reflector.JsonSerializer.Deserialize<Version>(json);
            _output.WriteLine($"Version from empty: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Version_Read_WhitespaceString_ReturnsNull()
        {
            var json = "\"   \"";
            var result = _reflector.JsonSerializer.Deserialize<Version>(json);
            _output.WriteLine($"Version from whitespace: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Version_Read_InvalidFormat_Throws()
        {
            var json = "\"not.a.version\"";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<Version>(json));
        }

        [Fact]
        public void Version_Read_WrongTokenType_Throws()
        {
            var json = "123";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<Version>(json));
        }

        [Fact]
        public void Version_RoundTrip_TwoComponents()
        {
            var original = new Version(5, 6);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<Version>(json);
            _output.WriteLine($"Version roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        [Fact]
        public void Version_RoundTrip_FourComponents()
        {
            var original = new Version(1, 2, 3, 4);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<Version>(json);
            _output.WriteLine($"Version roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region UriJsonConverter Tests

        [Fact]
        public void Uri_Write_HttpsUrl()
        {
            var value = new Uri("https://example.com/path");
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Uri https: {json}");
            Assert.Equal("\"https://example.com/path\"", json);
        }

        [Fact]
        public void Uri_Write_HttpUrl()
        {
            var value = new Uri("http://localhost:8080/api");
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Uri http: {json}");
            Assert.Equal("\"http://localhost:8080/api\"", json);
        }

        [Fact]
        public void Uri_Write_FileUrl()
        {
            var value = new Uri("file:///C:/path/to/file.txt");
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Uri file: {json}");
            Assert.Contains("file:///", json);
        }

        [Fact]
        public void Uri_Write_RelativeUrl()
        {
            var value = new Uri("/api/endpoint", UriKind.Relative);
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Uri relative: {json}");
            Assert.Equal("\"/api/endpoint\"", json);
        }

        [Fact]
        public void Uri_Write_UrlWithQueryString()
        {
            var value = new Uri("https://example.com/search?q=test&page=1");
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Uri with query: {json}");
            Assert.Contains("q=test", json);
            Assert.Contains("page=1", json);
        }

        [Fact]
        public void Uri_Write_UrlWithFragment()
        {
            var value = new Uri("https://example.com/page#section");
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Uri with fragment: {json}");
            Assert.Contains("#section", json);
        }

        [Fact]
        public void Uri_Write_Null()
        {
            Uri? value = null;
            var json = _reflector.JsonSerializer.Serialize(value);
            _output.WriteLine($"Uri null: {json}");
            Assert.Equal("null", json);
        }

        [Fact]
        public void Uri_Read_HttpsUrl()
        {
            var json = "\"https://example.com/test\"";
            var result = _reflector.JsonSerializer.Deserialize<Uri>(json);
            _output.WriteLine($"Uri from https: {result}");
            Assert.NotNull(result);
            Assert.Equal("https", result.Scheme);
            Assert.Equal("example.com", result.Host);
        }

        [Fact]
        public void Uri_Read_RelativeUrl()
        {
            var json = "\"/api/users\"";
            var result = _reflector.JsonSerializer.Deserialize<Uri>(json);
            _output.WriteLine($"Uri from relative: {result}");
            Assert.NotNull(result);
            Assert.False(result.IsAbsoluteUri);
        }

        [Fact]
        public void Uri_Read_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<Uri>(json);
            _output.WriteLine($"Uri from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Uri_Read_EmptyString_ReturnsNull()
        {
            var json = "\"\"";
            var result = _reflector.JsonSerializer.Deserialize<Uri>(json);
            _output.WriteLine($"Uri from empty: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Uri_Read_WhitespaceString_ReturnsNull()
        {
            var json = "\"   \"";
            var result = _reflector.JsonSerializer.Deserialize<Uri>(json);
            _output.WriteLine($"Uri from whitespace: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void Uri_Read_WrongTokenType_Throws()
        {
            var json = "12345";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<Uri>(json));
        }

        [Fact]
        public void Uri_RoundTrip_AbsoluteUrl()
        {
            var original = new Uri("https://api.example.com:8443/v1/users?active=true");
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<Uri>(json);
            _output.WriteLine($"Uri roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        [Fact]
        public void Uri_RoundTrip_RelativeUrl()
        {
            var original = new Uri("/path/to/resource", UriKind.Relative);
            var json = _reflector.JsonSerializer.Serialize(original);
            var deserialized = _reflector.JsonSerializer.Deserialize<Uri>(json);
            _output.WriteLine($"Uri roundtrip relative: {original} -> {json} -> {deserialized}");
            Assert.Equal(original.OriginalString, deserialized?.OriginalString);
        }

        [Fact]
        public void Uri_Read_EncodedCharacters()
        {
            var json = "\"https://example.com/path%20with%20spaces\"";
            var result = _reflector.JsonSerializer.Deserialize<Uri>(json);
            _output.WriteLine($"Uri with encoded chars: {result}");
            Assert.NotNull(result);
            Assert.Contains("path", result.AbsolutePath);
        }

        [Fact]
        public void Uri_Read_UnicodeCharacters()
        {
            var json = "\"https://example.com/\u4E2D\u6587\"";
            var result = _reflector.JsonSerializer.Deserialize<Uri>(json);
            _output.WriteLine($"Uri with unicode: {result}");
            Assert.NotNull(result);
        }

        #endregion
    }
}
