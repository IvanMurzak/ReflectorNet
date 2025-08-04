using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Tests.Model;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class MethodFindingTests : BaseTest
    {
        public MethodFindingTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void FindMethod_ExactMatch()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodRef
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
            var filter = new MethodRef
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
            var filter = new MethodRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.SerializedMemberList_ReturnString),
                InputParameters = new List<MethodRef.Parameter>
                {
                    new() { TypeName = typeof(SerializedMemberList).GetTypeName(pretty: false), Name = "gameObjectDiffs" }
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

        [Fact]
        public void FindMethod_With_Multiple_BindingFlags()
        {
            // Arrange
            var reflector = new Reflector();
            var filter = new MethodRef
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
        public void MethodCall_Parameter_Filtering_Tests()
        {
            // Arrange
            var reflector = new Reflector();
            var methodInfo = typeof(MethodHelper).GetMethod(nameof(MethodHelper.Object_Int_Bool))!;

            var serializedList = new SerializedMemberList
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
            var methodRef = new com.IvanMurzak.ReflectorNet.Model.MethodRef(methodInfo);
            var methodDataRef = new MethodData(methodInfo);

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
    }
}
