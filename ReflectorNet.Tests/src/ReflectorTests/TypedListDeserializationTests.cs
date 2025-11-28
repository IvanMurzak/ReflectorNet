using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.ReflectorNet.Model;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    /// <summary>
    /// Tests to verify that deserialized lists maintain proper generic type information
    /// and are not incorrectly deserialized as List&lt;object?&gt;
    /// </summary>
    public class TypedListDeserializationTests : BaseTest
    {
        public TypedListDeserializationTests(ITestOutputHelper output) : base(output) { }

        public class ClassWithListProperty
        {
            public List<int>? Numbers { get; set; }
            public List<string>? Names { get; set; }
        }

        [Fact]
        public void List_Int_ShouldDeserializeAsTypedList()
        {
            // Arrange
            var original = new List<int> { 1, 2, 3, 4, 5 };
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(original);
            var deserialized = reflector.Deserialize(serialized);

            // Assert
            Assert.NotNull(deserialized);

            // Critical: The deserialized object should be List<int>, not List<object?>
            Assert.IsType<List<int>>(deserialized);

            var typedList = (List<int>)deserialized;
            Assert.Equal(original.Count, typedList.Count);
            Assert.Equal(original[0], typedList[0]);
            Assert.Equal(original[4], typedList[4]);
        }

        [Fact]
        public void List_String_ShouldDeserializeAsTypedList()
        {
            // Arrange
            var original = new List<string> { "hello", "world", "test" };
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(original);
            var deserialized = reflector.Deserialize(serialized);

            // Assert
            Assert.NotNull(deserialized);

            // Critical: The deserialized object should be List<string>, not List<object?>
            Assert.IsType<List<string>>(deserialized);

            var typedList = (List<string>)deserialized;
            Assert.Equal(original.Count, typedList.Count);
            Assert.Equal(original[0], typedList[0]);
            Assert.Equal(original[2], typedList[2]);
        }

        [Fact]
        public void List_Double_ShouldDeserializeAsTypedList()
        {
            // Arrange
            var original = new List<double> { 1.5, 2.7, 3.14159 };
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(original);
            var deserialized = reflector.Deserialize(serialized);

            // Assert
            Assert.NotNull(deserialized);

            // Critical: The deserialized object should be List<double>, not List<object?>
            Assert.IsType<List<double>>(deserialized);

            var typedList = (List<double>)deserialized;
            Assert.Equal(original.Count, typedList.Count);
            Assert.Equal(original[0], typedList[0]);
            Assert.Equal(original[2], typedList[2]);
        }

        public class TestPerson
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        [Fact]
        public void List_ComplexType_ShouldDeserializeAsTypedList()
        {
            // Arrange
            var original = new List<TestPerson>
            {
                new TestPerson { Name = "Alice", Age = 30 },
                new TestPerson { Name = "Bob", Age = 25 }
            };
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(original);
            var deserialized = reflector.Deserialize(serialized);

            // Assert
            Assert.NotNull(deserialized);

            // Critical: The deserialized object should be List<TestPerson>, not List<object?>
            Assert.IsType<List<TestPerson>>(deserialized);

            var typedList = (List<TestPerson>)deserialized;
            Assert.Equal(original.Count, typedList.Count);
            Assert.Equal(original[0].Name, typedList[0].Name);
            Assert.Equal(original[1].Age, typedList[1].Age);
        }

        [Fact]
        public void List_GenericType_ShouldMaintainTypeInformation()
        {
            // Arrange
            var original = new List<int> { 10, 20, 30 };
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(original);
            var deserialized = reflector.Deserialize(serialized);

            // Assert
            Assert.NotNull(deserialized);

            // Verify the runtime type has the correct generic argument
            var deserializedType = deserialized.GetType();
            Assert.True(deserializedType.IsGenericType);
            Assert.Equal(typeof(List<>), deserializedType.GetGenericTypeDefinition());

            var genericArgs = deserializedType.GetGenericArguments();
            Assert.Single(genericArgs);
            Assert.Equal(typeof(int), genericArgs[0]);
        }

        [Fact]
        public void NestedList_ShouldDeserializeAsTypedList()
        {
            // Arrange
            var original = new List<List<int>>
            {
                new List<int> { 1, 2, 3 },
                new List<int> { 4, 5, 6 }
            };
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(original);
            var deserialized = reflector.Deserialize(serialized);

            // Assert
            Assert.NotNull(deserialized);

            // Critical: The deserialized object should be List<List<int>>, not List<object?>
            Assert.IsType<List<List<int>>>(deserialized);

            var typedList = (List<List<int>>)deserialized;
            Assert.Equal(original.Count, typedList.Count);
            Assert.Equal(original[0][0], typedList[0][0]);
            Assert.Equal(original[1][2], typedList[1][2]);
        }

        [Fact]
        public void ClassWithListProperty_ShouldDeserializeListPropertyAsTypedList()
        {
            // Arrange
            var original = new ClassWithListProperty
            {
                Numbers = new List<int> { 10, 20, 30 },
                Names = new List<string> { "Alice", "Bob", "Charlie" }
            };
            var reflector = new Reflector();

            // Act
            var serialized = reflector.Serialize(original);
            _output.WriteLine($"Serialized: {serialized.ToJson(reflector)}");

            var deserialized = reflector.Deserialize<ClassWithListProperty>(serialized);

            // Assert
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Numbers);
            Assert.NotNull(deserialized.Names);

            // Critical: The deserialized properties should be List<int> and List<string>, not List<object?>
            Assert.IsType<List<int>>(deserialized.Numbers);
            Assert.IsType<List<string>>(deserialized.Names);

            Assert.Equal(original.Numbers.Count, deserialized.Numbers.Count);
            Assert.Equal(original.Names.Count, deserialized.Names.Count);
            Assert.Equal(original.Numbers[0], deserialized.Numbers[0]);
            Assert.Equal(original.Names[0], deserialized.Names[0]);
        }

        [Fact]
        public void DirectListDeserializationViaSerializedMember_ShouldBeTyped()
        {
            // This test directly exercises TryDeserializeValueListInternal
            // by creating a SerializedMember with a JSON array

            var reflector = new Reflector();

            // Create a serialized member representing a List<int> with JSON array
            var jsonArray = System.Text.Json.JsonSerializer.SerializeToElement(new[] { 1, 2, 3, 4, 5 });
            var serializedMember = new SerializedMember
            {
                typeName = "System.Collections.Generic.List`1[[System.Int32]]",
                valueJsonElement = jsonArray
            };

            // Act
            var deserialized = reflector.Deserialize(serializedMember);

            // Assert
            Assert.NotNull(deserialized);
            _output.WriteLine($"Deserialized type: {deserialized.GetType().FullName}");

            // Critical: Should be List<int>, not List<object?>
            Assert.IsType<List<int>>(deserialized);

            var typedList = (List<int>)deserialized;
            Assert.Equal(5, typedList.Count);
            Assert.Equal(1, typedList[0]);
            Assert.Equal(5, typedList[4]);
        }
    }
}
