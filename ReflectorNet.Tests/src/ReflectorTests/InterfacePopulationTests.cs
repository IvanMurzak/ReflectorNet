using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Interface with a number property for testing population with interface types.
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
    /// Class with an interface-typed field for testing population scenarios.
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
    /// Tests for population scenarios involving interface-typed members.
    /// These tests verify that TryPopulate correctly handles interface types
    /// which cannot be instantiated directly.
    /// </summary>
    public class InterfacePopulationTests : BaseTest
    {
        public InterfacePopulationTests(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// Test that TryPopulate fails when trying to populate an interface-typed field
        /// with serialized data that has the interface type.
        ///
        /// Scenario:
        /// 1. Create an instance of ContainerWithInterfaceField with TestField = null
        /// 2. Serialize a real ClassWithNumber instance with Number = 42
        /// 3. Modify the serialized data to use interface type ITestNumber instead of concrete type
        /// 4. Try to populate the container - should fail because interfaces cannot be instantiated
        /// </summary>
        [Fact]
        public void TryPopulate_InterfaceTypedField_WithInterfaceTypeData_Fails()
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

            // Act - Try to populate with interface-typed data
            // This should fail because no converter can handle interface types
            var result = reflector.TryPopulate(
                ref obj,
                serialized,
                fallbackObjType: typeof(ITestNumber));

            // Assert - Population should fail
            Assert.False(result, "TryPopulate should return false because interfaces cannot be instantiated");
            _output.WriteLine($"TryPopulate correctly returned false for interface type");
        }

        /// <summary>
        /// Test that TryPopulate succeeds when using concrete type data.
        /// This is the control test to show that population works with concrete types.
        /// </summary>
        [Fact]
        public void TryPopulate_ConcreteTypedField_WithConcreteTypeData_Succeeds()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a real instance with Number = 42
            var sourceInstance = new ClassWithNumber { Number = 42 };

            // Serialize the concrete instance
            var serialized = reflector.Serialize(sourceInstance);
            Assert.NotNull(serialized);
            _output.WriteLine($"Serialized concrete type: {serialized.typeName}");

            // Create a target instance to populate
            object? obj = new ClassWithNumber { Number = 0 };

            // Act - Try to populate with concrete-typed data
            var result = reflector.TryPopulate(
                ref obj,
                serialized,
                fallbackObjType: typeof(ClassWithNumber));

            // Assert - Population should succeed
            Assert.True(result, "TryPopulate should succeed with concrete type");
            Assert.NotNull(obj);
            var populated = obj as ClassWithNumber;
            Assert.NotNull(populated);
            Assert.Equal(42, populated.Number);
            _output.WriteLine($"TryPopulate succeeded, Number = {populated.Number}");
        }
    }
}
