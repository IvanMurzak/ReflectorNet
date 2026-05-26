using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// TypeUtils.GetType(...) is the funnel for every type-name → Type resolution invoked by
    /// reflector modify / field-set / property-set actions. Callers may submit a C#-canonical raw
    /// type id (e.g. <c>System.Int32[]</c>, <c>IList&lt;Int32&gt;</c>, <c>Outer+Nested</c>) or the
    /// firewall-safe schema form (issue #80) that a JSON Schema <c>$ref</c> consumer round-trips
    /// (e.g. <c>System.Int32-1</c>, <c>IList(Int32)</c>, <c>Outer-Nested</c>). Both must resolve to
    /// the same Type. The safe-form conversion is:
    /// <c>( → &lt;</c>, <c>) → &gt;</c>, <c>-&lt;digits&gt; → array rank</c>, <c>-&lt;ident&gt; → +</c> (nested class).
    /// </summary>
    public class TestGetTypeDecodeSchemaRef : BaseTest
    {
        public TestGetTypeDecodeSchemaRef(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("System.Int32[]", "System.Int32-1")]
        [InlineData("System.String[]", "System.String-1")]
        [InlineData("System.Int32[][]", "System.Int32-1-1")]
        public void GetType_Array_AcceptsBothRawAndSafeForm(string raw, string safe)
        {
            var rawType = TypeUtils.GetType(raw);
            var safeType = TypeUtils.GetType(safe);

            Assert.NotNull(rawType);
            Assert.Same(rawType, safeType);
        }

        [Fact]
        public void GetType_MultiDimensionalArray_SafeRankResolves()
        {
            // rank-2 safe form '-2' must resolve to int[,] (distinct from jagged int[][] / '-1-1').
            var safe = TypeUtils.GetType("System.Int32-2");
            Assert.NotNull(safe);
            Assert.Equal(typeof(int[,]), safe);

            var jagged = TypeUtils.GetType("System.Int32-1-1");
            Assert.NotNull(jagged);
            Assert.Equal(typeof(int[][]), jagged);

            Assert.NotEqual(safe, jagged);
        }

        [Theory]
        [InlineData(
            "System.Collections.Generic.IList<System.Int32>",
            "System.Collections.Generic.IList(System.Int32)")]
        [InlineData(
            "System.Collections.Generic.IEnumerable<System.String>",
            "System.Collections.Generic.IEnumerable(System.String)")]
        public void GetType_Generic_AcceptsBothRawAndSafeForm(string raw, string safe)
        {
            var rawType = TypeUtils.GetType(raw);
            var safeType = TypeUtils.GetType(safe);

            Assert.NotNull(rawType);
            Assert.Same(rawType, safeType);
        }

        [Fact]
        public void GetType_NestedClass_AcceptsBothRawAndSafeForm()
        {
            var raw = "com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass+NestedClass";
            var safe = "com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass-NestedClass";

            var rawType = TypeUtils.GetType(raw);
            var safeType = TypeUtils.GetType(safe);

            Assert.NotNull(rawType);
            Assert.Equal(typeof(ParentClass.NestedClass), rawType);
            Assert.Same(rawType, safeType);
        }

        [Fact]
        public void GetType_GenericOfArrayOfNestedClass_AcceptsSafeForm()
        {
            // The combo from issue #80: IList<Outer+Nested[]>. Both the raw id and the safe-form
            // (what GetSchemaTypeId now emits, and what a $ref consumer round-trips) resolve to it.
            var raw = "System.Collections.Generic.IList<com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass+NestedClass[]>";
            var safe = "System.Collections.Generic.IList(com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass-NestedClass-1)";

            var rawType = TypeUtils.GetType(raw);
            var safeType = TypeUtils.GetType(safe);
            var directType = typeof(IList<ParentClass.NestedClass[]>);

            Assert.NotNull(rawType);
            Assert.Equal(directType, rawType);
            Assert.NotNull(safeType);
            Assert.Equal(directType, safeType);
        }

        [Fact]
        public void GetType_SafeFormRoundTripsFromGetSchemaTypeId()
        {
            // End-to-end: GetSchemaTypeId emits the safe form; GetType resolves it back to the
            // original Type. This is the round-trip the JSON Schema $defs/$ref pipeline relies on.
            var types = new[]
            {
                typeof(int[]),
                typeof(int[][]),
                typeof(int[,]),
                typeof(IList<int>),
                typeof(IEnumerable<string>),
                typeof(ParentClass.NestedClass),
                typeof(ParentClass.NestedClass[]),
                typeof(IList<ParentClass.NestedClass[]>),
            };

            foreach (var type in types)
            {
                var id = type.GetSchemaTypeId();
                var resolved = TypeUtils.GetType(id);
                Assert.Equal(type, resolved);
                _output.WriteLine($"{type} -> {id} -> {resolved}");
            }
        }
    }
}
