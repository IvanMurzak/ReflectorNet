using Xunit;
using com.IvanMurzak.ReflectorNet.Model;
using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.ReflectorTests
{
    public class BlacklistedArrayItemTests
    {
        private class BaseItem { }
        private class AllowedItem : BaseItem { public int Id = 1; }
        private class BlacklistedItem : BaseItem { public int Secret = 999; }

        [Fact]
        public void TestBlacklistedItemInArray()
        {
            var reflector = new Reflector();
            reflector.Converters.BlacklistType(typeof(BlacklistedItem));

            var array = new BaseItem[]
            {
                new AllowedItem(),
                new BlacklistedItem(),
                new AllowedItem()
            };

            var serialized = reflector.Serialize(array);
            var json = serialized.valueJsonElement?.GetRawText();

            Assert.NotNull(json);

            // Check if the second element is null
            using (var doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                Assert.Equal(JsonValueKind.Array, root.ValueKind);
                Assert.Equal(3, root.GetArrayLength());

                Assert.NotEqual(JsonValueKind.Null, root[0].ValueKind);

                // This is what we want to verify:
                Assert.Equal(JsonValueKind.Null, root[1].ValueKind);

                Assert.NotEqual(JsonValueKind.Null, root[2].ValueKind);
            }
        }

        [Fact]
        public void TestSerializedMemberListToStringWithNull()
        {
            var list = new SerializedMemberList();
            list.Add(null!);
            var str = list.ToString();
            Assert.Contains("Item[0]", str);
        }
    }
}
