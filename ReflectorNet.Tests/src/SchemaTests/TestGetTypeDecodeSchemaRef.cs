using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// TypeUtils.GetType(...) is the funnel for every type-name → Type resolution invoked by
    /// reflector modify / field-set / property-set actions. Callers may submit a raw type id
    /// (e.g. <c>System.Int32[]</c>) or a $ref-style percent-encoded form (e.g. <c>System.Int32%5B%5D</c>).
    /// Both must resolve to the same Type. Only these five chars are decoded:
    /// <c>%2B → +</c>, <c>%3C → &lt;</c>, <c>%3E → &gt;</c>, <c>%5B → [</c>, <c>%5D → ]</c>.
    /// </summary>
    public class TestGetTypeDecodeSchemaRef : BaseTest
    {
        public TestGetTypeDecodeSchemaRef(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("System.Int32[]", "System.Int32%5B%5D")]
        [InlineData("System.String[]", "System.String%5B%5D")]
        [InlineData("System.Int32[][]", "System.Int32%5B%5D%5B%5D")]
        public void GetType_Array_AcceptsBothRawAndEncodedBrackets(string raw, string encoded)
        {
            var rawType = TypeUtils.GetType(raw);
            var encodedType = TypeUtils.GetType(encoded);

            Assert.NotNull(rawType);
            Assert.Same(rawType, encodedType);
        }

        [Theory]
        [InlineData(
            "System.Collections.Generic.IList<System.Int32>",
            "System.Collections.Generic.IList%3CSystem.Int32%3E")]
        [InlineData(
            "System.Collections.Generic.IEnumerable<System.String>",
            "System.Collections.Generic.IEnumerable%3CSystem.String%3E")]
        public void GetType_Generic_AcceptsBothRawAndEncodedAngleBrackets(string raw, string encoded)
        {
            var rawType = TypeUtils.GetType(raw);
            var encodedType = TypeUtils.GetType(encoded);

            Assert.NotNull(rawType);
            Assert.Same(rawType, encodedType);
        }

        [Fact]
        public void GetType_NestedClass_AcceptsBothRawAndEncodedPlus()
        {
            var raw = "com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass+NestedClass";
            var encoded = "com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass%2BNestedClass";

            var rawType = TypeUtils.GetType(raw);
            var encodedType = TypeUtils.GetType(encoded);

            Assert.NotNull(rawType);
            Assert.Equal(typeof(ParentClass.NestedClass), rawType);
            Assert.Same(rawType, encodedType);
        }

        [Fact]
        public void GetType_GenericOfArrayOfNestedClass_AcceptsFullyEncodedForm()
        {
            // The $ref-style encoded form a JSON Schema consumer would send through.
            var encoded = "System.Collections.Generic.IList%3Ccom.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass%2BNestedClass%5B%5D%3E";

            var encodedType = TypeUtils.GetType(encoded);
            var directType = typeof(IList<ParentClass.NestedClass[]>);

            Assert.NotNull(encodedType);
            Assert.Equal(directType, encodedType);
        }
    }
}
