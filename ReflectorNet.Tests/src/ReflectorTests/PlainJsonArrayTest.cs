using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class PlainJsonArrayTest : BaseTest
    {
        public PlainJsonArrayTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void TestPlainJsonArrayDeserialization()
        {
            var json = "[1, 2, 3]";
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var serializedMember = new SerializedMember
            {
                typeName = "System.Collections.Generic.List`1[[System.Int32]]",
                valueJsonElement = root
            };

            var reflector = new Reflector();
            var result = reflector.Deserialize<List<int>>(serializedMember);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(3, result[2]);
        }

        [Fact]
        public void TestSerializedMemberArrayDeserialization()
        {
            // Construct a JSON that looks like what Reflector produces
            // Array of SerializedMember objects
            var json = @"[
                { ""typeName"": ""System.Int32"", ""value"": 1 },
                { ""typeName"": ""System.Int32"", ""value"": 2 }
            ]";
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var serializedMember = new SerializedMember
            {
                typeName = "System.Collections.Generic.List`1[[System.Int32]]",
                valueJsonElement = root
            };

            var reflector = new Reflector();
            var result = reflector.Deserialize<List<int>>(serializedMember);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
        }
    }
}
