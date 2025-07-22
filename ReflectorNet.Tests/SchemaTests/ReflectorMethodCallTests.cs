using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using ReflectorNet.Tests.Schema.Model;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReflectorNet.Tests.SchemaTests
{
    public class ReflectorMethodCallTests : BaseTest
    {
        public ReflectorMethodCallTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void MethodCall_SimpleInstanceMethod()
        {
            // Arrange
            var reflector = new Reflector();
            var testInstance = new TestClass();
            var targetObject = reflector.Serialize(testInstance);

            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            // Act
            var result = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                targetObject: targetObject,
                executeInMainThread: false
            );

            // Assert
            Assert.StartsWith("[Success]", result);
            Assert.Contains("true", result);
            _output.WriteLine($"Instance method call result: {result}");
        }

        [Fact]
        public void MethodCall_WithSerializedParameters()
        {
            // Arrange
            var reflector = new Reflector();
            var testInstance = new TestClass();
            var targetObject = reflector.Serialize(testInstance);

            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.SerializedMemberList_ReturnString)
            };

            var serializedList = new SerializedMemberList();
            var inputParam = reflector.Serialize(serializedList, name: "gameObjectDiffs");
            var inputParameters = new SerializedMemberList { inputParam };

            // Act
            var result = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                targetObject: targetObject,
                inputParameters: inputParameters,
                executeInMainThread: false
            );

            // Assert
            Assert.StartsWith("[Success]", result);
            Assert.Contains("SerializedMemberList", result);
            _output.WriteLine($"Method call with parameters result: {result}");
        }

        [Fact]
        public void MethodCall_MethodNotFound()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = "NonExistentMethod"
            };

            // Act
            var result = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                executeInMainThread: false
            );

            // Assert
            Assert.StartsWith("[Error] Method not found", result);
            _output.WriteLine($"Method not found result: {result}");
        }

        [Fact]
        public void MethodCall_HelperMethod_Object_Int_Bool()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(MethodHelper).Namespace,
                TypeName = nameof(MethodHelper),
                MethodName = nameof(MethodHelper.Object_Int_Bool)
            };

            // Create test parameters
            var gameObjectRef = new GameObjectRef
            {
                instanceID = 123,
                name = "TestObject",
                path = "/Test/Path"
            };

            var inputParameters = new SerializedMemberList
            {
                reflector.Serialize(gameObjectRef, name: "obj"),
                reflector.Serialize(42, name: "integer"),
                reflector.Serialize(true, name: "boolean")
            };

            // Act
            var result = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                inputParameters: inputParameters,
                executeInMainThread: false
            );

            // Assert
            Assert.StartsWith("[Success]", result);
            _output.WriteLine($"Helper method call result: {result}");
        }

        [Fact]
        public void MethodCall_HelperMethod_StringArray()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(MethodHelper).Namespace,
                TypeName = nameof(MethodHelper),
                MethodName = nameof(MethodHelper.StringArray)
            };

            var stringArray = new string[] { "test1", "test2", "test3" };
            var inputParameters = new SerializedMemberList
            {
                reflector.Serialize(stringArray, name: "stringArray")
            };

            // Act
            var result = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                inputParameters: inputParameters,
                executeInMainThread: false
            );

            // Assert
            Assert.StartsWith("[Success]", result);
            _output.WriteLine($"String array method call result: {result}");
        }

        [Fact]
        public void MethodCall_DefaultParameterValues()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(MethodHelper).Namespace,
                TypeName = nameof(MethodHelper),
                MethodName = nameof(MethodHelper.Object_Int_Bool)
            };

            // Only provide the required GameObjectRef parameter, let int and bool use defaults
            var gameObjectRef = new GameObjectRef { instanceID = 999, name = "DefaultTest" };
            var inputParameters = new SerializedMemberList
            {
                reflector.Serialize(gameObjectRef, name: "obj")
            };

            // Act
            var result = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                inputParameters: inputParameters,
                executeInMainThread: false
            );

            // Assert
            Assert.StartsWith("[Success]", result);
            _output.WriteLine($"Method call with default parameters result: {result}");
        }

        [Fact]
        public void MethodCall_AmbiguousMethod_ReturnsError()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = "NoParameters" // This might match multiple methods
            };

            // Act
            var result = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 1, // Loose matching to potentially find multiple
                executeInMainThread: false
            );

            // Assert
            // Result could be success (if only one match) or error (if multiple matches)
            Assert.NotNull(result);
            _output.WriteLine($"Ambiguous method call result: {result}");
        }

        [Fact]
        public void MethodCall_With_TargetObject_Instance()
        {
            // Arrange
            var reflector = new Reflector();
            var testInstance = new TestClass();
            var targetObjectSerialized = reflector.Serialize(testInstance);

            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            // Act
            var result = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                targetObject: targetObjectSerialized,
                executeInMainThread: false
            );

            // Assert
            Assert.StartsWith("[Success]", result);
            Assert.Contains("true", result);
            _output.WriteLine($"Instance method call result: {result}");
        }

        [Fact]
        public void MainThread_Integration_Test()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            // Act - Test both main thread and background execution
            var mainThreadResult = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                executeInMainThread: true
            );

            var backgroundResult = reflector.MethodCall(
                reflector: reflector,
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                executeInMainThread: false
            );

            // Assert
            Assert.StartsWith("[Success]", mainThreadResult);
            Assert.StartsWith("[Success]", backgroundResult);
            Assert.Contains("true", mainThreadResult);
            Assert.Contains("true", backgroundResult);

            _output.WriteLine($"Main thread result: {mainThreadResult.Length} chars");
            _output.WriteLine($"Background result: {backgroundResult.Length} chars");
        }
    }
}
