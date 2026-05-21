using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// $defs keys are stored as raw type-ids (arbitrary JSON object keys).
    /// $ref values are URI references and must percent-encode chars not allowed in URI fragments.
    /// A consumer that URI-decodes the $ref fragment recovers the raw type-id and looks it up in $defs.
    /// </summary>
    public class TestSchemaRefEncoding : BaseTest
    {
        public TestSchemaRefEncoding(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GetSchemaRef_GenericType_EncodesAngleBrackets()
        {
            var reflector = new Reflector();
            var schema = reflector.GetSchemaRef<IList<TestType>>();

            var refValue = schema?[JsonSchema.Ref]?.ToString();
            Assert.NotNull(refValue);
            Assert.StartsWith(JsonSchema.RefValue, refValue);
            Assert.Contains("%3C", refValue);
            Assert.Contains("%3E", refValue);
            Assert.DoesNotContain("<", refValue);
            Assert.DoesNotContain(">", refValue);
        }

        [Fact]
        public void GetSchemaRef_ArrayType_EncodesBrackets()
        {
            var reflector = new Reflector();
            var schema = reflector.GetSchemaRef<TestType[]>();

            var refValue = schema?[JsonSchema.Ref]?.ToString();
            Assert.NotNull(refValue);
            Assert.Contains("%5B", refValue);
            Assert.Contains("%5D", refValue);
            Assert.DoesNotContain("[", refValue!.Substring(JsonSchema.RefValue.Length));
            Assert.DoesNotContain("]", refValue.Substring(JsonSchema.RefValue.Length));
        }

        [Fact]
        public void GetSchemaRef_NestedClass_EncodesPlus()
        {
            var reflector = new Reflector();
            var schema = reflector.GetSchemaRef<ParentClass.NestedClass>();

            var refValue = schema?[JsonSchema.Ref]?.ToString();
            Assert.NotNull(refValue);
            Assert.Contains("%2B", refValue);
            Assert.DoesNotContain("+", refValue);
        }

        [Fact]
        public void GetSchema_GenericType_DefsKeyIsRaw()
        {
            // $defs keys must remain raw — URI-decoding the $ref fragment must recover them verbatim.
            var reflector = new Reflector();
            var schema = reflector.GetSchema<IList<TestType>>();
            var defs = schema?[JsonSchema.Defs] as System.Text.Json.Nodes.JsonObject;

            Assert.NotNull(defs);
            Assert.True(defs!.ContainsKey(typeof(IList<TestType>).GetSchemaTypeId()),
                $"$defs must contain raw key '{typeof(IList<TestType>).GetSchemaTypeId()}'. Actual keys: {string.Join(", ", defs.Select(kvp => kvp.Key))}");
        }

        [Fact]
        public void GetSchema_NestedClass_DefsKeyIsRawWithPlus()
        {
            var reflector = new Reflector();
            var schema = reflector.GetSchema<ParentClass.NestedClass>();
            var defs = schema?[JsonSchema.Defs] as System.Text.Json.Nodes.JsonObject;

            Assert.NotNull(defs);
            var expectedKey = typeof(ParentClass.NestedClass).GetSchemaTypeId();
            Assert.Contains("+", expectedKey);
            Assert.True(defs!.ContainsKey(expectedKey),
                $"$defs must contain raw key '{expectedKey}'. Actual keys: {string.Join(", ", defs.Select(kvp => kvp.Key))}");
        }
    }
}
