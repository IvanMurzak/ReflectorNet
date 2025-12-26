using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    public interface ICharacterController
    {
        void BeforeCharacterUpdate(float deltaTime);
        void PostGroundingUpdate(float deltaTime);
        void AfterCharacterUpdate(float deltaTime);
    }

    public class MockCharacterController : ICharacterController
    {
        public int Id { get; set; } = 1;
        public float Speed { get; set; } = 10.0f;

        public void BeforeCharacterUpdate(float deltaTime) { }
        public void PostGroundingUpdate(float deltaTime) { }
        public void AfterCharacterUpdate(float deltaTime) { }
    }

    public class ClassWithNonSerializedInterface
    {
        public string? Name;

        [System.NonSerialized]
        public ICharacterController? CharacterController;
    }

    public class ClassWithPropertyHavingNonSerializedBackingField
    {
        public string? Name;

        [field: System.NonSerialized]
        public string? Description { get; set; }
    }

    public class BaseClassWithPropertyHavingNonSerializedBackingField
    {
        [field: System.NonSerialized]
        public string? BaseDescription { get; set; }
    }

    public class DerivedClass : BaseClassWithPropertyHavingNonSerializedBackingField
    {
        public string? DerivedName;
    }

    public class ClassWithInterfaceField
    {
        public string? Name;
        public ICharacterController? CharacterController;
    }

    public class ClassWithInterfaceProperty
    {
        public string? Name;
        public ICharacterController? CharacterController { get; set; }
    }

    public class NonSerializedInterfaceTests : BaseTest
    {
        public NonSerializedInterfaceTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void TestNonSerializedInterfaceField()
        {
            var instance = new ClassWithNonSerializedInterface
            {
                Name = "TestInstance",
                CharacterController = null // Or mock it if needed, but null should be fine for serialization if it's ignored
            };

            var reflector = new Reflector();

            // This should not throw
            var serialized = reflector.Serialize(instance);

            Assert.NotNull(serialized);

            var deserialized = reflector.Deserialize<ClassWithNonSerializedInterface>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal("TestInstance", deserialized.Name);
            Assert.Null(deserialized.CharacterController);
        }

        [Fact]
        public void TestNonSerializedInterfaceField_NotNull()
        {
            var instance = new ClassWithNonSerializedInterface
            {
                Name = "TestInstance",
                CharacterController = new MockCharacterController()
            };

            var reflector = new Reflector();

            // This should not throw
            var serialized = reflector.Serialize(instance);

            Assert.NotNull(serialized);

            var deserialized = reflector.Deserialize<ClassWithNonSerializedInterface>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal("TestInstance", deserialized.Name);
            Assert.Null(deserialized.CharacterController);
        }

        [Fact]
        public void TestNonSerializedInterfaceProperty()
        {
            var instance = new ClassWithPropertyHavingNonSerializedBackingField
            {
                Name = "TestInstance",
                Description = "ShouldBeSerialized"
            };

            var reflector = new Reflector();

            // This should not throw
            var serialized = reflector.Serialize(instance);

            Assert.NotNull(serialized);

            var deserialized = reflector.Deserialize<ClassWithPropertyHavingNonSerializedBackingField>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal("TestInstance", deserialized.Name);
            Assert.Equal("ShouldBeSerialized", deserialized.Description);
        }


        [Fact]
        public void TestNonSerializedInheritedProperty()
        {
            var instance = new DerivedClass
            {
                DerivedName = "Derived",
                BaseDescription = "BaseShouldBeSerialized"
            };

            var reflector = new Reflector();

            // This should not throw
            var serialized = reflector.Serialize(instance);

            Assert.NotNull(serialized);

            var deserialized = reflector.Deserialize<DerivedClass>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal("Derived", deserialized.DerivedName);
            Assert.Equal("BaseShouldBeSerialized", deserialized.BaseDescription);
        }

        [Fact]
        public void TestInterfaceField_Null_NoAttribute()
        {
            var instance = new ClassWithInterfaceField
            {
                Name = "TestInstance",
                CharacterController = null
            };

            var reflector = new Reflector();

            // Fields now swallow exceptions during serialization (like properties), so this should NOT throw
            var serialized = reflector.Serialize(instance);
            Assert.NotNull(serialized);

            var deserialized = reflector.Deserialize<ClassWithInterfaceField>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal("TestInstance", deserialized.Name);
            Assert.Null(deserialized.CharacterController);
        }

        [Fact]
        public void TestInterfaceField_NotNull_NoAttribute()
        {
            var instance = new ClassWithInterfaceField
            {
                Name = "TestInstance",
                CharacterController = new MockCharacterController { Id = 99, Speed = 123.45f }
            };

            var reflector = new Reflector();

            var serialized = reflector.Serialize(instance);
            Assert.NotNull(serialized);

            var deserialized = reflector.Deserialize<ClassWithInterfaceField>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal("TestInstance", deserialized.Name);
            Assert.NotNull(deserialized.CharacterController);
            Assert.IsType<MockCharacterController>(deserialized.CharacterController);

            var controller = (MockCharacterController)deserialized.CharacterController;
            Assert.Equal(99, controller.Id);
            Assert.Equal(123.45f, controller.Speed);
        }

        [Fact]
        public void TestInterfaceProperty_Null_NoAttribute()
        {
            var instance = new ClassWithInterfaceProperty
            {
                Name = "TestInstance",
                CharacterController = null
            };

            var reflector = new Reflector();

            // Properties swallow exceptions during serialization, so this should NOT throw
            var serialized = reflector.Serialize(instance);
            Assert.NotNull(serialized);

            var deserialized = reflector.Deserialize<ClassWithInterfaceProperty>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal("TestInstance", deserialized.Name);
            Assert.Null(deserialized.CharacterController);
        }

        [Fact]
        public void TestInterfaceProperty_NotNull_NoAttribute()
        {
            var instance = new ClassWithInterfaceProperty
            {
                Name = "TestInstance",
                CharacterController = new MockCharacterController { Id = 88, Speed = 67.89f }
            };

            var reflector = new Reflector();

            var serialized = reflector.Serialize(instance);
            Assert.NotNull(serialized);

            var deserialized = reflector.Deserialize<ClassWithInterfaceProperty>(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal("TestInstance", deserialized.Name);
            Assert.NotNull(deserialized.CharacterController);
            Assert.IsType<MockCharacterController>(deserialized.CharacterController);

            var controller = (MockCharacterController)deserialized.CharacterController;
            Assert.Equal(88, controller.Id);
            Assert.Equal(67.89f, controller.Speed);
        }
    }
}
