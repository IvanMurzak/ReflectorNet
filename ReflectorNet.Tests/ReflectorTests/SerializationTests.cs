using System.Linq;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.ReflectorNet.Tests.Utils.Model;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class SerializationTests : BaseTest
    {
        public SerializationTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Serialize_ParentClass_NestedClass_Array_Instance()
        {
            // Arrange
            var reflector = new Reflector();

            var sourceInstance = new WrapperClass<ParentClass.NestedClass[]>
            {
                ValueField = new[]
                {
                //     new ParentClass.NestedClass { NestedField = "First Field", NestedProperty = "First Property" },
                    new ParentClass.NestedClass { NestedField = "Second Field", NestedProperty = "Second Property" }
                }
                // ValueProperty = new[]
                //  {
                // // //     new ParentClass.NestedClass { NestedField = "Third Field", NestedProperty = "Third Property" },
                //      new ParentClass.NestedClass { NestedField = "Fourth Field", NestedProperty = "Fourth Property" }
                //  }
            };

            _output.WriteLine($"Source WrapperClass<ParentClass.NestedClass[]>: {JsonUtils.ToJson(sourceInstance)}");
            _output.WriteLine("------------------------------------------------------");

            // Act
            var stringBuilder = new StringBuilder();
            var serialized = reflector.Serialize(sourceInstance, name: nameof(sourceInstance), stringBuilder: stringBuilder);
            _output.WriteLine($"Serialized WrapperClass<ParentClass.NestedClass[]>: {JsonUtils.ToJson(serialized)}");
            _output.WriteLine(stringBuilder.ToString());

            stringBuilder.Clear();

            var stringBuilderLogger = new StringBuilderLogger("T");
            var deserializedInstance = reflector.Deserialize<WrapperClass<ParentClass.NestedClass[]>>(serialized, stringBuilder: stringBuilder, logger: stringBuilderLogger);
            _output.WriteLine($"Deserialized WrapperClass<ParentClass.NestedClass[]>: {JsonUtils.ToJson(deserializedInstance)}");
            _output.WriteLine(stringBuilderLogger.ToString());

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.valueJsonElement);

            Assert.NotNull(deserializedInstance);
            Assert.Equal(JsonUtils.ToJson(sourceInstance), JsonUtils.ToJson(deserializedInstance));

            // Assert.Equal(wrapperInstance.ValueField!.Length, deserializedInstance.ValueField!.Length);
            // Assert.Equal(wrapperInstance.ValueProperty!.Length, deserializedInstance.ValueProperty!.Length);
            // Assert.Equal(typeof(WrapperClass<ParentClass.NestedClass[]>).GetTypeName(pretty: false), serialized.typeName);

            // Assert.Equal(wrapperInstance.ValueField![0].NestedField, deserializedInstance.ValueField![0].NestedField);
            // Assert.Equal(wrapperInstance.ValueField![0].NestedProperty, deserializedInstance.ValueField![0].NestedProperty);
            // Assert.Equal(wrapperInstance.ValueProperty![0].NestedField, deserializedInstance.ValueProperty![0].NestedField);
            // Assert.Equal(wrapperInstance.ValueProperty![0].NestedProperty, deserializedInstance.ValueProperty![0].NestedProperty);

            // Assert.Equal(wrapperInstance.ValueField![1].NestedField, deserializedInstance.ValueField![1].NestedField);
            // Assert.Equal(wrapperInstance.ValueField![1].NestedProperty, deserializedInstance.ValueField![1].NestedProperty);
            // Assert.Equal(wrapperInstance.ValueProperty![1].NestedField, deserializedInstance.ValueProperty![1].NestedField);
            // Assert.Equal(wrapperInstance.ValueProperty![1].NestedProperty, deserializedInstance.ValueProperty![1].NestedProperty);

            Assert.Equal(typeof(WrapperClass<ParentClass.NestedClass[]>).GetTypeName(pretty: false), serialized.typeName);

            Assert.Equal(JsonUtils.ToJson(sourceInstance), JsonUtils.ToJson(deserializedInstance));

            // Assert.NotNull(wrapperField_Fields_0.valueJsonElement);
            // Assert.Equal("First Field", wrapperField_Fields_0.GetProperty(nameof(ParentClass.NestedClass.NestedField)).GetString());
            // Assert.Equal("First Property", wrapperField_Fields_0.GetProperty(nameof(ParentClass.NestedClass.NestedProperty)).GetString());

            // ------------------------------------------------------

            // Assert.NotNull(serialized.props);
            // Assert.NotNull(serialized.props.FirstOrDefault(p => p.name == nameof(WrapperClass<object>.ValueProperty)));

            // Assert.NotNull(serialized.fields);
            // Assert.NotNull(serialized.fields.FirstOrDefault(p => p.name == nameof(ParentClass.NestedClass.NestedField)));
            // Assert.NotNull(serialized.props);
            // Assert.NotNull(serialized.props.FirstOrDefault(p => p.name == nameof(ParentClass.NestedClass.NestedProperty)));
        }

        [Fact]
        public void Serialize_ParentClass_NestedClass_Instance()
        {
            // Arrange
            var reflector = new Reflector();
            var nestedInstance = new ParentClass.NestedClass
            {
                NestedField = "Modified field value",
                NestedProperty = "Modified property value"
            };

            // Act
            var serialized = reflector.Serialize(nestedInstance);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(ParentClass.NestedClass).GetTypeName(pretty: false), serialized.typeName);
            Assert.NotNull(serialized.valueJsonElement);
            _output.WriteLine($"Serialized ParentClass.NestedClass: {JsonUtils.ToJson(serialized)}");
        }

        [Fact]
        public void Deserialize_ParentClass_NestedClass_Instance()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new ParentClass.NestedClass
            {
                NestedField = "Original field",
                NestedProperty = "Original property"
            };

            var serialized = reflector.Serialize(original);

            // Act
            var deserialized = reflector.Deserialize(serialized) as ParentClass.NestedClass;

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.NestedField, deserialized.NestedField);
            Assert.Equal(original.NestedProperty, deserialized.NestedProperty);
            _output.WriteLine($"Successfully deserialized ParentClass.NestedClass: Field='{deserialized.NestedField}', Property='{deserialized.NestedProperty}'");
        }

        [Fact]
        public void Serialize_StaticParentClass_NestedClass_Instance()
        {
            // Arrange
            var reflector = new Reflector();
            var nestedInstance = new StaticParentClass.NestedClass
            {
                NestedField = "Static parent nested field",
                NestedProperty = "Static parent nested property"
            };

            // Act
            var serialized = reflector.Serialize(nestedInstance);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(StaticParentClass.NestedClass).GetTypeName(pretty: false), serialized.typeName);
            Assert.NotNull(serialized.valueJsonElement);
            _output.WriteLine($"Serialized StaticParentClass.NestedClass: {JsonUtils.ToJson(serialized)}");
        }

        [Fact]
        public void Deserialize_StaticParentClass_NestedClass_Instance()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new StaticParentClass.NestedClass
            {
                NestedField = "Test field value",
                NestedProperty = "Test property value"
            };

            var serialized = reflector.Serialize(original);

            // Act
            var deserialized = reflector.Deserialize(serialized) as StaticParentClass.NestedClass;

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.NestedField, deserialized.NestedField);
            Assert.Equal(original.NestedProperty, deserialized.NestedProperty);
            _output.WriteLine($"Successfully deserialized StaticParentClass.NestedClass: Field='{deserialized.NestedField}', Property='{deserialized.NestedProperty}'");
        }

        [Fact]
        public void Serialize_Deserialize_RoundTrip_ParentClass_NestedClass()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new ParentClass.NestedClass
            {
                NestedField = "Round trip field test",
                NestedProperty = "Round trip property test"
            };

            // Act - Serialize and then deserialize
            var serialized = reflector.Serialize(original);
            var deserialized = reflector.Deserialize(serialized) as ParentClass.NestedClass;

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.NestedField, deserialized.NestedField);
            Assert.Equal(original.NestedProperty, deserialized.NestedProperty);

            // Verify they are not the same reference but have equal values
            Assert.False(ReferenceEquals(original, deserialized));
            Assert.Equal(original.NestedField, deserialized.NestedField);
            Assert.Equal(original.NestedProperty, deserialized.NestedProperty);

            _output.WriteLine($"Round trip test successful for ParentClass.NestedClass");
            _output.WriteLine($"Original: Field='{original.NestedField}', Property='{original.NestedProperty}'");
            _output.WriteLine($"Deserialized: Field='{deserialized.NestedField}', Property='{deserialized.NestedProperty}'");
        }

        [Fact]
        public void Serialize_Deserialize_RoundTrip_StaticParentClass_NestedClass()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new StaticParentClass.NestedClass
            {
                NestedField = "Static round trip field",
                NestedProperty = "Static round trip property"
            };

            // Act - Serialize and then deserialize
            var serialized = reflector.Serialize(original);
            var deserialized = reflector.Deserialize(serialized) as StaticParentClass.NestedClass;

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.NestedField, deserialized.NestedField);
            Assert.Equal(original.NestedProperty, deserialized.NestedProperty);

            // Verify they are not the same reference but have equal values
            Assert.False(ReferenceEquals(original, deserialized));
            Assert.Equal(original.NestedField, deserialized.NestedField);
            Assert.Equal(original.NestedProperty, deserialized.NestedProperty);

            _output.WriteLine($"Round trip test successful for StaticParentClass.NestedClass");
            _output.WriteLine($"Original: Field='{original.NestedField}', Property='{original.NestedProperty}'");
            _output.WriteLine($"Deserialized: Field='{deserialized.NestedField}', Property='{deserialized.NestedProperty}'");
        }

        [Fact]
        public void Serialize_NestedClass_WithDefaultValues()
        {
            // Arrange
            var reflector = new Reflector();
            var nestedWithDefaults = new ParentClass.NestedClass(); // Uses default values

            // Act
            var serialized = reflector.Serialize(nestedWithDefaults);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(ParentClass.NestedClass).GetTypeName(pretty: false), serialized.typeName);
            _output.WriteLine($"Serialized nested class with defaults: {JsonUtils.ToJson(serialized)}");
        }

        [Fact]
        public void Deserialize_NestedClass_WithDefaultValues()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new ParentClass.NestedClass(); // Default values: "I am field", "I am property"
            var serialized = reflector.Serialize(original);

            // Act
            var deserialized = reflector.Deserialize(serialized) as ParentClass.NestedClass;

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal("I am field", deserialized.NestedField);
            Assert.Equal("I am property", deserialized.NestedProperty);
            _output.WriteLine($"Deserialized with defaults: Field='{deserialized.NestedField}', Property='{deserialized.NestedProperty}'");
        }

        [Fact]
        public void Serialize_NestedClass_WithNullValues()
        {
            // Arrange
            var reflector = new Reflector();
            var nestedWithNulls = new ParentClass.NestedClass
            {
                NestedField = string.Empty,
                NestedProperty = string.Empty
            };

            // Act
            var serialized = reflector.Serialize(nestedWithNulls);

            // Assert
            Assert.NotNull(serialized);
            Assert.Equal(typeof(ParentClass.NestedClass).GetTypeName(pretty: false), serialized.typeName);
            _output.WriteLine($"Serialized nested class with empty strings: {JsonUtils.ToJson(serialized)}");
        }

        [Fact]
        public void Deserialize_NestedClass_WithEmptyValues()
        {
            // Arrange
            var reflector = new Reflector();
            var original = new ParentClass.NestedClass
            {
                NestedField = string.Empty,
                NestedProperty = string.Empty
            };
            var serialized = reflector.Serialize(original);

            // Act
            var deserialized = reflector.Deserialize(serialized) as ParentClass.NestedClass;

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(string.Empty, deserialized.NestedField);
            Assert.Equal(string.Empty, deserialized.NestedProperty);
            _output.WriteLine($"Successfully deserialized nested class with empty string values");
        }

        [Fact]
        public void Serialize_Static_Members_From_ParentClass_NestedClass()
        {
            // Arrange
            var reflector = new Reflector();

            // Get the static field and property values from ParentClass.NestedClass
            var staticFieldValue = ParentClass.NestedClass.NestedStaticField;
            var staticPropertyValue = ParentClass.NestedClass.NestedStaticProperty;

            // Act - Test serialization of static member values
            var serializedField = reflector.Serialize(staticFieldValue, name: "NestedStaticField");
            var serializedProperty = reflector.Serialize(staticPropertyValue, name: "NestedStaticProperty");

            // Assert
            Assert.NotNull(serializedField);
            Assert.NotNull(serializedProperty);
            Assert.Equal("NestedStaticField", serializedField.name);
            Assert.Equal("NestedStaticProperty", serializedProperty.name);
            Assert.Equal(typeof(string).GetTypeName(pretty: false), serializedField.typeName);
            Assert.Equal(typeof(string).GetTypeName(pretty: false), serializedProperty.typeName);

            _output.WriteLine($"Serialized static field: {JsonUtils.ToJson(serializedField)}");
            _output.WriteLine($"Serialized static property: {JsonUtils.ToJson(serializedProperty)}");
        }

        [Fact]
        public void Serialize_Static_Members_From_ParentClass_NestedStaticClass()
        {
            // Arrange
            var reflector = new Reflector();

            // Get the static field and property values from ParentClass.NestedStaticClass
            var staticFieldValue = ParentClass.NestedStaticClass.NestedStaticField;
            var staticPropertyValue = ParentClass.NestedStaticClass.NestedStaticProperty;

            // Act - Test serialization of static member values
            var serializedField = reflector.Serialize(staticFieldValue, name: "NestedStaticField");
            var serializedProperty = reflector.Serialize(staticPropertyValue, name: "NestedStaticProperty");

            // Assert
            Assert.NotNull(serializedField);
            Assert.NotNull(serializedProperty);
            Assert.Equal("NestedStaticField", serializedField.name);
            Assert.Equal("NestedStaticProperty", serializedProperty.name);
            Assert.Equal(typeof(string).GetTypeName(pretty: false), serializedField.typeName);
            Assert.Equal(typeof(string).GetTypeName(pretty: false), serializedProperty.typeName);

            _output.WriteLine($"Serialized ParentClass.NestedStaticClass static field: {JsonUtils.ToJson(serializedField)}");
            _output.WriteLine($"Serialized ParentClass.NestedStaticClass static property: {JsonUtils.ToJson(serializedProperty)}");
        }

        [Fact]
        public void Serialize_Static_Members_From_StaticParentClass_NestedClass()
        {
            // Arrange
            var reflector = new Reflector();

            // Get the static field and property values from StaticParentClass.NestedClass
            var staticFieldValue = StaticParentClass.NestedClass.NestedStaticField;
            var staticPropertyValue = StaticParentClass.NestedClass.NestedStaticProperty;

            // Act - Test serialization of static member values
            var serializedField = reflector.Serialize(staticFieldValue, name: "NestedStaticField");
            var serializedProperty = reflector.Serialize(staticPropertyValue, name: "NestedStaticProperty");

            // Assert
            Assert.NotNull(serializedField);
            Assert.NotNull(serializedProperty);
            Assert.Equal("NestedStaticField", serializedField.name);
            Assert.Equal("NestedStaticProperty", serializedProperty.name);
            Assert.Equal(typeof(string).GetTypeName(pretty: false), serializedField.typeName);
            Assert.Equal(typeof(string).GetTypeName(pretty: false), serializedProperty.typeName);

            _output.WriteLine($"Serialized StaticParentClass.NestedClass static field: {JsonUtils.ToJson(serializedField)}");
            _output.WriteLine($"Serialized StaticParentClass.NestedClass static property: {JsonUtils.ToJson(serializedProperty)}");
        }

        [Fact]
        public void Serialize_Static_Members_From_StaticParentClass_NestedStaticClass()
        {
            // Arrange
            var reflector = new Reflector();

            // Get the static field and property values from StaticParentClass.NestedStaticClass
            var staticFieldValue = StaticParentClass.NestedStaticClass.NestedStaticField;
            var staticPropertyValue = StaticParentClass.NestedStaticClass.NestedStaticProperty;

            // Act - Test serialization of static member values
            var serializedField = reflector.Serialize(staticFieldValue, name: "NestedStaticField");
            var serializedProperty = reflector.Serialize(staticPropertyValue, name: "NestedStaticProperty");

            // Assert
            Assert.NotNull(serializedField);
            Assert.NotNull(serializedProperty);
            Assert.Equal("NestedStaticField", serializedField.name);
            Assert.Equal("NestedStaticProperty", serializedProperty.name);
            Assert.Equal(typeof(string).GetTypeName(pretty: false), serializedField.typeName);
            Assert.Equal(typeof(string).GetTypeName(pretty: false), serializedProperty.typeName);

            _output.WriteLine($"Serialized StaticParentClass.NestedStaticClass static field: {JsonUtils.ToJson(serializedField)}");
            _output.WriteLine($"Serialized StaticParentClass.NestedStaticClass static property: {JsonUtils.ToJson(serializedProperty)}");
        }

        [Fact]
        public void Serialize_Deserialize_Collection_Of_NestedClasses()
        {
            // Arrange
            var reflector = new Reflector();
            var nestedClassList = new ParentClass.NestedClass[]
            {
                new ParentClass.NestedClass { NestedField = "First", NestedProperty = "First Property" },
                new ParentClass.NestedClass { NestedField = "Second", NestedProperty = "Second Property" },
                new ParentClass.NestedClass { NestedField = "Third", NestedProperty = "Third Property" }
            };

            // Act - Serialize and deserialize collection
            var serialized = reflector.Serialize(nestedClassList, name: "NestedClassCollection");
            var deserialized = reflector.Deserialize(serialized) as ParentClass.NestedClass[];

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(3, deserialized.Length);
            Assert.Equal("First", deserialized[0].NestedField);
            Assert.Equal("First Property", deserialized[0].NestedProperty);
            Assert.Equal("Second", deserialized[1].NestedField);
            Assert.Equal("Second Property", deserialized[1].NestedProperty);
            Assert.Equal("Third", deserialized[2].NestedField);
            Assert.Equal("Third Property", deserialized[2].NestedProperty);

            _output.WriteLine($"Successfully serialized and deserialized collection of {deserialized.Length} nested class instances");
            _output.WriteLine($"First item: Field='{deserialized[0].NestedField}', Property='{deserialized[0].NestedProperty}'");
            _output.WriteLine($"Second item: Field='{deserialized[1].NestedField}', Property='{deserialized[1].NestedProperty}'");
            _output.WriteLine($"Third item: Field='{deserialized[2].NestedField}', Property='{deserialized[2].NestedProperty}'");
        }
    }
}
