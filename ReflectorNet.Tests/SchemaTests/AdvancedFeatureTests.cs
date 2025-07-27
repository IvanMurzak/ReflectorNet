using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Tests.Schema.Model;
using Xunit.Abstractions;
using System;
using System.Reflection;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class AdvancedFeatureTests : BaseTest
    {
        public AdvancedFeatureTests(ITestOutputHelper output) : base(output) { }

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
            Assert.Equal(typeof(GameObjectRef).GetTypeName(pretty: false), propertiesOnly.typeName);

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
            var filter = new MethodPointerRef
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
    }
}
