using System;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using Xunit;

namespace ReflectorNet.Tests
{
    public class ReflectorTests
    {
        [Fact]
        public void FindMethod_WithKnownMethod_ShouldReturnMethod()
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

            // Assert
            Assert.Single(foundMethods);
            Assert.Equal(nameof(TestClass.NoParameters_ReturnBool), foundMethods[0].Name);
            Assert.Equal(typeof(bool), foundMethods[0].ReturnType);
        }

        [Fact]
        public void FindMethod_WithPartialMethodName_ShouldReturnMatchingMethods()
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

            // Assert
            Assert.Contains(foundMethods, m => m.Name == nameof(TestClass.NoParameters_ReturnBool));
        }
    }
}
