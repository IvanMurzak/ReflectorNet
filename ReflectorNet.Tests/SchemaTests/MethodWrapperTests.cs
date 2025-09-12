using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class MethodWrapperTests : BaseTest
    {
        public MethodWrapperTests(ITestOutputHelper output) : base(output) { }

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
        public async Task MethodWrapper_Enum_Parameter_String_Input()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(com.IvanMurzak.ReflectorNet.Tests.Utils.MethodHelper).GetMethod(nameof(com.IvanMurzak.ReflectorNet.Tests.Utils.MethodHelper.ProcessEnum))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            // Act & Assert - Test with string representation of enum
            var parameters = new Dictionary<string, object?> 
            { 
                { "enumValue", "Option2" } 
            };

            var result = await wrapper.InvokeDict(parameters);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Processed enum: Option2", result.ToString());
            _output.WriteLine($"Enum parameter test with string input: {result}");
        }

        [Fact]
        public async Task MethodWrapper_Enum_Parameter_JsonElement_Input()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(com.IvanMurzak.ReflectorNet.Tests.Utils.MethodHelper).GetMethod(nameof(com.IvanMurzak.ReflectorNet.Tests.Utils.MethodHelper.ProcessEnum))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            // Act & Assert - Test with JsonElement containing enum string
            var jsonDocument = JsonDocument.Parse("\"Option3\"");
            var jsonElement = jsonDocument.RootElement;
            
            var parameters = new Dictionary<string, object?> 
            { 
                { "enumValue", jsonElement } 
            };

            var result = await wrapper.InvokeDict(parameters);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Processed enum: Option3", result.ToString());
            _output.WriteLine($"Enum parameter test with JsonElement input: {result}");
        }

        [Fact]
        public async Task MethodWrapper_Enum_Parameter_Default_Value()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(com.IvanMurzak.ReflectorNet.Tests.Utils.MethodHelper).GetMethod(nameof(com.IvanMurzak.ReflectorNet.Tests.Utils.MethodHelper.ProcessEnumWithDefault))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            // Act & Assert - Test with no parameter (should use default value Option2)
            var result = await wrapper.InvokeDict(null);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Processed enum with default: Option2", result.ToString());
            _output.WriteLine($"Enum parameter test with default value: {result}");
        }

        [Fact]
        public async Task MethodWrapper_Enum_Parameter_Mixed_Types()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(com.IvanMurzak.ReflectorNet.Tests.Utils.MethodHelper).GetMethod(nameof(com.IvanMurzak.ReflectorNet.Tests.Utils.MethodHelper.ProcessStringAndEnum))!;
            var wrapper = MethodWrapper.Create(reflector, null, methodInfo);

            // Act & Assert - Test with string and enum parameters
            var parameters = new Dictionary<string, object?> 
            { 
                { "text", "Hello" },
                { "enumValue", "Option4" } 
            };

            var result = await wrapper.InvokeDict(parameters);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Text: Hello, Enum: Option4", result.ToString());
            _output.WriteLine($"Mixed parameters test with enum: {result}");
        }
    }
}
