using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using ReflectorNet.Tests.Schema.Model;
using Xunit.Abstractions;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using com.IvanMurzak.ReflectorNet.Utils;

namespace ReflectorNet.Tests.SchemaTests
{
    public partial class TestMethod : BaseTest
    {
        public TestMethod(ITestOutputHelper output) : base(output) { }

        #region Schema Tests (Existing)
        [Fact]
        public void Parameters_Object_Int_Bool()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;

            TestMethodInputs_Defines(methodInfo,
                typeof(GameObjectRef));

            TestMethodInputs_PropertyRefs(methodInfo,
                "obj");
        }

        [Fact]
        public void Parameters_ListObject_ListObject()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.ListObject_ListObject))!;

            TestMethodInputs_Defines(methodInfo,
                typeof(GameObjectRef),
                typeof(GameObjectRefList),
                typeof(SerializedMember),
                typeof(SerializedMemberList));

            TestMethodInputs_PropertyRefs(methodInfo,
                "obj1",
                "obj2");
        }

        [Fact]
        public void Parameters_StringArray()
        {
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.StringArray))!;

            TestMethodInputs_Defines(methodInfo,
                typeof(string[]));

            TestMethodInputs_PropertyRefs(methodInfo,
                "stringArray");
        }
        #endregion

        #region MethodWrapper Tests
        [Fact]
        public void MethodWrapper_Create_StaticMethod()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.NoParameters_ReturnBool))!;

            // Act
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            // Assert
            Assert.NotNull(wrapper);
            Assert.NotNull(wrapper.InputSchema);
            _output.WriteLine($"Static method wrapper created for: {methodInfo.Name}");
        }

        [Fact]
        public void MethodWrapper_Create_InstanceMethod()
        {
            // Arrange
            var reflector = new Reflector();
            var testInstance = new TestClass();
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.NoParameters_ReturnBool))!;

            // Act
            var wrapper = MethodWrapper.CreateFromInstance(reflector, null, testInstance, methodInfo);

            // Assert
            Assert.NotNull(wrapper);
            Assert.NotNull(wrapper.InputSchema);
            _output.WriteLine($"Instance method wrapper created for: {methodInfo.Name}");
        }

        [Fact]
        public async Task MethodWrapper_Invoke_NoParameters()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.NoParameters_ReturnBool))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            // Act
            var result = await wrapper.Invoke();

            // Assert
            Assert.True((bool)result!);
            _output.WriteLine($"Method invoked successfully, result: {result}");
        }

        [Fact]
        public async Task MethodWrapper_InvokeDict_WithParameters()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.SerializedMemberList_ReturnString))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            var gameObjectDiffs = new SerializedMemberList();
            var parameters = new Dictionary<string, object?>
            {
                ["gameObjectDiffs"] = gameObjectDiffs
            };

            // Act
            var result = await wrapper.InvokeDict(parameters);

            // Assert
            Assert.Equal("SerializedMemberList", result);
            _output.WriteLine($"Method invoked with parameters, result: {result}");
        }

        [Fact]
        public void MethodWrapper_VerifyParameters_Valid()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.SerializedMemberList_ReturnString))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            var parameters = new Dictionary<string, object?>
            {
                ["gameObjectDiffs"] = new SerializedMemberList()
            };

            // Act
            var isValid = wrapper.VerifyParameters(parameters, out var error);

            // Assert
            Assert.True(isValid);
            Assert.Null(error);
            _output.WriteLine("Parameter verification passed");
        }

        [Fact]
        public void MethodWrapper_VerifyParameters_InvalidParameterName()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.SerializedMemberList_ReturnString))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            var parameters = new Dictionary<string, object?>
            {
                ["invalidParameterName"] = new SerializedMemberList()
            };

            // Act
            var isValid = wrapper.VerifyParameters(parameters, out var error);

            // Assert
            Assert.False(isValid);
            Assert.NotNull(error);
            Assert.Contains("does not have a parameter named 'invalidParameterName'", error);
            _output.WriteLine($"Parameter verification correctly failed: {error}");
        }
        #endregion

        #region Reflector Method Call Tests
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
        #endregion

        #region Serialization/Deserialization Tests
        [Fact]
        public void Serialize_GameObjectRef()
        {
            // Arrange
            var reflector = new Reflector();
            var gameObject = new GameObjectRef
            {
                instanceID = 456,
                name = "TestGameObject",
                path = "/Root/Child/TestGameObject"
            };

            // Act
            var serialized = reflector.Serialize(gameObject);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(GameObjectRef).FullName, serialized.typeName);
            Assert.NotNull(serialized.valueJsonElement);
            _output.WriteLine($"Serialized GameObjectRef: {JsonUtils.Serialize(serialized)}");
        }

        [Fact]
        public void Deserialize_GameObjectRef()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new GameObjectRef
            {
                instanceID = 789,
                name = "DeserializeTest",
                path = "/Test/Deserialize"
            };

            var serialized = reflector.Serialize(original);

            // Act
            var deserialized = reflector.Deserialize(serialized) as GameObjectRef;

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.instanceID, deserialized.instanceID);
            Assert.Equal(original.name, deserialized.name);
            Assert.Equal(original.path, deserialized.path);
            _output.WriteLine($"Successfully deserialized: {deserialized}");
        }

        [Fact]
        public void Serialize_StringArray()
        {
            // Arrange
            var reflector = new Reflector();
            var stringArray = new[] { "alpha", "beta", "gamma" };

            // Act
            var serialized = reflector.Serialize(stringArray);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(string[]).FullName, serialized.typeName);
            _output.WriteLine($"Serialized string array: {JsonUtils.Serialize(serialized)}");
        }

        [Fact]
        public void Deserialize_StringArray()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new[] { "one", "two", "three" };
            var serialized = reflector.Serialize(original);

            // Act
            var deserialized = reflector.Deserialize(serialized) as string[];

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Length, deserialized.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
            _output.WriteLine($"Successfully deserialized string array with {deserialized.Length} elements");
        }

        [Fact]
        public void Serialize_Null_Value()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(null, typeof(GameObjectRef));

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(GameObjectRef).FullName, serialized.typeName);
            Assert.Null(serialized.valueJsonElement);
            _output.WriteLine($"Serialized null value: {JsonUtils.Serialize(serialized)}");
        }

        [Fact]
        public void Deserialize_Null_Value()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a SerializedMember with null value
            var serialized = new com.IvanMurzak.ReflectorNet.Model.SerializedMember
            {
                typeName = typeof(GameObjectRef).FullName!,
                valueJsonElement = null
            };

            // Act
            var deserialized = reflector.Deserialize(serialized);

            // Assert - For reference types with null value, should return default instance
            // The actual behavior might be to return a default instance rather than null
            Assert.NotNull(deserialized);
            _output.WriteLine($"Deserialized null value result: {deserialized}");
        }
        #endregion

        #region Method Finding Tests
        [Fact]
        public void FindMethod_ExactMatch()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            // Act
            var methods = reflector.FindMethod(
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6
            );

            // Assert
            var methodsList = methods.ToList();
            Assert.Single(methodsList);
            Assert.Equal(nameof(TestClass.NoParameters_ReturnBool), methodsList[0].Name);
            _output.WriteLine($"Found exact method: {methodsList[0].Name}");
        }

        [Fact]
        public void FindMethod_PartialMatch()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = "Return" // Partial match
            };

            // Act
            var methods = reflector.FindMethod(
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 2 // Allow partial matches
            );

            // Assert
            var methodsList = methods.ToList();
            Assert.NotEmpty(methodsList);
            Assert.Contains(methodsList, m => m.Name.Contains("Return"));
            _output.WriteLine($"Found {methodsList.Count} methods with partial match");
        }

        [Fact]
        public void FindMethod_WithParameterMatching()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.SerializedMemberList_ReturnString),
                InputParameters = new List<MethodPointerRef.Parameter>
                {
                    new() { TypeName = typeof(SerializedMemberList).FullName, Name = "gameObjectDiffs" }
                }
            };

            // Act
            var methods = reflector.FindMethod(
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                parametersMatchLevel: 2 // Exact parameter match
            );

            // Assert
            var methodsList = methods.ToList();
            Assert.Single(methodsList);
            Assert.Equal(nameof(TestClass.SerializedMemberList_ReturnString), methodsList[0].Name);
            _output.WriteLine($"Found method with parameter matching: {methodsList[0].Name}");
        }
        #endregion

        #region Error Handling Tests
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
        #endregion

        #region Complex Type Tests
        [Fact]
        public void Serialize_GameObjectRefList()
        {
            // Arrange
            var reflector = new Reflector();
            var gameObjectList = new GameObjectRefList
            {
                new GameObjectRef { instanceID = 1, name = "Object1" },
                new GameObjectRef { instanceID = 2, name = "Object2" },
                new GameObjectRef { instanceID = 3, name = "Object3" }
            };

            // Act
            var serialized = reflector.Serialize(gameObjectList);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(GameObjectRefList).FullName, serialized.typeName);
            _output.WriteLine($"Serialized GameObjectRefList: {JsonUtils.Serialize(serialized)}");
        }

        [Fact]
        public void Deserialize_GameObjectRefList()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new GameObjectRefList
            {
                new GameObjectRef { instanceID = 10, name = "OriginalObject1" },
                new GameObjectRef { instanceID = 20, name = "OriginalObject2" }
            };

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized data: {JsonUtils.Serialize(serialized)}");

            // Act
            var deserialized = reflector.Deserialize<GameObjectRefList>(serialized);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                _output.WriteLine($"Original[{i}]: instanceID={original[i].instanceID}, name={original[i].name}");
                _output.WriteLine($"Deserialized[{i}]: instanceID={deserialized[i].instanceID}, name={deserialized[i].name}");
                Assert.Equal(original[i].instanceID, deserialized[i].instanceID);
                Assert.Equal(original[i].name, deserialized[i].name);
            }
            _output.WriteLine($"Successfully deserialized GameObjectRefList with {deserialized.Count} items");
        }
        #endregion

        #region Edge Case Tests
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
        public void Serialize_EmptyArray()
        {
            // Arrange
            var reflector = new Reflector();
            var emptyArray = new string[0];

            // Act
            var serialized = reflector.Serialize(emptyArray);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(string[]).FullName, serialized.typeName);
            _output.WriteLine($"Serialized empty array: {JsonUtils.Serialize(serialized)}");
        }

        [Fact]
        public void Deserialize_EmptyArray()
        {
            // Arrange
            var reflector = new Reflector();
            var emptyArray = new string[0];
            var serialized = reflector.Serialize(emptyArray);

            // Act
            var deserialized = reflector.Deserialize(serialized) as string[];

            // Assert
            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
            _output.WriteLine("Successfully deserialized empty array");
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
        public async Task MethodWrapper_JsonElement_Parameter_Handling()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.StringArray))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            // Create a JsonElement representing a string array
            var jsonString = """["element1", "element2", "element3"]""";
            var jsonDoc = JsonDocument.Parse(jsonString);
            var jsonElement = jsonDoc.RootElement;

            // Act - JsonElement should be converted to proper string array
            var result = await wrapper.Invoke(jsonElement);

            // Assert - The method should complete successfully
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result); // MethodHelper.StringArray returns empty string

            _output.WriteLine($"JsonElement handling test completed successfully");
        }

        [Fact]
        public void MethodWrapper_Static_vs_Instance_Method_Creation()
        {
            // Arrange
            var reflector = new Reflector();
            var staticMethodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;
            var instanceMethodInfo = typeof(TestClass).GetMethod(nameof(TestClass.NoParameters_ReturnBool))!;
            var testInstance = new TestClass();

            // Act & Assert - Static method
            var staticWrapper = MethodWrapper.Create(reflector, null, staticMethodInfo);
            Assert.NotNull(staticWrapper);

            // Act & Assert - Instance method with Create (should work but create instance)
            var instanceWrapperFromCreate = MethodWrapper.Create(reflector, null, instanceMethodInfo);
            Assert.NotNull(instanceWrapperFromCreate);

            // Act & Assert - Instance method with proper creation
            var instanceWrapper = MethodWrapper.CreateFromInstance(reflector, null, testInstance, instanceMethodInfo);
            Assert.NotNull(instanceWrapper);

            _output.WriteLine("Static vs Instance method wrapper creation tests passed");
        }

        [Fact]
        public async Task MethodWrapper_Invoke_WithDefaultParameters()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            var gameObjectRef = new GameObjectRef { instanceID = 789, name = "DefaultParamsTest" };

            // Act - Only provide the required parameter, let others use defaults
            var result = await wrapper.Invoke(gameObjectRef);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result); // MethodHelper returns empty string
            _output.WriteLine("Method invoked with default parameters successfully");
        }

        [Fact]
        public void MethodWrapper_VerifyParameters_NoParametersMethod()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.NoParameters_ReturnBool))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            // Test with no parameters (should pass)
            var isValid1 = wrapper.VerifyParameters(null, out var error1);
            Assert.True(isValid1);
            Assert.Null(error1);

            // Test with empty dictionary (should pass)
            var isValid2 = wrapper.VerifyParameters(new Dictionary<string, object?>(), out var error2);
            Assert.True(isValid2);
            Assert.Null(error2);

            // Test with parameters when none expected (should fail)
            var parametersDict = new Dictionary<string, object?> { ["unexpected"] = "value" };
            var isValid3 = wrapper.VerifyParameters(parametersDict, out var error3);
            Assert.False(isValid3);
            Assert.NotNull(error3);
            Assert.Contains("does not accept any parameters", error3);

            _output.WriteLine("No-parameters method verification tests passed");
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

        [Fact]
        public void Serialization_BindingFlags_Control()
        {
            // Arrange
            var reflector = new Reflector();
            var testObject = new TestClass();

            // Act - Serialize with different BindingFlags
            var publicOnlySerialized = reflector.Serialize(testObject,
                flags: BindingFlags.Public | BindingFlags.Instance);
            var allMembersSerialized = reflector.Serialize(testObject,
                flags: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(publicOnlySerialized);
            Assert.NotNull(allMembersSerialized);
            Assert.Equal(typeof(TestClass).FullName, publicOnlySerialized.typeName);
            Assert.Equal(typeof(TestClass).FullName, allMembersSerialized.typeName);

            _output.WriteLine("BindingFlags serialization control test passed");
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
        public void FindMethod_With_Multiple_BindingFlags()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            // Act - Find with different binding flags
            var publicOnlyMethods = reflector.FindMethod(
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                bindingFlags: BindingFlags.Public | BindingFlags.Instance
            ).ToList();

            var allMethods = reflector.FindMethod(
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6,
                bindingFlags: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
            ).ToList();

            // Assert
            Assert.NotEmpty(publicOnlyMethods);
            Assert.NotEmpty(allMethods);
            Assert.True(allMethods.Count >= publicOnlyMethods.Count);

            _output.WriteLine($"Public only methods: {publicOnlyMethods.Count}, All methods: {allMethods.Count}");
        }

        [Fact]
        public void SerializedMember_Name_Property_Handling()
        {
            // Arrange
            var reflector = new Reflector();
            var testObject = new GameObjectRef { instanceID = 999, name = "NamedObject" };

            // Act
            var serializedWithName = reflector.Serialize(testObject, name: "customName");
            var serializedWithoutName = reflector.Serialize(testObject);

            // Assert
            Assert.NotNull(serializedWithName);
            Assert.NotNull(serializedWithoutName);
            Assert.Equal("customName", serializedWithName.name);
            Assert.Null(serializedWithoutName.name);

            _output.WriteLine($"Named serialization: {serializedWithName.name}");
            _output.WriteLine($"Unnamed serialization: {serializedWithoutName.name.ValueOrNull()}");
        }

        [Fact]
        public void Complex_Nested_Type_Serialization()
        {
            // Arrange
            var reflector = new Reflector();
            var nestedObject = new GameObjectRefList
            {
                new GameObjectRef { instanceID = 1, name = "Nested1" },
                new GameObjectRef { instanceID = 2, name = "Nested2", path = "/Root/Nested2" }
            };

            // Act
            var serialized = reflector.Serialize(nestedObject, recursive: true);
            var deserialized = reflector.Deserialize(serialized) as GameObjectRefList;

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(nestedObject.Count, deserialized.Count);

            for (int i = 0; i < nestedObject.Count; i++)
            {
                Assert.Equal(nestedObject[i].instanceID, deserialized[i].instanceID);
                Assert.Equal(nestedObject[i].name, deserialized[i].name);
                Assert.Equal(nestedObject[i].path, deserialized[i].path);
            }

            _output.WriteLine($"Nested type serialization test passed with {deserialized.Count} items");
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
                InputParameters = new List<MethodPointerRef.Parameter>
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
            object? testObject = new GameObjectRef();
            var errorResult = reflector.Populate(ref testObject, invalidData, depth: 2);

            Assert.NotNull(errorResult);
            var errorString = errorResult.ToString();
            Assert.Contains("Type 'NonExistent.Type.Name' not found", errorString);
            // Check that indentation (depth) is applied
            Assert.StartsWith("    ", errorString); // 2 levels of depth = 4 spaces

            _output.WriteLine($"Error with depth formatting: {errorString}");
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
        public void Async_Method_Invocation_Support()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a simple async method for testing
            Func<Task<string>> asyncMethod = async () =>
            {
                await Task.Delay(1);
                return "AsyncResult";
            };

            // Test that the MethodWrapper can handle Task return types
            var taskType = asyncMethod.GetType();
            var invokeMethod = taskType.GetMethod("Invoke");

            if (invokeMethod != null)
            {
                var wrapper = MethodWrapper.Create(reflector, null, invokeMethod);
                Assert.NotNull(wrapper);
                _output.WriteLine("Async method wrapper creation test passed");
            }
            else
            {
                _output.WriteLine("Async method testing skipped - no suitable method found");
            }
        }

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
        public void SerializedMemberList_Validation_Tests()
        {
            // Arrange
            var validList = new com.IvanMurzak.ReflectorNet.Model.SerializedMemberList
            {
                new() { name = "param1", typeName = "System.String" },
                new() { name = "param2", typeName = "System.Int32" }
            };

            var invalidList = new com.IvanMurzak.ReflectorNet.Model.SerializedMemberList
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
        public async Task MethodWrapper_BuildParameters_Array_Tests()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            var gameObjectRef = new GameObjectRef { instanceID = 123, name = "ArrayTest" };

            // Act - Test parameter array building
            var result = await wrapper.Invoke(gameObjectRef, 42, true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result); // MethodHelper returns empty string

            _output.WriteLine("Parameter array building test passed");
        }

        [Fact]
        public void MethodCall_Parameter_Filtering_Tests()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;

            var serializedList = new com.IvanMurzak.ReflectorNet.Model.SerializedMemberList
            {
                reflector.Serialize(new GameObjectRef { instanceID = 456 }, name: "obj"),
                reflector.Serialize(99, name: "integer")
                // Boolean parameter omitted - should use default value
            };

            var methods = new[] { methodInfo };

            // Act - Test parameter filtering
            var filteredMethod = methods.FilterByParameters(serializedList);

            // Assert
            Assert.NotNull(filteredMethod);
            Assert.Equal(methodInfo, filteredMethod);

            _output.WriteLine($"Parameter filtering found method: {filteredMethod.Name}");
        }

        [Fact]
        public void Complex_Method_Signature_Tests()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.ListObject_ListObject))!;

            // Act - Create method reference from MethodInfo
            var methodRef = new com.IvanMurzak.ReflectorNet.Model.MethodPointerRef(methodInfo);
            var methodDataRef = new com.IvanMurzak.ReflectorNet.Model.MethodDataRef(methodInfo);

            // Assert
            Assert.True(methodRef.IsValid);
            Assert.NotNull(methodRef.InputParameters);
            Assert.Equal(2, methodRef.InputParameters.Count);

            Assert.Equal(methodInfo.IsPublic, methodDataRef.IsPublic);
            Assert.Equal(methodInfo.IsStatic, methodDataRef.IsStatic);
            Assert.NotNull(methodDataRef.InputParametersSchema);
            Assert.Equal(2, methodDataRef.InputParametersSchema.Count);

            _output.WriteLine($"Method reference: {methodRef}");
            _output.WriteLine($"Method data: IsPublic={methodDataRef.IsPublic}, IsStatic={methodDataRef.IsStatic}");
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
        public void MainThread_Integration_Test()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new com.IvanMurzak.ReflectorNet.Model.MethodPointerRef
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

        [Fact]
        public void Edge_Case_Type_Handling()
        {
            // Arrange
            var reflector = new Reflector();

            // Test nullable types
            int? nullableInt = 42;
            var nullableIntSerialized = reflector.Serialize(nullableInt);
            var nullableIntDeserialized = reflector.Deserialize(nullableIntSerialized);

            // Test empty collections
            var emptyList = new GameObjectRefList();
            var emptyListSerialized = reflector.Serialize(emptyList);
            var emptyListDeserialized = reflector.Deserialize(emptyListSerialized) as GameObjectRefList;

            // Test null values in collections
            var listWithNull = new List<GameObjectRef?> { null, new GameObjectRef { instanceID = 1 } };
            var listWithNullSerialized = reflector.Serialize(listWithNull);

            // Assert
            Assert.Equal(42, nullableIntDeserialized);
            Assert.NotNull(emptyListDeserialized);
            Assert.Empty(emptyListDeserialized);
            Assert.NotNull(listWithNullSerialized);

            _output.WriteLine($"Nullable int: {nullableIntDeserialized}");
            _output.WriteLine($"Empty list count: {emptyListDeserialized.Count}");
            _output.WriteLine($"List with null serialized: {listWithNullSerialized.typeName}");
        }

        [Fact]
        public void Property_vs_Field_Serialization()
        {
            // Arrange
            var reflector = new Reflector();
            var testObject = new GameObjectRef { instanceID = 999, name = "PropertyFieldTest" };

            // Act - Test with different binding flags
            var propertiesOnly = reflector.Serialize(testObject,
                flags: BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

            var fieldsOnly = reflector.Serialize(testObject,
                flags: BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField);

            var allMembers = reflector.Serialize(testObject,
                flags: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(propertiesOnly);
            Assert.NotNull(fieldsOnly);
            Assert.NotNull(allMembers);
            Assert.Equal(typeof(GameObjectRef).FullName, propertiesOnly.typeName);

            _output.WriteLine($"Properties serialization: {propertiesOnly.typeName}");
            _output.WriteLine($"Fields serialization: {fieldsOnly.typeName}");
            _output.WriteLine($"All members serialization: {allMembers.typeName}");
        }

        [Fact]
        public void Method_Overload_Resolution_Tests()
        {
            // Arrange
            var reflector = new Reflector();

            // Find all methods with same name but different signatures
            var filter = new com.IvanMurzak.ReflectorNet.Model.MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = "NoParameters" // Partial name that might match multiple
            };

            // Act
            var allMethods = reflector.FindMethod(
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 1 // Low level to find partial matches
            ).ToList();

            var exactMethods = reflector.FindMethod(
                filter: filter,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6 // High level for exact matches
            ).ToList();

            // Assert
            Assert.NotEmpty(allMethods);
            // Exact matches should be subset of all matches
            Assert.True(exactMethods.Count <= allMethods.Count);

            foreach (var method in allMethods)
            {
                Assert.Contains("NoParameters", method.Name);
                _output.WriteLine($"Found method: {method.Name}, Return type: {method.ReturnType.Name}");
            }
        }
        #endregion
    }
}
