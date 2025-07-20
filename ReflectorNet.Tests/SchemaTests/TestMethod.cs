using com.IvanMurzak.ReflectorNet.Model;
using ReflectorNet.Tests.Schema.Model;
using Xunit.Abstractions;

namespace ReflectorNet.Tests.SchemaTests
{
    public partial class TestMethod : BaseTest
    {
        public TestMethod(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Parameters_Object_Int_Bool()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;

            TestMethodInputs_Defines(methodInfo,
                typeof(GameObjectRef));

            TestMethodInputs_PropertyRefs(methodInfo,
                "obj");
        }
        [Fact]
        public void Parameters_ListObject_ListObject()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.ListObject_ListObject))!;

            TestMethodInputs_Defines(methodInfo,
                typeof(GameObjectRef),
                typeof(GameObjectRefList),
                typeof(SerializedMember),
                typeof(SerializedMemberList));

            TestMethodInputs_PropertyRefs(methodInfo,
                "obj1",
                "obj2");
        }
        [Fact]
        public void Parameters_StringArray()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.StringArray))!;

            TestMethodInputs_Defines(methodInfo,
                typeof(string[]));

            TestMethodInputs_PropertyRefs(methodInfo,
                "stringArray");
        }
    }
}
