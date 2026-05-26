using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    /// <summary>
    /// GetTypeId emits firewall-safe structural delimiters (issue #80):
    /// generic '&lt;&gt;' -> '()', nested-class '+' -> '-', array '[]' rank-1 -> '-1',
    /// rank-N -> '-N', jagged repeats stack (int[][] -> '-1-1'). Namespace '.' and
    /// generic-arg separator ',' stay literal.
    /// </summary>
    public class TestTypeId : BaseTest
    {
        public TestTypeId(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GetTypeId_SimpleArray_ShouldAppendArray()
        {
            // Arrange
            var type = typeof(int[]);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Int32-1", result);
            _output.WriteLine($"int[] -> {result}");
        }

        [Fact]
        public void GetTypeId_NestedArray_ShouldAppendMultipleArrays()
        {
            // Arrange
            var type = typeof(int[][]);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Int32-1-1", result);
            _output.WriteLine($"int[][] -> {result}");
        }

        [Fact]
        public void GetTypeId_TripleNestedArray_ShouldAppendThreeArrays()
        {
            // Arrange
            var type = typeof(int[][][]);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Int32-1-1-1", result);
            _output.WriteLine($"int[][][] -> {result}");
        }

        [Fact]
        public void GetTypeId_MultiDimensionalArray_ShouldUseRankDigit()
        {
            // Arrange
            var type = typeof(int[,]);

            // Act
            var result = type.GetTypeId();

            // Assert
            // rank-2 multi-dimensional array -> '-2' (distinct from jagged int[][] -> '-1-1').
            Assert.Equal("System.Int32-2", result);
            _output.WriteLine($"int[,] -> {result}");
        }

        [Fact]
        public void GetTypeId_StringArray_ShouldWorkForAnyType()
        {
            // Arrange
            var type = typeof(string[]);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.String-1", result);
            _output.WriteLine($"string[] -> {result}");
        }

        [Fact]
        public void GetTypeId_ListOfInt_ShouldUseParens()
        {
            // Arrange
            var type = typeof(List<int>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.List(System.Int32)", result);
            _output.WriteLine($"List<int> -> {result}");
        }

        [Fact]
        public void GetTypeId_ListOfIntArray_ShouldAppendArrayRank()
        {
            // Arrange
            var type = typeof(List<int[]>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.List(System.Int32-1)", result);
            _output.WriteLine($"List<int[]> -> {result}");
        }

        [Fact]
        public void GetTypeId_ListOfListOfInt_ShouldNestParens()
        {
            // Arrange
            var type = typeof(List<List<int>>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.List(System.Collections.Generic.List(System.Int32))", result);
            _output.WriteLine($"List<List<int>> -> {result}");
        }

        [Fact]
        public void GetTypeId_ListOfDoubleNestedArray_ShouldStackArrayRanks()
        {
            // Arrange
            var type = typeof(List<int[][]>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.List(System.Int32-1-1)", result);
            _output.WriteLine($"List<int[][]> -> {result}");
        }

        [Fact]
        public void GetTypeId_ListOfStringArray_ShouldWorkForAnyType()
        {
            // Arrange
            var type = typeof(List<string[]>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.List(System.String-1)", result);
            _output.WriteLine($"List<string[]> -> {result}");
        }

        [Fact]
        public void GetTypeId_ComplexNestedCollections_ShouldHandleDeepNesting()
        {
            // Arrange
            var type = typeof(List<List<string[]>[]>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.List(System.Collections.Generic.List(System.String-1)-1)", result);
            _output.WriteLine($"List<List<string[]>[]> -> {result}");
        }

        [Fact]
        public void GetTypeId_NonCollectionType_ShouldReturnFullName()
        {
            // Arrange
            var type = typeof(string);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.String", result);
            _output.WriteLine($"string -> {result}");
        }

        [Fact]
        public void GetTypeId_NullableType_ShouldHandleUnderlyingType()
        {
            // Arrange
            var type = typeof(int?);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Int32", result);
            _output.WriteLine($"int? -> {result}");
        }

        [Fact]
        public void GetTypeId_NullableArrayType_ShouldHandleUnderlyingArrayType()
        {
            // Arrange
            var type = typeof(int?[]);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Int32-1", result);
            _output.WriteLine($"int?[] -> {result}");
        }

        [Fact]
        public void GetTypeId_GenericTypeNotCollection_ShouldReturnGenericFormat()
        {
            // Arrange
            var type = typeof(Dictionary<string, int>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Contains("System.Collections.Generic.Dictionary", result);
            Assert.Contains("System.String", result);
            Assert.Contains("System.Int32", result);
            // Firewall-safe: no angle brackets in the emitted id.
            Assert.DoesNotContain("<", result);
            Assert.DoesNotContain(">", result);
            _output.WriteLine($"Dictionary<string, int> -> {result}");
        }
    }
}
