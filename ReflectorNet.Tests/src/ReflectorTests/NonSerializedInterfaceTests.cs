using System;
using Xunit;
using Xunit.Abstractions;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Tests.ReflectorTests
{
    public interface ICharacterController
    {
        void BeforeCharacterUpdate(float deltaTime);
        void PostGroundingUpdate(float deltaTime);
        void AfterCharacterUpdate(float deltaTime);
    }

    public class ClassWithNonSerializedInterface
    {
        public string Name;

        [System.NonSerialized]
        public ICharacterController CharacterController;
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
            // Verify that the field is NOT present in the serialized data if it's NonSerialized
            // Or if ReflectorNet handles NonSerialized by ignoring it.
        }
    }
}
