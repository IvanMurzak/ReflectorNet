using System.Linq;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using Xunit.Abstractions;
using com.IvanMurzak.ReflectorNet.Utils;

namespace ReflectorNet.Tests.ReflectorTests
{
    public class FindMethod : BaseTest
    {
        public FindMethod(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void WithKnownMethod_ShouldReturnMethod()
        {
            // Arrange
            var reflector = new Reflector();
            var methodPointer = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = nameof(TestClass.NoParameters_ReturnBool)
            };

            // Act
            var foundMethods = reflector.FindMethod(
                methodPointer,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 6
            ).ToList();

            _output.WriteLine(JsonUtils.ToJson(foundMethods));

            // Assert
            Assert.Single(foundMethods);
            Assert.Equal(nameof(TestClass.NoParameters_ReturnBool), foundMethods[0].Name);
            Assert.Equal(typeof(bool), foundMethods[0].ReturnType);
        }

        [Fact]
        public void WithPartialMethodName_ShouldReturnMatchingMethods()
        {
            // Arrange
            var reflector = new Reflector();
            var methodPointer = new MethodPointerRef
            {
                Namespace = typeof(TestClass).Namespace,
                TypeName = nameof(TestClass),
                MethodName = "Return" // Partial method name
            };

            // Act
            var foundMethods = reflector.FindMethod(
                methodPointer,
                knownNamespace: true,
                typeNameMatchLevel: 6,
                methodNameMatchLevel: 2 // Lower level to match partial names
            ).ToList();

            _output.WriteLine(JsonUtils.ToJson(foundMethods));

            // Assert
            Assert.Contains(foundMethods, m => m.Name == nameof(TestClass.NoParameters_ReturnBool));
        }
    }
}
