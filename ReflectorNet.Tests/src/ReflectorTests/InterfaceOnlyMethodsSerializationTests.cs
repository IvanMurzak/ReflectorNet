using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    /// <summary>
    /// Interface with only a void method, no fields or properties.
    /// This is a minimal interface that has no serializable members.
    /// </summary>
    public interface IActionWithNoMembers
    {
        void Execute();
    }

    /// <summary>
    /// Concrete implementation of the minimal interface.
    /// Also has no serializable fields or properties.
    /// </summary>
    public class ActionWithNoMembers : IActionWithNoMembers
    {
        public void Execute() { }
    }

    /// <summary>
    /// Class with a property of interface type that has no fields/properties.
    /// </summary>
    public class ClassWithInterfaceOnlyMethodsProperty
    {
        public string? Name { get; set; }
        public IActionWithNoMembers? Action { get; set; }
    }

    /// <summary>
    /// Class with a field of interface type that has no fields/properties.
    /// </summary>
    public class ClassWithInterfaceOnlyMethodsField
    {
        public string? Name;
        public IActionWithNoMembers? Action;
    }

    /// <summary>
    /// Tests for serializing classes that contain interface-typed members
    /// where the interface has no fields/properties, only methods.
    ///
    /// This test reproduces the issue at Reflector.cs:139 where an ArgumentException
    /// is thrown because no converter can handle interface types with null values.
    /// </summary>
    public class InterfaceOnlyMethodsSerializationTests : BaseTest
    {
        public InterfaceOnlyMethodsSerializationTests(ITestOutputHelper output) : base(output) { }

        /// <summary>
        /// Test that serializing a null value with an interface fallback type
        /// returns a SerializedMember with null value (no exception thrown).
        ///
        /// This was fixed by adding early handling in Reflector.Serialize() for
        /// null objects with interface or abstract types.
        /// </summary>
        [Fact]
        public void Serialize_InterfaceType_Directly_NullValue_ReturnsNullMember()
        {
            // Arrange
            IActionWithNoMembers? nullAction = null;
            var reflector = new Reflector();

            // Act - This should now succeed and return a SerializedMember with null value
            var serialized = reflector.Serialize(nullAction, fallbackType: typeof(IActionWithNoMembers));

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(IActionWithNoMembers).GetTypeId(), serialized.typeName);
            _output.WriteLine($"Serialized successfully: typeName={serialized.typeName}");
        }

        /// <summary>
        /// Test that serializing a class with an interface-typed property set to null
        /// works correctly. The fix handles null interface values gracefully.
        /// </summary>
        [Fact]
        public void Serialize_ClassWithInterfaceOnlyMethodsProperty_NullValue_Succeeds()
        {
            // Arrange
            var instance = new ClassWithInterfaceOnlyMethodsProperty
            {
                Name = "TestInstance",
                Action = null // Null interface property is now handled gracefully
            };

            var reflector = new Reflector();

            // Act - Serialization succeeds with the fix
            var serialized = reflector.Serialize(instance);

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.props);
            _output.WriteLine($"Serialized successfully with null interface property");
        }

        /// <summary>
        /// Test that serializing a class with an interface-typed field set to null
        /// works correctly. The fix handles null interface values gracefully.
        /// </summary>
        [Fact]
        public void Serialize_ClassWithInterfaceOnlyMethodsField_NullValue_Succeeds()
        {
            // Arrange
            var instance = new ClassWithInterfaceOnlyMethodsField
            {
                Name = "TestInstance",
                Action = null // Null interface field is now handled gracefully
            };

            var reflector = new Reflector();

            // Act - Serialization succeeds with the fix
            var serialized = reflector.Serialize(instance);

            // Assert
            Assert.NotNull(serialized);
            _output.WriteLine($"Serialized successfully with null interface field");
        }

        /// <summary>
        /// Test that serializing an interface type with a null value works correctly.
        /// The fix handles null objects with interface types by returning SerializedMember.Null.
        /// </summary>
        [Fact]
        public void Serialize_InterfaceType_NullValue_Succeeds()
        {
            // Arrange
            IActionWithNoMembers? nullAction = null;
            var reflector = new Reflector();

            // Act - This now works after the fix in Reflector.Serialize()
            var serialized = reflector.Serialize(nullAction, fallbackType: typeof(IActionWithNoMembers));

            // Assert - We expect a valid SerializedMember with null value
            Assert.NotNull(serialized);
            Assert.Equal(typeof(IActionWithNoMembers).GetTypeId(), serialized.typeName);
            _output.WriteLine($"Serialized successfully: typeName={serialized.typeName}");
        }

        /// <summary>
        /// Test that serialization works when the interface property has a concrete value.
        /// When the value is not null, the serializer uses the concrete type, not the interface type.
        /// </summary>
        [Fact]
        public void Serialize_ClassWithInterfaceOnlyMethodsProperty_WithValue_Succeeds()
        {
            // Arrange
            var instance = new ClassWithInterfaceOnlyMethodsProperty
            {
                Name = "TestInstance",
                Action = new ActionWithNoMembers() // Concrete implementation
            };

            var reflector = new Reflector();

            // Act - This should succeed because the concrete type is used
            var serialized = reflector.Serialize(instance);

            // Assert
            Assert.NotNull(serialized);
            _output.WriteLine($"Serialized successfully: {serialized.typeName}");
        }

        /// <summary>
        /// Test that serialization works when the interface field has a concrete value.
        /// </summary>
        [Fact]
        public void Serialize_ClassWithInterfaceOnlyMethodsField_WithValue_Succeeds()
        {
            // Arrange
            var instance = new ClassWithInterfaceOnlyMethodsField
            {
                Name = "TestInstance",
                Action = new ActionWithNoMembers() // Concrete implementation
            };

            var reflector = new Reflector();

            // Act - This should succeed because the concrete type is used
            var serialized = reflector.Serialize(instance);

            // Assert
            Assert.NotNull(serialized);
            _output.WriteLine($"Serialized successfully: {serialized.typeName}");
        }
    }
}
