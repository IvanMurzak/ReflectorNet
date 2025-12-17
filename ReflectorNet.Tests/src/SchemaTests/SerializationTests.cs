using com.IvanMurzak.ReflectorNet.Model;
using Xunit.Abstractions;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.ReflectorNet.Tests.Model;

namespace com.IvanMurzak.ReflectorNet.Tests.SchemaTests
{
    public class SerializationTests : BaseTest
    {
        public SerializationTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Serialize_GameObjectRef()
        {
            // Arrange
            var reflector = new Reflector();
            var gameObject = new GameObjectRef
            {
                instanceID = 456,
                name = "TestGameObject",
                path = "/Root/Child/TestGameObject"
            };

            // Act
            var serialized = reflector.Serialize(gameObject);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(GameObjectRef).GetTypeId(), serialized.typeName);
            Assert.NotNull(serialized.valueJsonElement);
            _output.WriteLine($"Serialized GameObjectRef: {serialized.ToJson(reflector)}");
        }

        [Fact]
        public void Deserialize_GameObjectRef()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new GameObjectRef
            {
                instanceID = 789,
                name = "DeserializeTest",
                path = "/Test/Deserialize"
            };

            // Act
            var serializeLogger = new StringBuilderLogger();
            var serialized = reflector.Serialize(original, name: nameof(original), logger: serializeLogger);
            _output.WriteLine($"Serialize result:\n{serializeLogger}");

            var deserializeLogger = new StringBuilderLogger();
            var deserialized = reflector.Deserialize(serialized, logger: deserializeLogger) as GameObjectRef;
            _output.WriteLine($"Deserialize result:\n{deserializeLogger}");

            _output.WriteLine($"Json:\n{serialized.ToJson(reflector)}");

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.instanceID, deserialized.instanceID);
            Assert.Equal(original.name, deserialized.name);
            Assert.Equal(original.path, deserialized.path);
            _output.WriteLine($"Successfully deserialized: {deserialized}");
        }

        [Fact]
        public void Serialize_StringArray()
        {
            // Arrange
            var reflector = new Reflector();
            var stringArray = new[] { "alpha", "beta", "gamma" };

            // Act
            var serialized = reflector.Serialize(stringArray);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(string[]).GetTypeId(), serialized.typeName);
            _output.WriteLine($"Serialized string array: {serialized.ToJson(reflector)}");
        }

        [Fact]
        public void Deserialize_StringArray()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new[] { "one", "two", "three" };
            var serialized = reflector.Serialize(original);

            // Act
            var deserialized = reflector.Deserialize(serialized) as string[];

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Length, deserialized.Length);
            for (int i = 0; i < original.Length; i++)
            {
                Assert.Equal(original[i], deserialized[i]);
            }
            _output.WriteLine($"Successfully deserialized string array with {deserialized.Length} elements");
        }

        [Fact]
        public void Serialize_Null_Value()
        {
            // Arrange
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(null, typeof(GameObjectRef));

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(GameObjectRef).GetTypeId(), serialized.typeName);
            Assert.Null(serialized.valueJsonElement);
            _output.WriteLine($"Serialized null value: {serialized.ToJson(reflector)}");
        }

        [Fact]
        public void Deserialize_Null_Value()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a SerializedMember with null value
            var serialized = new SerializedMember
            {
                typeName = typeof(GameObjectRef).GetTypeId()!,
                valueJsonElement = null
            };

            // Act
            var deserialized = reflector.Deserialize(serialized);

            // Assert - For null values, we expect the deserialized object to be null
            Assert.Null(deserialized);
            _output.WriteLine($"Deserialized null value result: {deserialized}");
        }

        [Fact]
        public void Serialize_GameObjectRefList()
        {
            // Arrange
            var reflector = new Reflector();
            var gameObjectList = new GameObjectRefList
            {
                new GameObjectRef { instanceID = 1, name = "Object1" },
                new GameObjectRef { instanceID = 2, name = "Object2" },
                new GameObjectRef { instanceID = 3, name = "Object3" }
            };

            // Act
            var serialized = reflector.Serialize(gameObjectList);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(GameObjectRefList).GetTypeId(), serialized.typeName);
            _output.WriteLine($"Serialized GameObjectRefList: {serialized.ToJson(reflector)}");
        }

        [Fact]
        public void Deserialize_GameObjectRefList()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new GameObjectRefList
            {
                new GameObjectRef { instanceID = 10, name = "OriginalObject1" },
                new GameObjectRef { instanceID = 20, name = "OriginalObject2" }
            };

            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized data: {serialized.ToJson(reflector)}");

            // Act
            var deserialized = reflector.Deserialize<GameObjectRefList>(serialized);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Count, deserialized.Count);
            for (int i = 0; i < original.Count; i++)
            {
                _output.WriteLine($"Original[{i}]: instanceID={original[i].instanceID}, name={original[i].name}");
                _output.WriteLine($"Deserialized[{i}]: instanceID={deserialized[i].instanceID}, name={deserialized[i].name}");
                Assert.Equal(original[i].instanceID, deserialized[i].instanceID);
                Assert.Equal(original[i].name, deserialized[i].name);
            }
            _output.WriteLine($"Successfully deserialized GameObjectRefList with {deserialized.Count} items");
        }

        [Fact]
        public void Serialize_EmptyArray()
        {
            // Arrange
            var reflector = new Reflector();
            var emptyArray = new string[0];

            // Act
            var serialized = reflector.Serialize(emptyArray);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(string[]).GetTypeId(), serialized.typeName);
            _output.WriteLine($"Serialized empty array: {serialized.ToJson(reflector)}");
        }

        [Fact]
        public void Deserialize_EmptyArray()
        {
            // Arrange
            var reflector = new Reflector();
            var emptyArray = new string[0];
            var serialized = reflector.Serialize(emptyArray);

            // Act
            var deserialized = reflector.Deserialize(serialized) as string[];

            // Assert
            Assert.NotNull(deserialized);
            Assert.Empty(deserialized);
            _output.WriteLine("Successfully deserialized empty array");
        }

        [Fact]
        public void Serialization_BindingFlags_Control()
        {
            // Arrange
            var reflector = new Reflector();
            var testObject = new TestClass();

            // Act - Serialize with different BindingFlags
            var publicOnlySerialized = reflector.Serialize(testObject,
                flags: BindingFlags.Public | BindingFlags.Instance);
            var allMembersSerialized = reflector.Serialize(testObject,
                flags: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(publicOnlySerialized);
            Assert.NotNull(allMembersSerialized);
            Assert.Equal(typeof(TestClass).GetTypeId(), publicOnlySerialized.typeName);
            Assert.Equal(typeof(TestClass).GetTypeId(), allMembersSerialized.typeName);

            _output.WriteLine("BindingFlags serialization control test passed");
        }

        [Fact]
        public void SerializedMember_Name_Property_Handling()
        {
            // Arrange
            var reflector = new Reflector();
            var testObject = new GameObjectRef { instanceID = 999, name = "NamedObject" };

            // Act
            var serializedWithName = reflector.Serialize(testObject, name: "customName");
            var serializedWithoutName = reflector.Serialize(testObject);

            // Assert
            Assert.NotNull(serializedWithName);
            Assert.NotNull(serializedWithoutName);
            Assert.Equal("customName", serializedWithName.name);
            Assert.Null(serializedWithoutName.name);

            _output.WriteLine($"Named serialization: {serializedWithName.name}");
            _output.WriteLine($"Unnamed serialization: {serializedWithoutName.name.ValueOrNull()}");
        }

        [Fact]
        public void Complex_Nested_Type_Serialization()
        {
            // Arrange
            var reflector = new Reflector();
            var nestedObject = new GameObjectRefList
            {
                new GameObjectRef { instanceID = 1, name = "Nested1" },
                new GameObjectRef { instanceID = 2, name = "Nested2", path = "/Root/Nested2" }
            };

            // Act
            var serialized = reflector.Serialize(nestedObject, recursive: true);
            var deserialized = reflector.Deserialize(serialized) as GameObjectRefList;

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(nestedObject.Count, deserialized.Count);

            for (int i = 0; i < nestedObject.Count; i++)
            {
                Assert.Equal(nestedObject[i].instanceID, deserialized[i].instanceID);
                Assert.Equal(nestedObject[i].name, deserialized[i].name);
                Assert.Equal(nestedObject[i].path, deserialized[i].path);
            }

            _output.WriteLine($"Nested type serialization test passed with {deserialized.Count} items");
        }

        [Fact]
        public void Edge_Case_Type_Handling()
        {
            // Arrange
            var reflector = new Reflector();

            // Test nullable types
            int? nullableInt = 42;
            var nullableIntSerialized = reflector.Serialize(nullableInt);
            var nullableIntDeserialized = reflector.Deserialize(nullableIntSerialized);

            // Test empty collections
            var emptyList = new GameObjectRefList();
            var emptyListSerialized = reflector.Serialize(emptyList);
            var emptyListDeserialized = reflector.Deserialize(emptyListSerialized);

            // Assert
            Assert.NotNull(nullableIntSerialized);
            Assert.NotNull(nullableIntDeserialized);
            Assert.NotNull(emptyListSerialized);
            Assert.NotNull(emptyListDeserialized);

            _output.WriteLine("Edge case type handling test passed");
        }
    }
}
