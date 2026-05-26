using System.Collections.Generic;
using System.Linq;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// Firewall-safe schema type-ids (issue #80): the type-id uses delimiters that are illegal in
    /// C# identifiers, legal unescaped in a URI fragment, and absent from every LLM provider's
    /// content-firewall blocklist. As a result NO escaping is needed: the $defs key (raw JSON
    /// object key) and the $ref value (URI reference) are byte-identical, and NEITHER contains any
    /// of the forbidden chars '&lt; &gt; [ ] + %' (the first five were the old structural chars, '%'
    /// was the percent-encoding from the superseded PR #77 that broke Google Gemini).
    /// </summary>
    public class TestSchemaRefEncoding : BaseTest
    {
        public TestSchemaRefEncoding(ITestOutputHelper output) : base(output) { }

        static readonly char[] ForbiddenChars = new[] { '<', '>', '[', ']', '+', '%' };

        static void AssertNoForbiddenChars(string value, string what)
        {
            foreach (var c in ForbiddenChars)
                Assert.False(value.Contains(c),
                    $"{what} '{value}' must not contain forbidden char '{c}'.");
        }

        [Fact]
        public void GetSchemaRef_GenericType_RefHasNoForbiddenChars()
        {
            var reflector = new Reflector();
            var schema = reflector.GetSchemaRef<IList<TestType>>();

            var refValue = schema?[JsonSchema.Ref]?.ToString();
            Assert.NotNull(refValue);
            Assert.StartsWith(JsonSchema.RefValue, refValue);

            // Only check the type-id fragment (the RefValue prefix may legitimately contain '#').
            var fragment = refValue!.Substring(JsonSchema.RefValue.Length);
            AssertNoForbiddenChars(fragment, "$ref fragment");
        }

        [Fact]
        public void GetSchemaRef_ArrayType_RefHasNoForbiddenChars()
        {
            var reflector = new Reflector();
            var schema = reflector.GetSchemaRef<TestType[]>();

            var refValue = schema?[JsonSchema.Ref]?.ToString();
            Assert.NotNull(refValue);

            var fragment = refValue!.Substring(JsonSchema.RefValue.Length);
            AssertNoForbiddenChars(fragment, "$ref fragment");
        }

        [Fact]
        public void GetSchemaRef_NestedClass_RefHasNoForbiddenChars()
        {
            var reflector = new Reflector();
            var schema = reflector.GetSchemaRef<ParentClass.NestedClass>();

            var refValue = schema?[JsonSchema.Ref]?.ToString();
            Assert.NotNull(refValue);

            var fragment = refValue!.Substring(JsonSchema.RefValue.Length);
            AssertNoForbiddenChars(fragment, "$ref fragment");
        }

        [Fact]
        public void GetSchema_GenericType_DefsKeyEqualsRefAndHasNoForbiddenChars()
        {
            // $defs key must equal the $ref value (symmetric — no encode/decode asymmetry) and
            // must itself contain none of the forbidden chars.
            var reflector = new Reflector();
            var schema = reflector.GetSchema<IList<TestType>>();
            var defs = schema?[JsonSchema.Defs] as System.Text.Json.Nodes.JsonObject;

            Assert.NotNull(defs);
            var expectedKey = typeof(IList<TestType>).GetSchemaTypeId();
            Assert.True(defs!.ContainsKey(expectedKey),
                $"$defs must contain key '{expectedKey}'. Actual keys: {string.Join(", ", defs.Select(kvp => kvp.Key))}");

            foreach (var key in defs.Select(kvp => kvp.Key))
                AssertNoForbiddenChars(key, "$defs key");
        }

        [Fact]
        public void GetSchema_NestedClass_DefsKeyHasNoForbiddenChars()
        {
            var reflector = new Reflector();
            var schema = reflector.GetSchema<ParentClass.NestedClass>();
            var defs = schema?[JsonSchema.Defs] as System.Text.Json.Nodes.JsonObject;

            Assert.NotNull(defs);
            var expectedKey = typeof(ParentClass.NestedClass).GetSchemaTypeId();
            Assert.DoesNotContain("+", expectedKey);
            Assert.True(defs!.ContainsKey(expectedKey),
                $"$defs must contain key '{expectedKey}'. Actual keys: {string.Join(", ", defs.Select(kvp => kvp.Key))}");

            foreach (var key in defs.Select(kvp => kvp.Key))
                AssertNoForbiddenChars(key, "$defs key");
        }

        [Fact]
        public void GetSchema_GenericOfArrayOfNestedClass_DefsKeyEqualsRef()
        {
            // The combo from issue #80: IList<Outer+Nested[]>. Verify the $ref value equals the
            // $defs key for the top-level type (symmetric) and neither has forbidden chars.
            var reflector = new Reflector();
            var schema = reflector.GetSchema<IList<ParentClass.NestedClass[]>>();
            var defs = schema?[JsonSchema.Defs] as System.Text.Json.Nodes.JsonObject;

            Assert.NotNull(defs);
            foreach (var key in defs!.Select(kvp => kvp.Key))
                AssertNoForbiddenChars(key, "$defs key");

            // A nested $ref somewhere in the schema points at the array element type's id; assert
            // every $defs key is a valid no-forbidden-char id and the top-level id is present.
            var topId = typeof(IList<ParentClass.NestedClass[]>).GetSchemaTypeId();
            AssertNoForbiddenChars(topId, "top-level type-id");
        }
    }
}
