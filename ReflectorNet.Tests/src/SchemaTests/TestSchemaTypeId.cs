using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.OuterAssembly.Model;
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
            Assert.Equal("System.Int32[]", result);
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
            Assert.Equal("System.Int32[][]", result);
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
            Assert.Equal("System.Int32[][][]", result);
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
            Assert.Equal("System.String[]", result);
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
            Assert.Equal("System.Int32[]", result);
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
        public void GetSchemaTypeId_ArrayOfCustomClass_ShouldAppendArray()
        {
            // Arrange
            var type = typeof(TestType[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("com.IvanMurzak.ReflectorNet.Tests.SchemaTests.TestType[]", result);
            _output.WriteLine($"TestType[] -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NestedClass_ShouldUsePlusSeparator()
        {
            // Arrange
            var type = typeof(ParentClass.NestedClass);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            // $defs keys are raw JSON object keys — the '+' nested-class separator is stored
            // verbatim. URI encoding happens at the $ref site only (see TestSchemaRefEncoding).
            Assert.Equal("com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass+NestedClass", result);
            _output.WriteLine($"ParentClass.NestedClass -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NestedStaticClass_ShouldUsePlusSeparator()
        {
            // Arrange
            var type = typeof(ParentClass.NestedStaticClass);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass+NestedStaticClass", result);
            _output.WriteLine($"ParentClass.NestedStaticClass -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_NestedClassInStaticParent_ShouldUsePlusSeparator()
        {
            // Arrange
            var type = typeof(StaticParentClass.NestedClass);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("com.IvanMurzak.ReflectorNet.OuterAssembly.Model.StaticParentClass+NestedClass", result);
            _output.WriteLine($"StaticParentClass.NestedClass -> {result}");
        }

        [Fact]
        public void GetSchemaTypeId_ArrayOfNestedClass_ShouldUsePlusAndBrackets()
        {
            // Arrange
            var type = typeof(ParentClass.NestedClass[]);

            // Act
            var result = type.GetSchemaTypeId();

            // Assert
            Assert.Equal("com.IvanMurzak.ReflectorNet.OuterAssembly.Model.ParentClass+NestedClass[]", result);
            _output.WriteLine($"ParentClass.NestedClass[] -> {result}");
        }
    }
}
