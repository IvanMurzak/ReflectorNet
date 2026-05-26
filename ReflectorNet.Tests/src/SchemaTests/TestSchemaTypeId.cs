using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// GetSchemaTypeId emits firewall-safe structural delimiters (issue #80):
    /// generic '&lt;&gt;' -> '()', nested-class '+' -> '-', array '[]' rank-1 -> '-1',
    /// rank-N -> '-N', jagged repeats stack (int[][] -> '-1-1'). Namespace '.' and
    /// generic-arg separator ',' stay literal. The $defs key equals the $ref value (symmetric).
    /// </summary>
    public class TestSchemaTypeId : BaseTest
    {
        public TestSchemaTypeId(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GetSchemaTypeId_SimpleArray_ShouldAppendArrayRank()
        {
            // Arrange
            var type = typeof(int[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.Int32-1", result);
            _output.WriteLine($"int[] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NestedArray_ShouldStackArrayRanks()
        {
            // Arrange
            var type = typeof(int[][]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.Int32-1-1", result);
            _output.WriteLine($"int[][] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_TripleNestedArray_ShouldStackThreeArrayRanks()
        {
            // Arrange
            var type = typeof(int[][][]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.Int32-1-1-1", result);
            _output.WriteLine($"int[][][] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_MultiDimensionalArray_ShouldUseRankDigit()
        {
            // Arrange
            var type = typeof(int[,]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            // rank-2 multi-dimensional array -> '-2' (distinct from jagged int[][] -> '-1-1').
            Assert.Equal("System.Int32-2", result);
            _output.WriteLine($"int[,] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_StringArray_ShouldWorkForAnyType()
        {
            // Arrange
            var type = typeof(string[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.String-1", result);
            _output.WriteLine($"string[] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NonCollectionType_ShouldReturnFullName()
        {
            // Arrange
            var type = typeof(string);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.String", result);
            _output.WriteLine($"string -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NullableType_ShouldHandleUnderlyingType()
        {
            // Arrange
            var type = typeof(int?);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.Int32", result);
            _output.WriteLine($"int? -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NullableArrayType_ShouldHandleUnderlyingArrayType()
        {
            // Arrange
            var type = typeof(int?[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.Int32-1", result);
            _output.WriteLine($"int?[] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_IEnumerableOfInt_ShouldReturnGenericFormat()
        {
            // Arrange
            var type = typeof(IEnumerable<int>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.IEnumerable(System.Int32)", result);
            _output.WriteLine($"IEnumerable<int> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ICollectionOfString_ShouldReturnGenericFormat()
        {
            // Arrange
            var type = typeof(ICollection<string>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.ICollection(System.String)", result);
            _output.WriteLine($"ICollection<string> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_IListOfInt_ShouldReturnGenericFormat()
        {
            // Arrange
            var type = typeof(IList<int>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.IList(System.Int32)", result);
            _output.WriteLine($"IList<int> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_CustomClass_ShouldReturnFullName()
        {
            // Arrange
            var type = typeof(TestType);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("com.IvanMurzak.ReflectorNet.Tests.SchemaTests.TestType", result);
            _output.WriteLine($"TestType -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ArrayOfCustomClass_ShouldAppendArrayRank()
        {
            // Arrange
            var type = typeof(TestType[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("com.IvanMurzak.ReflectorNet.Tests.SchemaTests.TestType-1", result);
            _output.WriteLine($"TestType[] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NestedClass_ShouldUseHyphenSeparator()
        {
            // Arrange
            var type = typeof(ParentClass.NestedClass);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            // Firewall-safe nested-class delimiter (issue #80): '+' -> '-'. The $defs key and the
            // $ref value are byte-identical (no encode/decode asymmetry).
            Assert.Equal("com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass-NestedClass", result);
            _output.WriteLine($"ParentClass.NestedClass -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NestedStaticClass_ShouldUseHyphenSeparator()
        {
            // Arrange
            var type = typeof(ParentClass.NestedStaticClass);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass-NestedStaticClass", result);
            _output.WriteLine($"ParentClass.NestedStaticClass -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NestedClassInStaticParent_ShouldUseHyphenSeparator()
        {
            // Arrange
            var type = typeof(StaticParentClass.NestedClass);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("com.IvanMurzak.ReflectorNet.OuterAssembly.Model.StaticParentClass-NestedClass", result);
            _output.WriteLine($"StaticParentClass.NestedClass -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ArrayOfNestedClass_ShouldUseHyphenAndArrayRank()
        {
            // Arrange
            var type = typeof(ParentClass.NestedClass[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass-NestedClass-1", result);
            _output.WriteLine($"ParentClass.NestedClass[] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_GenericOfArrayOfNestedClass_ShouldComposeAllDelimiters()
        {
            // Arrange — the combo from issue #80: IList<Outer+Nested[]>.
            var type = typeof(IList<ParentClass.NestedClass[]>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal(
                "System.Collections.Generic.IList(com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass-NestedClass-1)",
                result);
            _output.WriteLine($"IList<ParentClass.NestedClass[]> -> {result}");
        }
    }
}
