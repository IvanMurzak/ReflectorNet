using System;
using com.IvanMurzak.ReflectorNet.Model;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Interface for testing deserialization scenarios.
    /// </summary>
    public interface IDeserializableInterface
    {
        int Value { get; set; }
    }

    /// <summary>
    /// Abstract class for testing deserialization scenarios.
    /// </summary>
    public abstract class AbstractDeserializableClass
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// Concrete implementation of the interface.
    /// </summary>
    public class ConcreteDeserializableClass : IDeserializableInterface
    {
        public int Value { get; set; }
    }

    /// <summary>
    /// Concrete implementation of the abstract class.
    /// </summary>
    public class ConcreteFromAbstractClass : AbstractDeserializableClass
    {
    }

    /// <summary>
    /// Tests for deserialization scenarios involving interface and abstract class types.
    /// These tests verify that:
    /// - Deserializing non-null data with interface type throws an exception
    /// - Deserializing non-null data with abstract class type throws an exception
    /// - Deserializing null data with interface type returns null (success)
    /// - Deserializing null data with abstract class type returns null (success)
    /// </summary>
    public class InterfaceDeserializationTests : BaseTest
    {
        public InterfaceDeserializationTests(ITestOutputHelper output) : base(output) { }

        #region Interface Tests

        /// <summary>
        /// Test that deserializing a SerializedMember with interface typeName and non-null data
        /// throws an ArgumentException because interfaces cannot be instantiated.
        /// </summary>
        [Fact]
        public void Deserialize_InterfaceType_WithNonNullData_ThrowsException()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a concrete instance and serialize it
            var sourceInstance = new ConcreteDeserializableClass { Value = 42 };
            var serialized = reflector.Serialize(sourceInstance);
            Assert.NotNull(serialized);
            _output.WriteLine($"Original serialized type: {serialized.typeName}");

            // Modify the typeName to be the interface type
            serialized.typeName = typeof(IDeserializableInterface).GetTypeId();
            _output.WriteLine($"Modified to interface type: {serialized.typeName}");

            // Act & Assert - Should throw because interfaces cannot be instantiated
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                reflector.Deserialize(serialized);
            });

            _output.WriteLine($"Exception message: {exception.Message}");
            Assert.Contains("interface", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Test that deserializing a SerializedMember with interface typeName and null data
        /// succeeds and returns null.
        /// </summary>
        [Fact]
        public void Deserialize_InterfaceType_WithNullData_ReturnsNull()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a SerializedMember with interface type and null value
            var serialized = SerializedMember.Null(typeof(IDeserializableInterface));
            _output.WriteLine($"SerializedMember type: {serialized.typeName}");
            _output.WriteLine($"SerializedMember IsNull: {serialized.IsNull()}");

            // Act - Should succeed and return null
            var result = reflector.Deserialize(serialized);

            // Assert
            Assert.Null(result);
            _output.WriteLine($"Deserialize correctly returned null for interface type with null data");
        }

        /// <summary>
        /// Test that deserializing with interface fallback type and null data returns null.
        /// </summary>
        [Fact]
        public void Deserialize_InterfaceType_WithFallbackType_NullData_ReturnsNull()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a SerializedMember with null value (no specific type)
            var serialized = SerializedMember.Null(typeof(IDeserializableInterface));
            _output.WriteLine($"SerializedMember type: {serialized.typeName}");

            // Act - Should succeed and return null
            var result = reflector.Deserialize(
                serialized,
                fallbackType: typeof(IDeserializableInterface));

            // Assert
            Assert.Null(result);
            _output.WriteLine($"Deserialize correctly returned null for interface fallback type with null data");
        }

        #endregion

        #region Abstract Class Tests

        /// <summary>
        /// Test that deserializing a SerializedMember with abstract class typeName and non-null data
        /// throws an exception because abstract classes cannot be instantiated.
        /// Note: Abstract classes take a different code path than interfaces and throw
        /// InvalidOperationException from CreateInstance instead of ArgumentException.
        /// </summary>
        [Fact]
        public void Deserialize_AbstractClassType_WithNonNullData_ThrowsException()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a concrete instance and serialize it
            var sourceInstance = new ConcreteFromAbstractClass { Value = 42 };
            var serialized = reflector.Serialize(sourceInstance);
            Assert.NotNull(serialized);
            _output.WriteLine($"Original serialized type: {serialized.typeName}");

            // Modify the typeName to be the abstract class type
            serialized.typeName = typeof(AbstractDeserializableClass).GetTypeId();
            _output.WriteLine($"Modified to abstract class type: {serialized.typeName}");

            // Act & Assert - Should throw because abstract classes cannot be instantiated
            // Abstract classes have a valid converter (they inherit from object) but fail
            // when trying to create an instance, throwing InvalidOperationException
            var exception = Assert.Throws<InvalidOperationException>(() =>
            {
                reflector.Deserialize(serialized);
            });

            _output.WriteLine($"Exception message: {exception.Message}");
            Assert.Contains("abstract", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Test that deserializing a SerializedMember with abstract class typeName and null data
        /// succeeds and returns null.
        /// </summary>
        [Fact]
        public void Deserialize_AbstractClassType_WithNullData_ReturnsNull()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a SerializedMember with abstract class type and null value
            var serialized = SerializedMember.Null(typeof(AbstractDeserializableClass));
            _output.WriteLine($"SerializedMember type: {serialized.typeName}");
            _output.WriteLine($"SerializedMember IsNull: {serialized.IsNull()}");

            // Act - Should succeed and return null
            var result = reflector.Deserialize(serialized);

            // Assert
            Assert.Null(result);
            _output.WriteLine($"Deserialize correctly returned null for abstract class type with null data");
        }

        /// <summary>
        /// Test that deserializing with abstract class fallback type and null data returns null.
        /// </summary>
        [Fact]
        public void Deserialize_AbstractClassType_WithFallbackType_NullData_ReturnsNull()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a SerializedMember with null value
            var serialized = SerializedMember.Null(typeof(AbstractDeserializableClass));
            _output.WriteLine($"SerializedMember type: {serialized.typeName}");

            // Act - Should succeed and return null
            var result = reflector.Deserialize(
                serialized,
                fallbackType: typeof(AbstractDeserializableClass));

            // Assert
            Assert.Null(result);
            _output.WriteLine($"Deserialize correctly returned null for abstract class fallback type with null data");
        }

        #endregion

        #region Control Tests (Concrete Types)

        /// <summary>
        /// Control test: deserializing concrete type with non-null data succeeds.
        /// </summary>
        [Fact]
        public void Deserialize_ConcreteType_WithNonNullData_Succeeds()
        {
            // Arrange
            var reflector = new Reflector();

            var sourceInstance = new ConcreteDeserializableClass { Value = 42 };
            var serialized = reflector.Serialize(sourceInstance);
            Assert.NotNull(serialized);
            _output.WriteLine($"Serialized concrete type: {serialized.typeName}");

            // Act
            var result = reflector.Deserialize(serialized);

            // Assert
            Assert.NotNull(result);
            var concrete = result as ConcreteDeserializableClass;
            Assert.NotNull(concrete);
            Assert.Equal(42, concrete.Value);
            _output.WriteLine($"Deserialize succeeded, Value = {concrete.Value}");
        }

        /// <summary>
        /// Control test: deserializing concrete type with null data returns null.
        /// </summary>
        [Fact]
        public void Deserialize_ConcreteType_WithNullData_ReturnsNull()
        {
            // Arrange
            var reflector = new Reflector();

            var serialized = SerializedMember.Null(typeof(ConcreteDeserializableClass));
            _output.WriteLine($"SerializedMember type: {serialized.typeName}");

            // Act
            var result = reflector.Deserialize(serialized);

            // Assert
            Assert.Null(result);
            _output.WriteLine($"Deserialize correctly returned null for concrete type with null data");
        }

        #endregion
    }
}
