using System;
using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.JsonConverterTests
{
    /// <summary>
    /// Tests for network type JSON converters: IPAddress, IPEndPoint
    /// </summary>
    public class NetworkTypesConverterTests : BaseTest
    {
        private readonly Reflector _reflector;

        public NetworkTypesConverterTests(ITestOutputHelper output) : base(output)
        {
            _reflector = new Reflector();
        }

        #region IPAddressJsonConverter Tests

        [Fact]
        public void IPAddress_Serialize_IPv4()
        {
            var value = IPAddress.Parse("192.168.1.1");
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPAddress));
            _output.WriteLine($"IPAddress IPv4: {json}");
            Assert.Contains("192.168.1.1", json);
        }

        [Fact]
        public void IPAddress_Serialize_Loopback()
        {
            var value = IPAddress.Parse("127.0.0.1");
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPAddress));
            _output.WriteLine($"IPAddress loopback: {json}");
            Assert.Contains("127.0.0.1", json);
        }

        [Fact]
        public void IPAddress_Serialize_Any()
        {
            var value = IPAddress.Parse("0.0.0.0");
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPAddress));
            _output.WriteLine($"IPAddress any: {json}");
            Assert.Contains("0.0.0.0", json);
        }

        [Fact]
        public void IPAddress_Serialize_IPv6()
        {
            var value = IPAddress.Parse("::1");
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPAddress));
            _output.WriteLine($"IPAddress IPv6 loopback: {json}");
            Assert.Contains("::1", json);
        }

        [Fact]
        public void IPAddress_Serialize_IPv6_Full()
        {
            var value = IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334");
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPAddress));
            _output.WriteLine($"IPAddress IPv6 full: {json}");
            Assert.Contains("2001:", json);
        }

        [Fact]
        public void IPAddress_Serialize_Null()
        {
            IPAddress? value = null;
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPAddress));
            _output.WriteLine($"IPAddress null: {json}");
            Assert.Equal("null", json);
        }

        [Fact]
        public void IPAddress_Read_IPv4()
        {
            var json = "\"10.0.0.1\"";
            var result = _reflector.JsonSerializer.Deserialize<IPAddress>(json);
            _output.WriteLine($"IPAddress from IPv4: {result}");
            Assert.NotNull(result);
            Assert.Equal(IPAddress.Parse("10.0.0.1"), result);
        }

        [Fact]
        public void IPAddress_Read_IPv6()
        {
            var json = "\"::1\"";
            var result = _reflector.JsonSerializer.Deserialize<IPAddress>(json);
            _output.WriteLine($"IPAddress from IPv6: {result}");
            Assert.NotNull(result);
            Assert.Equal(IPAddress.IPv6Loopback, result);
        }

        [Fact]
        public void IPAddress_Read_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<IPAddress>(json);
            _output.WriteLine($"IPAddress from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void IPAddress_Read_InvalidFormat_Throws()
        {
            var json = "\"not.an.ip\"";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<IPAddress>(json));
        }

        [Fact]
        public void IPAddress_Read_WrongTokenType_Throws()
        {
            var json = "12345";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<IPAddress>(json));
        }

        [Fact]
        public void IPAddress_RoundTrip_IPv4()
        {
            var original = IPAddress.Parse("172.16.0.1");
            var json = _reflector.JsonSerializer.Serialize(original, typeof(IPAddress));
            var deserialized = _reflector.JsonSerializer.Deserialize<IPAddress>(json);
            _output.WriteLine($"IPAddress roundtrip IPv4: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        [Fact]
        public void IPAddress_RoundTrip_IPv6()
        {
            var original = IPAddress.Parse("fe80::1");
            var json = _reflector.JsonSerializer.Serialize(original, typeof(IPAddress));
            var deserialized = _reflector.JsonSerializer.Deserialize<IPAddress>(json);
            _output.WriteLine($"IPAddress roundtrip IPv6: {original} -> {json} -> {deserialized}");
            Assert.Equal(original, deserialized);
        }

        #endregion

        #region IPEndPointJsonConverter Tests

        [Fact]
        public void IPEndPoint_Serialize_IPv4()
        {
            var value = new IPEndPoint(IPAddress.Parse("192.168.1.1"), 8080);
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPEndPoint));
            _output.WriteLine($"IPEndPoint IPv4: {json}");
            Assert.Contains("\"address\"", json);
            Assert.Contains("192.168.1.1", json);
            Assert.Contains("\"port\"", json);
            Assert.Contains("8080", json);
        }

        [Fact]
        public void IPEndPoint_Serialize_IPv6()
        {
            var value = new IPEndPoint(IPAddress.Parse("::1"), 443);
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPEndPoint));
            _output.WriteLine($"IPEndPoint IPv6: {json}");
            Assert.Contains("\"address\"", json);
            Assert.Contains("::1", json);
            Assert.Contains("\"port\"", json);
            Assert.Contains("443", json);
        }

        [Fact]
        public void IPEndPoint_Serialize_Port0()
        {
            var value = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPEndPoint));
            _output.WriteLine($"IPEndPoint port 0: {json}");
            Assert.Contains("\"port\"", json);
        }

        [Fact]
        public void IPEndPoint_Serialize_MaxPort()
        {
            var value = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 65535);
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPEndPoint));
            _output.WriteLine($"IPEndPoint max port: {json}");
            Assert.Contains("\"port\"", json);
            Assert.Contains("65535", json);
        }

        [Fact]
        public void IPEndPoint_Serialize_Null()
        {
            IPEndPoint? value = null;
            var json = _reflector.JsonSerializer.Serialize(value, typeof(IPEndPoint));
            _output.WriteLine($"IPEndPoint null: {json}");
            Assert.Equal("null", json);
        }

        [Fact]
        public void IPEndPoint_Read_IPv4()
        {
            var json = "{\"address\":\"10.0.0.1\",\"port\":3000}";
            var result = _reflector.JsonSerializer.Deserialize<IPEndPoint>(json);
            _output.WriteLine($"IPEndPoint from IPv4: {result}");
            Assert.NotNull(result);
            Assert.Equal(IPAddress.Parse("10.0.0.1"), result.Address);
            Assert.Equal(3000, result.Port);
        }

        [Fact]
        public void IPEndPoint_Read_IPv6()
        {
            var json = "{\"address\":\"::1\",\"port\":8443}";
            var result = _reflector.JsonSerializer.Deserialize<IPEndPoint>(json);
            _output.WriteLine($"IPEndPoint from IPv6: {result}");
            Assert.NotNull(result);
            Assert.Equal(IPAddress.IPv6Loopback, result.Address);
            Assert.Equal(8443, result.Port);
        }

        [Fact]
        public void IPEndPoint_Read_CaseInsensitive()
        {
            var json = "{\"ADDRESS\":\"127.0.0.1\",\"PORT\":80}";
            var result = _reflector.JsonSerializer.Deserialize<IPEndPoint>(json);
            _output.WriteLine($"IPEndPoint case insensitive: {result}");
            Assert.NotNull(result);
            Assert.Equal(IPAddress.Loopback, result.Address);
            Assert.Equal(80, result.Port);
        }

        [Fact]
        public void IPEndPoint_Read_Null()
        {
            var json = "null";
            var result = _reflector.JsonSerializer.Deserialize<IPEndPoint>(json);
            _output.WriteLine($"IPEndPoint from null: {result}");
            Assert.Null(result);
        }

        [Fact]
        public void IPEndPoint_Read_MissingAddress_Throws()
        {
            var json = "{\"port\":8080}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<IPEndPoint>(json));
        }

        [Fact]
        public void IPEndPoint_Read_MissingPort_Throws()
        {
            var json = "{\"address\":\"127.0.0.1\"}";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<IPEndPoint>(json));
        }

        [Fact]
        public void IPEndPoint_Read_WrongTokenType_Throws()
        {
            var json = "\"127.0.0.1:8080\"";
            Assert.ThrowsAny<JsonException>(() => _reflector.JsonSerializer.Deserialize<IPEndPoint>(json));
        }

        [Fact]
        public void IPEndPoint_RoundTrip_IPv4()
        {
            var original = new IPEndPoint(IPAddress.Parse("192.168.0.1"), 5000);
            var json = _reflector.JsonSerializer.Serialize(original, typeof(IPEndPoint));
            var deserialized = _reflector.JsonSerializer.Deserialize<IPEndPoint>(json);
            _output.WriteLine($"IPEndPoint roundtrip: {original} -> {json} -> {deserialized}");
            Assert.Equal(original.Address, deserialized?.Address);
            Assert.Equal(original.Port, deserialized?.Port);
        }

        [Fact]
        public void IPEndPoint_RoundTrip_IPv6()
        {
            var original = new IPEndPoint(IPAddress.Parse("fe80::1"), 9000);
            var json = _reflector.JsonSerializer.Serialize(original, typeof(IPEndPoint));
            var deserialized = _reflector.JsonSerializer.Deserialize<IPEndPoint>(json);
            _output.WriteLine($"IPEndPoint roundtrip IPv6: {original} -> {json} -> {deserialized}");
            Assert.Equal(original.Address, deserialized?.Address);
            Assert.Equal(original.Port, deserialized?.Port);
        }

        [Fact]
        public void IPEndPoint_Read_ExtraProperties_Ignored()
        {
            var json = "{\"address\":\"127.0.0.1\",\"port\":80,\"extra\":\"ignored\"}";
            var result = _reflector.JsonSerializer.Deserialize<IPEndPoint>(json);
            _output.WriteLine($"IPEndPoint with extra props: {result}");
            Assert.NotNull(result);
            Assert.Equal(80, result.Port);
        }

        #endregion
    }
}
