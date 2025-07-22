using com.IvanMurzak.ReflectorNet;
using ReflectorNet.Tests.Schema.Model;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReflectorNet.Tests.SchemaTests
{
    public class PerformanceTests : BaseTest
    {
        public PerformanceTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Performance_Stress_Test_Serialization()
        {
            // Arrange
            var reflector = new Reflector();
            var testObjects = new List<GameObjectRef>();

            for (int i = 0; i < 100; i++)
            {
                testObjects.Add(new GameObjectRef
                {
                    instanceID = i,
                    name = $"Object_{i}",
                    path = $"/Root/Object_{i}"
                });
            }

            // Act - Measure performance of bulk serialization
            var startTime = DateTime.UtcNow;
            var serializedObjects = testObjects.Select(obj => reflector.Serialize(obj)).ToList();
            var serializationTime = DateTime.UtcNow - startTime;

            startTime = DateTime.UtcNow;
            var deserializedObjects = serializedObjects.Select(s => reflector.Deserialize(s) as GameObjectRef).ToList();
            var deserializationTime = DateTime.UtcNow - startTime;

            // Assert
            Assert.Equal(testObjects.Count, serializedObjects.Count);
            Assert.Equal(testObjects.Count, deserializedObjects.Count);
            Assert.All(deserializedObjects, Assert.NotNull);

            _output.WriteLine($"Serialized {testObjects.Count} objects in {serializationTime.TotalMilliseconds}ms");
            _output.WriteLine($"Deserialized {testObjects.Count} objects in {deserializationTime.TotalMilliseconds}ms");
        }

        [Fact]
        public void Method_Parameter_Enhancement_Tests()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;

            var inputParameters = new com.IvanMurzak.ReflectorNet.Model.SerializedMemberList
            {
                new() { name = "", typeName = "" }, // Will be enhanced
                new() { name = "integer", typeName = "System.Int32" },
                new() { name = "boolean", typeName = "System.Boolean" }
            };

            // Act - Test parameter enhancement
            inputParameters.EnhanceNames(methodInfo);
            inputParameters.EnhanceTypes(methodInfo);

            // Assert
            Assert.Equal("obj", inputParameters[0].name);
            Assert.NotNull(inputParameters[0].typeName);
            Assert.Contains("GameObjectRef", inputParameters[0].typeName);

            _output.WriteLine($"Enhanced parameters: {string.Join(", ", inputParameters.Select(p => $"{p.name}:{p.typeName}"))}");
        }

        [Fact]
        public void Reflector_Introspection_Tests()
        {
            // Arrange
            var reflector = new Reflector();
            var testType = typeof(GameObjectRef);

            // Act - Test introspection capabilities
            var schema = com.IvanMurzak.ReflectorNet.Utils.JsonUtils.Schema.GetSchema(testType);
            var typeId = com.IvanMurzak.ReflectorNet.Utils.JsonUtils.Schema.GetTypeId(testType);

            // Assert
            Assert.NotNull(schema);
            Assert.NotNull(typeId);
            Assert.Equal(testType.FullName, typeId);

            _output.WriteLine($"Type: {testType.FullName}");
            _output.WriteLine($"Schema: {schema}");
            _output.WriteLine($"TypeId: {typeId}");
        }

        [Fact]
        public void TypeUtils_Integration_Tests()
        {
            // Test type resolution
            var stringType = com.IvanMurzak.ReflectorNet.Utils.TypeUtils.GetType("System.String");
            Assert.Equal(typeof(string), stringType);

            var gameObjectRefType = com.IvanMurzak.ReflectorNet.Utils.TypeUtils.GetType(typeof(GameObjectRef).FullName!);
            Assert.Equal(typeof(GameObjectRef), gameObjectRefType);

            // Test default value generation
            var defaultInt = com.IvanMurzak.ReflectorNet.Utils.TypeUtils.GetDefaultValue(typeof(int));
            Assert.Equal(0, defaultInt);

            var defaultString = com.IvanMurzak.ReflectorNet.Utils.TypeUtils.GetDefaultValue(typeof(string));
            Assert.Null(defaultString);

            _output.WriteLine("TypeUtils integration tests passed");
        }

        [Fact]
        public void JsonUtils_Comprehensive_Tests()
        {
            // Arrange
            var testObject = new GameObjectRefList
            {
                new GameObjectRef { instanceID = 1, name = "Test1" },
                new GameObjectRef { instanceID = 2, name = "Test2" }
            };

            // Act - Test JsonUtils functionality
            var serializedJson = com.IvanMurzak.ReflectorNet.Utils.JsonUtils.Serialize(testObject);
            var deserializedObject = com.IvanMurzak.ReflectorNet.Utils.JsonUtils.Deserialize<GameObjectRefList>(serializedJson);

            var schema = com.IvanMurzak.ReflectorNet.Utils.JsonUtils.Schema.GetSchema(typeof(GameObjectRefList), justRef: false);
            var argumentsSchema = com.IvanMurzak.ReflectorNet.Utils.JsonUtils.Schema.GetArgumentsSchema(
                typeof(MethodHelper).GetMethod(nameof(MethodHelper.ListObject_ListObject))!);

            // Assert
            Assert.NotNull(serializedJson);
            Assert.NotNull(deserializedObject);
            Assert.Equal(testObject.Count, deserializedObject.Count);
            Assert.NotNull(schema);
            Assert.NotNull(argumentsSchema);

            _output.WriteLine($"Serialized JSON length: {serializedJson.Length}");
            _output.WriteLine($"Schema properties count: {schema.AsObject().Count}");
            _output.WriteLine($"Arguments schema: {argumentsSchema}");
        }

        [Fact]
        public void MethodDataRef_Construction_From_MethodInfo()
        {
            // Arrange
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.SerializedMemberList_ReturnString))!;

            // Act
            var methodDataRef = new com.IvanMurzak.ReflectorNet.Model.MethodDataRef(methodInfo);

            // Assert
            Assert.Equal(typeof(TestClass).Namespace, methodDataRef.Namespace);
            Assert.Equal(nameof(TestClass), methodDataRef.TypeName);
            Assert.Equal(nameof(TestClass.SerializedMemberList_ReturnString), methodDataRef.MethodName);
            Assert.Equal(methodInfo.IsPublic, methodDataRef.IsPublic);
            Assert.Equal(methodInfo.IsStatic, methodDataRef.IsStatic);
            Assert.Equal(methodInfo.ReturnType.FullName, methodDataRef.ReturnType);
            Assert.NotNull(methodDataRef.ReturnSchema);
            Assert.NotNull(methodDataRef.InputParametersSchema);
            Assert.Single(methodDataRef.InputParametersSchema); // SerializedMemberList parameter

            _output.WriteLine($"MethodDataRef created for: {methodDataRef}");
        }
    }
}
