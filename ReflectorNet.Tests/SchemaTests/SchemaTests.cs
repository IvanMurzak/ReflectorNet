using System;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class SchemaTests : SchemaTestBase
    {
        public SchemaTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Parameters_Object_Int_Bool()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;

            TestMethodInputs_Defines(
                reflector: null,
                methodInfo: methodInfo,
                expectedTypes: typeof(GameObjectRef));

            TestMethodInputs_PropertyRefs(
                reflector: null,
                methodInfo: methodInfo,
                parameterNames: "obj");
        }

        [Fact]
        public void Parameters_ListObject_ListObject()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.ListObject_ListObject))!;

            TestMethodInputs_Defines(
                reflector: null,
                methodInfo: methodInfo,
                typeof(GameObjectRef),
                typeof(GameObjectRefList),
                typeof(SerializedMember),
                typeof(SerializedMemberList));

            TestMethodInputs_PropertyRefs(
                reflector: null,
                methodInfo: methodInfo,
                "obj1",
                "obj2");
        }

        [Fact]
        public void Parameters_StringArray()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.StringArray))!;

            TestMethodInputs_Defines(
                reflector: null,
                methodInfo: methodInfo,
                typeof(string[]));

            TestMethodInputs_PropertyRefs(
                reflector: null,
                methodInfo: methodInfo,
                "stringArray");
        }

        [Fact]
        void GameObjectRef()
        {
            JsonSchemaValidation(typeof(GameObjectRef));
        }
    }
}
