using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet;
using ReflectorNet.Tests.Schema.Model;
using Xunit.Abstractions;
using System;
using System.Text.Json;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Utils;

namespace ReflectorNet.Tests.SchemaTests
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
            Assert.Equal(typeof(GameObjectRef).GetTypeName(pretty: false), serialized.typeName);
            Assert.NotNull(serialized.valueJsonElement);
            _output.WriteLine($"Serialized GameObjectRef: {JsonUtils.Serialize(serialized)}");
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

            var serialized = reflector.Serialize(original);

            // Act
            var deserialized = reflector.Deserialize(serialized) as GameObjectRef;

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
            Assert.Equal(typeof(string[]).GetTypeName(pretty: false), serialized.typeName);
            _output.WriteLine($"Serialized string array: {JsonUtils.Serialize(serialized)}");
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
            Assert.Equal(typeof(GameObjectRef).GetTypeName(pretty: false), serialized.typeName);
            Assert.Null(serialized.valueJsonElement);
            _output.WriteLine($"Serialized null value: {JsonUtils.Serialize(serialized)}");
        }

        [Fact]
        public void Deserialize_Null_Value()
        {
            // Arrange
            var reflector = new Reflector();

            // Create a SerializedMember with null value
            var serialized = new com.IvanMurzak.ReflectorNet.Model.SerializedMember
            {
                typeName = typeof(GameObjectRef).GetTypeName(pretty: false)!,
                valueJsonElement = null
            };

            // Act
            var deserialized = reflector.Deserialize(serialized);

            // Assert - For reference types with null value, should return default instance
            // The actual behavior might be to return a default instance rather than null
            Assert.NotNull(deserialized);
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
            Assert.Equal(typeof(GameObjectRefList).GetTypeName(pretty: false), serialized.typeName);
            _output.WriteLine($"Serialized GameObjectRefList: {JsonUtils.Serialize(serialized)}");
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
            _output.WriteLine($"Serialized data: {JsonUtils.Serialize(serialized)}");

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
            Assert.Equal(typeof(string[]).GetTypeName(pretty: false), serialized.typeName);
            _output.WriteLine($"Serialized empty array: {JsonUtils.Serialize(serialized)}");
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
            Assert.Equal(typeof(TestClass).GetTypeName(pretty: false), publicOnlySerialized.typeName);
            Assert.Equal(typeof(TestClass).GetTypeName(pretty: false), allMembersSerialized.typeName);

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
