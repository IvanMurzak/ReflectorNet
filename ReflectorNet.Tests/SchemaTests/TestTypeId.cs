using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
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
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}", result);
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
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result);
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
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"int[][][] -> {result}");
        }

        [Fact]
        public void GetTypeId_StringArray_ShouldWorkForAnyType()
        {
            // Arrange
            var type = typeof(string[]);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal($"System.String{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"string[] -> {result}");
        }

        [Fact]
        public void GetTypeId_ListOfInt_ShouldAppendArray()
        {
            // Arrange
            var type = typeof(List<int>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal($"System.Collections.Generic.List<System.Int32>", result);
            _output.WriteLine($"List<int> -> {result}");
        }

        [Fact]
        public void GetTypeId_ListOfIntArray_ShouldAppendTwoArrays()
        {
            // Arrange
            var type = typeof(List<int[]>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal($"System.Collections.Generic.List<System.Int32{TypeUtils.ArraySuffix}>", result);
            _output.WriteLine($"List<int[]> -> {result}");
        }

        [Fact]
        public void GetTypeId_ListOfListOfInt_ShouldAppendTwoArrays()
        {
            // Arrange
            var type = typeof(List<List<int>>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal("System.Collections.Generic.List<System.Collections.Generic.List<System.Int32>>", result);
            _output.WriteLine($"List<List<int>> -> {result}");
        }

        [Fact]
        public void GetTypeId_ListOfDoubleNestedArray_ShouldAppendThreeArrays()
        {
            // Arrange
            var type = typeof(List<int[][]>);

            // Act
            var result = type.GetTypeId();

            // Assert
            Assert.Equal($"System.Collections.Generic.List<System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}>", result);
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
            Assert.Equal($"System.Collections.Generic.List<System.String{TypeUtils.ArraySuffix}>", result);
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
            Assert.Equal($"System.Collections.Generic.List<System.Collections.Generic.List<System.String{TypeUtils.ArraySuffix}>{TypeUtils.ArraySuffix}>", result);
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
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}", result);
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
            _output.WriteLine($"Dictionary<string, int> -> {result}");
        }
    }
}
