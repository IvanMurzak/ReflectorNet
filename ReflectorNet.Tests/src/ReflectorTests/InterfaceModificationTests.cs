using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Interface with a number property for testing modification with interface types.
    /// </summary>
    public interface ITestNumber
    {
        int Number { get; set; }
    }

    /// <summary>
    /// Concrete implementation of ITestNumber.
    /// </summary>
    public class ClassWithNumber : ITestNumber
    {
        public int Number { get; set; }
    }

    /// <summary>
    /// Class with an interface-typed field for testing modification scenarios.
    /// </summary>
    public class ContainerWithInterfaceField
    {
        public ITestNumber? TestField;
    }

    /// <summary>
    /// Class with a concrete-typed field for comparison.
    /// </summary>
    public class ContainerWithConcreteField
    {
        public ClassWithNumber? TestField;
    }

    /// <summary>
    /// Tests for modification scenarios involving interface-typed members.
    /// These tests verify that TryModify correctly handles interface types
    /// which cannot be instantiated directly.
    /// </summary>
    public class InterfaceModificationTests : BaseTest
    {
        public InterfaceModificationTests(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// Test that TryModify fails when trying to modify an interface-typed field
        /// with serialized data that has the interface type.
        ///
        /// Scenario:
        /// 1. Create an instance of ContainerWithInterfaceField with TestField = null
        /// 2. Serialize a real ClassWithNumber instance with Number = 42
        /// 3. Modify the serialized data to use interface type ITestNumber instead of concrete type
        /// 4. Try to modify the container - should fail because interfaces cannot be instantiated
        /// </summary>
        [Fact]
        public void TryModify_InterfaceTypedField_WithInterfaceTypeData_Fails()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a real instance with Number = 42
            var sourceInstance = new ClassWithNumber { Number = 42 };

            // Serialize the concrete instance
            var serialized = reflector.Serialize(sourceInstance);
            Assert.NotNull(serialized);
            _output.WriteLine($"Serialized concrete type: {serialized.typeName}");

            // Modify the type to be the interface type instead of concrete type
            // This simulates data that was serialized with interface type information
            serialized.typeName = typeof(ITestNumber).GetTypeId();
            _output.WriteLine($"Modified to interface type: {serialized.typeName}");

            // Create a container with null interface field
            var container = new ContainerWithInterfaceField { TestField = null };
            object? obj = container.TestField;

            // Act - Try to modify with interface-typed data
            // This should fail because no converter can handle interface types
            var result = reflector.TryModify(
                ref obj,
                serialized,
                fallbackObjType: typeof(ITestNumber));

            // Assert - Modification should fail
            Assert.False(result, "TryModify should return false because interfaces cannot be instantiated");
            _output.WriteLine($"TryModify correctly returned false for interface type");
        }

        /// <summary>
        /// Test that TryModify succeeds when using concrete type data.
        /// This is the control test to show that modification works with concrete types.
        /// </summary>
        [Fact]
        public void TryModify_ConcreteTypedField_WithConcreteTypeData_Succeeds()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a real instance with Number = 42
            var sourceInstance = new ClassWithNumber { Number = 42 };

            // Serialize the concrete instance
            var serialized = reflector.Serialize(sourceInstance);
            Assert.NotNull(serialized);
            _output.WriteLine($"Serialized concrete type: {serialized.typeName}");

            // Create a target instance to modify
            object? obj = new ClassWithNumber { Number = 0 };

            // Act - Try to modify with concrete-typed data
            var result = reflector.TryModify(
                ref obj,
                serialized,
                fallbackObjType: typeof(ClassWithNumber));

            // Assert - Modification should succeed
            Assert.True(result, "TryModify should succeed with concrete type");
            Assert.NotNull(obj);
            var modified = obj as ClassWithNumber;
            Assert.NotNull(modified);
            Assert.Equal(42, modified.Number);
            _output.WriteLine($"TryModify succeeded, Number = {modified.Number}");
        }
    }
}
