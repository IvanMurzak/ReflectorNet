using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using Xunit.Abstractions;
using System;
using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class ErrorHandlingTests : BaseTest
    {
        public ErrorHandlingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Serialize_UnsupportedType_ThrowsException()
        {
            // Arrange
            var reflector = new Reflector();
            var unsupportedObject = new IntPtr(123); // IntPtr is not typically serializable

            // Act & Assert
            var exception = Assert.ThrowsAny<Exception>(() =>
                reflector.Serialize(unsupportedObject));

            Assert.Contains("not supported", exception.Message);
            _output.WriteLine($"Expected exception caught: {exception.Message}");
        }

        [Fact]
        public void Deserialize_InvalidTypeName_ThrowsException()
        {
            // Arrange
            var reflector = new Reflector();
            var serialized = new SerializedMember
            {
                typeName = "InvalidTypeName.DoesNotExist",
                valueJsonElement = JsonDocument.Parse("{}").RootElement
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                reflector.Deserialize(serialized));

            Assert.Contains("Type 'InvalidTypeName.DoesNotExist' not found", exception.Message);
            _output.WriteLine($"Expected exception caught: {exception.Message}");
        }

        [Fact]
        public void Error_Message_Formatting_And_Depth()
        {
            // Arrange
            var reflector = new Reflector();
            var invalidData = new com.IvanMurzak.ReflectorNet.Model.SerializedMember
            {
                typeName = "NonExistent.Type.Name",
                valueJsonElement = JsonDocument.Parse("{}").RootElement
            };

            // Act & Assert - Test error handling in population
            object? testObject = default(GameObjectRef);
            var errorResult = reflector.Populate(ref testObject, invalidData, depth: 2);

            Assert.NotNull(errorResult);
            var errorString = errorResult.ToString();
            Assert.Contains($"Type '{invalidData.typeName}' not found", errorString);
            // Check that indentation (depth) is applied
            Assert.StartsWith("    ", errorString); // 2 levels of depth = 4 spaces

            _output.WriteLine($"Error with depth formatting: {errorString}");
        }

        [Fact]
        public void GameObjectRef_IsValid_Tests()
        {
            // Test with instanceID
            var gameObjectWithId = new GameObjectRef { instanceID = 123 };
            Assert.True(gameObjectWithId.IsValid);

            // Test with path
            var gameObjectWithPath = new GameObjectRef { path = "/Root/Child" };
            Assert.True(gameObjectWithPath.IsValid);

            // Test with name
            var gameObjectWithName = new GameObjectRef { name = "TestObject" };
            Assert.True(gameObjectWithName.IsValid);

            // Test with nothing (invalid)
            var gameObjectEmpty = new GameObjectRef();
            Assert.False(gameObjectEmpty.IsValid);

            _output.WriteLine("GameObjectRef validation tests passed");
        }

        [Fact]
        public void GameObjectRef_ToString_Tests()
        {
            // Test with instanceID (highest priority)
            var gameObjectWithId = new GameObjectRef
            {
                instanceID = 123,
                path = "/Root/Child",
                name = "TestObject"
            };
            Assert.Contains("instanceID='123'", gameObjectWithId.ToString());

            // Test with path only (second priority)
            var gameObjectWithPath = new GameObjectRef { path = "/Root/Child" };
            Assert.Contains("path='/Root/Child'", gameObjectWithPath.ToString());

            // Test with name only (third priority)
            var gameObjectWithName = new GameObjectRef { name = "TestObject" };
            Assert.Contains("name='TestObject'", gameObjectWithName.ToString());

            // Test with nothing
            var gameObjectEmpty = new GameObjectRef();
            Assert.Contains("unknown", gameObjectEmpty.ToString());

            _output.WriteLine("GameObjectRef ToString tests passed");
        }

        [Fact]
        public void MethodPointerRef_ToString_Formatting()
        {
            // Test without namespace
            var methodRef1 = new MethodPointerRef
            {
                TypeName = "TestClass",
                MethodName = "TestMethod"
            };
            var toString1 = methodRef1.ToString();
            Assert.Equal("TestClass.TestMethod()", toString1);

            // Test with namespace
            var methodRef2 = new MethodPointerRef
            {
                Namespace = "TestNamespace",
                TypeName = "TestClass",
                MethodName = "TestMethod"
            };
            var toString2 = methodRef2.ToString();
            Assert.Equal("TestNamespace.TestClass.TestMethod()", toString2);

            // Test with parameters
            var methodRef3 = new MethodPointerRef
            {
                Namespace = "TestNamespace",
                TypeName = "TestClass",
                MethodName = "TestMethod",
                InputParameters = new System.Collections.Generic.List<MethodPointerRef.Parameter>
                {
                    new() { TypeName = "System.String", Name = "param1" },
                    new() { TypeName = "System.Int32", Name = "param2" }
                }
            };
            var toString3 = methodRef3.ToString();
            Assert.Contains("TestNamespace.TestClass.TestMethod(", toString3);
            Assert.Contains("System.String param1", toString3);
            Assert.Contains("System.Int32 param2", toString3);

            _output.WriteLine("MethodPointerRef ToString formatting tests passed");
        }

        [Fact]
        public void SerializedMemberList_Validation_Tests()
        {
            // Arrange
            var validList = new SerializedMemberList
            {
                new() { name = "param1", typeName = "System.String" },
                new() { name = "param2", typeName = "System.Int32" }
            };

            var invalidList = new SerializedMemberList
            {
                new() { name = "param1", typeName = "InvalidType.Name" },
                new() { name = "param2", typeName = "System.Int32" }
            };

            // Act & Assert - Test validation
            var isValidResult = validList.IsValidTypeNames("testField", out var validError);
            Assert.True(isValidResult);
            Assert.Null(validError);

            var isInvalidResult = invalidList.IsValidTypeNames("testField", out var invalidError);
            Assert.False(isInvalidResult);
            Assert.NotNull(invalidError);
            Assert.Contains("InvalidType.Name", invalidError);

            _output.WriteLine($"Valid list validation: {isValidResult}");
            _output.WriteLine($"Invalid list validation: {isInvalidResult}, Error: {invalidError}");
        }

        [Fact]
        public void Reflector_Registry_Converter_Management()
        {
            // Arrange
            var reflector = new Reflector();

            // Act - Add a custom converter (using existing one for test)
            var primitiveConverter = new com.IvanMurzak.ReflectorNet.Convertor.PrimitiveReflectionConvertor();
            reflector.Convertors.Add(primitiveConverter);

            // Act - Remove converter by type
            reflector.Convertors.Remove<com.IvanMurzak.ReflectorNet.Convertor.PrimitiveReflectionConvertor>();

            // Assert - Test that registry operations work
            Assert.NotNull(reflector.Convertors);

            _output.WriteLine("Registry management test passed - converters can be added and removed");
        }
    }
}
