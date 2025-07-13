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
            var schema = JsonUtils.GetArgumentsSchema(methodInfo, justRef: false)!;

            _output.WriteLine(schema.ToString());

            Assert.NotNull(schema);
            Assert.NotNull(schema[JsonUtils.SchemaDefs]);

            var defines = schema[JsonUtils.SchemaDefs]?.AsObject();
            Assert.NotNull(defines);

            var targetSchema = defines[typeof(SerializedMemberList).FullName!];
            Assert.NotNull(targetSchema);
        }
    }
}
