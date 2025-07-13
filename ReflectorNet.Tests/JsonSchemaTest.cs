using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace ReflectorNet.Tests
{
    public class JsonSchemaTest : BaseTest
    {
        public JsonSchemaTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SerializedMemberList()
        {
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.SerializedMemberList_ReturnString))!;
            var schema = JsonUtils.Schema.GetArgumentsSchema(methodInfo, justRef: false)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonUtils.Schema.Defs]);

            var defines = schema[JsonUtils.Schema.Defs]?.AsObject();
            Assert.NotNull(defines);

            var targetSchema = defines[typeof(SerializedMemberList).FullName!];
            Assert.NotNull(targetSchema);
        }
    }
}
