using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests
{
    public class LazyReflectionConverterTests : BaseTest
    {
        public LazyReflectionConverterTests(ITestOutputHelper output) : base(output) { }

        // Test class for "existing type" scenario
        public class TestTarget
        {
            public string Name { get; set; } = "Test";
            public int Value { get; set; } = 42;
            public string Secret { get; set; } = "Hidden";
        }

        public class DerivedTestTarget : TestTarget
        {
            public string Extra { get; set; } = "Extra";
        }

        // Test class with fields for "ignoredFields" scenario
        public class TestTargetWithFields
        {
            public string Name = "Test";
            public int Value = 42;
            public string SecretField = "Hidden";
        }

        [Fact]
        public void SerializationPriority_TypeExists_ReturnsHighPriority()
        {
            // Arrange
            var typeName = typeof(TestTarget).FullName!;
            var converter = new LazyGenericReflectionConverter(typeName);

            // Act
            var priority = converter.SerializationPriority(typeof(TestTarget));

            // Assert
            Assert.True(priority > 0, $"Priority should be positive for existing type {typeName}");
            _output.WriteLine($"Priority for {typeName}: {priority}");
        }

        [Fact]
        public void SerializationPriority_TypeDoesNotExist_ReturnsZero()
        {
            // Arrange
            var typeName = "System.NonExistentType.ShouldNotExist";
            var converter = new LazyGenericReflectionConverter(typeName);

            // Act
            var priority = converter.SerializationPriority(typeof(TestTarget));

            // Assert
            Assert.Equal(0, priority);
            _output.WriteLine($"Priority for non-existent type: {priority}");
        }

        [Fact]
        public void SerializationPriority_DerivedType_ReturnsPositivePriority()
        {
            // Arrange
            var typeName = typeof(TestTarget).FullName!;
            var converter = new LazyGenericReflectionConverter(typeName);

            // Act
            var exactMatchPriority = converter.SerializationPriority(typeof(TestTarget));
            var derivedPriority = converter.SerializationPriority(typeof(DerivedTestTarget));

            // Assert
            Assert.True(exactMatchPriority > 0, "Priority should be positive for exact type");
            Assert.True(derivedPriority > 0, "Priority should be positive for derived type");
            Assert.True(derivedPriority < exactMatchPriority, "Priority for derived type should be less than priority for exact type");
            _output.WriteLine($"Priority for exact type: {exactMatchPriority}, derived type: {derivedPriority}");
        }

        [Fact]
        public void Serialize_IgnoresConfiguredProperties()
        {
            // Arrange
            var typeName = typeof(TestTarget).FullName!;
            var ignoredProps = new[] { "Secret" };
            var converter = new LazyGenericReflectionConverter(typeName, ignoredProperties: ignoredProps);
            var reflector = new Reflector();

            // Register manually to ensure it's used
            reflector.Converters.Add(converter);

            var obj = new TestTarget();

            // Act
            var serialized = reflector.Serialize(obj);

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.props);

            // Check that "Name" and "Value" are present
            Assert.Contains(serialized.props, p => p.name == "Name");
            Assert.Contains(serialized.props, p => p.name == "Value");

            // Check that "Secret" is missing
            Assert.DoesNotContain(serialized.props, p => p.name == "Secret");
        }

        [Fact]
        public void Serialize_IgnoresConfiguredFields()
        {
            // Arrange
            var typeName = typeof(TestTargetWithFields).FullName!;
            var ignoredFields = new[] { "SecretField" };
            var converter = new LazyGenericReflectionConverter(typeName, ignoredFields: ignoredFields);
            var reflector = new Reflector();

            // Register manually to ensure it's used
            reflector.Converters.Add(converter);

            var obj = new TestTargetWithFields();

            // Act
            var serialized = reflector.Serialize(obj);

            // Assert
            Assert.NotNull(serialized);
            Assert.NotNull(serialized.fields);

            // Check that "Name" and "Value" fields are present
            Assert.Contains(serialized.fields, f => f.name == "Name");
            Assert.Contains(serialized.fields, f => f.name == "Value");

            // Check that "SecretField" is missing
            Assert.DoesNotContain(serialized.fields, f => f.name == "SecretField");
        }

        [Fact]
        public void Constructor_NullOrEmptyTypeName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LazyGenericReflectionConverter(null!));
            Assert.Throws<ArgumentException>(() => new LazyGenericReflectionConverter(""));
            Assert.Throws<ArgumentException>(() => new LazyGenericReflectionConverter("   "));
        }

        [Fact]
        public void Serialize_DelegatesToBackingConverter()
        {
            // Arrange
            var typeName = typeof(TestTarget).FullName!;
            var mockConverter = new MockConverter();
            var converter = new LazyGenericReflectionConverter(typeName, backingConverter: mockConverter);
            var reflector = new Reflector();
            reflector.Converters.Add(converter);

            var obj = new TestTarget { Name = "Delegated" };

            // Act
            var serialized = reflector.Serialize(obj);

            // Assert
            Assert.True(mockConverter.MetadataWasAccessed, "Backing converter metadata should be accessed.");

            // "Name" should be present as normal serialization happens using the metadata
            Assert.NotNull(serialized.props);
            Assert.Contains(serialized.props, p => p.name == "Name");
            // "Value" should also be present since we are using TestTarget
            Assert.Contains(serialized.props, p => p.name == "Value");
        }

        [Fact]
        public void Serialize_DelegatesAndFilters()
        {
            // Arrange
            var typeName = typeof(TestTarget).FullName!;
            var mockConverter = new MockConverter(); // Serializes everything normally because it returns all properties

            // Should ignore "Secret" even though delegated
            var converter = new LazyGenericReflectionConverter(
                typeName,
                backingConverter: mockConverter,
                ignoredProperties: new[] { "Secret" });

            var reflector = new Reflector();
            reflector.Converters.Add(converter);

            var obj = new TestTarget { Name = "Filtered", Secret = "ShouldBeGone" };

            // Act
            var serialized = reflector.Serialize(obj);

            // Assert
            Assert.True(mockConverter.MetadataWasAccessed, "Backing converter metadata should be accessed.");
            Assert.NotNull(serialized.props);

            // "Name" should remain
            Assert.Contains(serialized.props, p => p.name == "Name");

            // "Secret" should be removed by LazyReflectionConverter filtering logic
            Assert.DoesNotContain(serialized.props, p => p.name == "Secret");
        }

        [Fact]
        public void Constructor_BackingConverterWithIgnoredMembers_Succeeds()
        {
            // This verifies the fix: we no longer throw exception for this combination
            var typeName = typeof(TestTarget).FullName!;
            var mockConverter = new MockConverter();

            var converter = new LazyGenericReflectionConverter(
                typeName,
                ignoredProperties: new[] { "Test" },
                backingConverter: mockConverter);

            Assert.NotNull(converter);
        }

        class MockConverter : GenericReflectionConverter<TestTarget>
        {
            public bool MetadataWasAccessed { get; private set; }

            protected override IEnumerable<System.Reflection.PropertyInfo>? GetSerializablePropertiesInternal(
                Reflector reflector,
                Type objType,
                System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                ILogger? logger = null)
            {
                MetadataWasAccessed = true;
                return base.GetSerializablePropertiesInternal(reflector, objType, flags, logger);
            }
        }
    }
}
