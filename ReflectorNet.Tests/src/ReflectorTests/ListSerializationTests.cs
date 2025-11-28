using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class ListSerializationTests : BaseTest
    {
        public ListSerializationTests(ITestOutputHelper output) : base(output) { }

        public class ClassWithList
        {
            public List<int>? IntList { get; set; }
        }

        [Fact]
        public void TestListDeserialization()
        {
            var original = new ClassWithList { IntList = new List<int> { 1, 2, 3 } };
            var reflector = new Reflector();
            var serialized = reflector.Serialize(original);
            var deserialized = reflector.Deserialize<ClassWithList>(serialized);

            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.IntList);
            Assert.Equal(original.IntList.Count, deserialized.IntList.Count);
            Assert.Equal(original.IntList[0], deserialized.IntList[0]);
        }
    }
}
