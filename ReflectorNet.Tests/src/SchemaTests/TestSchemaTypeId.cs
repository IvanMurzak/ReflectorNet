using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class TestSchemaTypeId : BaseTest
    {
        public TestSchemaTypeId(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GetSchemaTypeId_SimpleArray_ShouldAppendArray()
        {
            // Arrange
            var type = typeof(int[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"int[] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NestedArray_ShouldAppendMultipleArrays()
        {
            // Arrange
            var type = typeof(int[][]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"int[][] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_TripleNestedArray_ShouldAppendThreeArrays()
        {
            // Arrange
            var type = typeof(int[][][]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"int[][][] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_StringArray_ShouldWorkForAnyType()
        {
            // Arrange
            var type = typeof(string[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.String{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"string[] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ListOfInt_ShouldNormalizeToArray()
        {
            // Arrange
            var listType = typeof(List<int>);
            var arrayType = typeof(int[]);

            // Act
            var listResult = listType.GetSchemaTypeId();
            var arrayResult = arrayType.GetSchemaTypeId();

            // Assert
            Assert.Equal(arrayResult, listResult);
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}", listResult);
            _output.WriteLine($"List<int> -> {listResult}");
            _output.WriteLine($"int[] -> {arrayResult}");
            _output.WriteLine($"Both should be equal: {listResult == arrayResult}");
        }

        [Fact]
        public void GetSchemaTypeId_ListOfString_ShouldMatchStringArray()
        {
            // Arrange
            var listType = typeof(List<string>);
            var arrayType = typeof(string[]);

            // Act
            var listResult = listType.GetSchemaTypeId();
            var arrayResult = arrayType.GetSchemaTypeId();

            // Assert
            Assert.Equal(arrayResult, listResult);
            Assert.Equal($"System.String{TypeUtils.ArraySuffix}", listResult);
            _output.WriteLine($"List<string> -> {listResult}");
            _output.WriteLine($"string[] -> {arrayResult}");
        }

        [Fact]
        public void GetSchemaTypeId_ListOfIntArray_ShouldNormalizeToDoubleArray()
        {
            // Arrange
            var type = typeof(List<int[]>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"List<int[]> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ListOfListOfInt_ShouldNormalizeToDoubleArray()
        {
            // Arrange
            var type = typeof(List<List<int>>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"List<List<int>> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ListOfListOfInt_ShouldMatchIntDoubleArray()
        {
            // Arrange
            var listType = typeof(List<List<int>>);
            var arrayType = typeof(int[][]);

            // Act
            var listResult = listType.GetSchemaTypeId();
            var arrayResult = arrayType.GetSchemaTypeId();

            // Assert
            Assert.Equal(arrayResult, listResult);
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", listResult);
            _output.WriteLine($"List<List<int>> -> {listResult}");
            _output.WriteLine($"int[][] -> {arrayResult}");
        }

        [Fact]
        public void GetSchemaTypeId_ListOfDoubleNestedArray_ShouldAppendThreeArrays()
        {
            // Arrange
            var type = typeof(List<int[][]>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"List<int[][]> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ListOfStringArray_ShouldWorkForAnyType()
        {
            // Arrange
            var type = typeof(List<string[]>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.String{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"List<string[]> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ComplexNestedCollections_ShouldHandleDeepNesting()
        {
            // Arrange
            var type = typeof(List<List<string[]>[]>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.String{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"List<List<string[]>[]> -> {result}");
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
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}", result);
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
            Assert.Equal("System.Collections.Generic.IEnumerable<System.Int32>", result);
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
            Assert.Equal("System.Collections.Generic.ICollection<System.String>", result);
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
            Assert.Equal("System.Collections.Generic.IList<System.Int32>", result);
            _output.WriteLine($"IList<int> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_HashSetOfInt_ShouldNormalizeToArray()
        {
            // Arrange
            var type = typeof(HashSet<int>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"HashSet<int> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_SortedSetOfString_ShouldNormalizeToArray()
        {
            // Arrange
            var type = typeof(SortedSet<string>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.String{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"SortedSet<string> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_LinkedListOfInt_ShouldNormalizeToArray()
        {
            // Arrange
            var type = typeof(LinkedList<int>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"LinkedList<int> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_QueueOfString_ShouldNormalizeToArray()
        {
            // Arrange
            var type = typeof(Queue<string>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.String{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"Queue<string> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_StackOfInt_ShouldNormalizeToArray()
        {
            // Arrange
            var type = typeof(Stack<int>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"Stack<int> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NestedGenericCollections_AllShouldNormalize()
        {
            // Arrange
            var listOfHashSet = typeof(List<HashSet<int>>);
            var queueOfStack = typeof(Queue<Stack<string>>);
            var arrayOfLinkedList = typeof(LinkedList<double>[]);

            // Act
            var result1 = listOfHashSet.GetSchemaTypeId();
            var result2 = queueOfStack.GetSchemaTypeId();
            var result3 = arrayOfLinkedList.GetSchemaTypeId();

            // Assert
            Assert.Equal($"System.Int32{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result1);
            Assert.Equal($"System.String{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result2);
            Assert.Equal($"System.Double{TypeUtils.ArraySuffix}{TypeUtils.ArraySuffix}", result3);
            _output.WriteLine($"List<HashSet<int>> -> {result1}");
            _output.WriteLine($"Queue<Stack<string>> -> {result2}");
            _output.WriteLine($"LinkedList<double>[] -> {result3}");
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
        public void GetSchemaTypeId_ListOfCustomClass_ShouldNormalizeToArray()
        {
            // Arrange
            var type = typeof(List<TestType>);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"com.IvanMurzak.ReflectorNet.Tests.SchemaTests.TestType{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"List<TestType> -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ArrayOfCustomClass_ShouldAppendArray()
        {
            // Arrange
            var type = typeof(TestType[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal($"com.IvanMurzak.ReflectorNet.Tests.SchemaTests.TestType{TypeUtils.ArraySuffix}", result);
            _output.WriteLine($"TestType[] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ListAndArrayOfCustomClass_ShouldMatch()
        {
            // Arrange
            var listType = typeof(List<TestType>);
            var arrayType = typeof(TestType[]);

            // Act
            var listResult = listType.GetSchemaTypeId();
            var arrayResult = arrayType.GetSchemaTypeId();

            // Assert
            Assert.Equal(arrayResult, listResult);
            Assert.Equal($"com.IvanMurzak.ReflectorNet.Tests.SchemaTests.TestType{TypeUtils.ArraySuffix}", listResult);
            _output.WriteLine($"List<TestType> -> {listResult}");
            _output.WriteLine($"TestType[] -> {arrayResult}");
        }
    }
}
