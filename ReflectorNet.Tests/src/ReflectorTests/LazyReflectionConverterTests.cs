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
            var converter = new LazyReflectionConverter(typeName);

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
            var converter = new LazyReflectionConverter(typeName);

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
            var converter = new LazyReflectionConverter(typeName);

            // Act
            var priority = converter.SerializationPriority(typeof(DerivedTestTarget));

            // Assert
            Assert.True(priority > 0, "Priority should be positive for derived type");
            Assert.True(priority < 10001, "Priority for derived type should be less than exact match (MAX_DEPTH + 1)");
            _output.WriteLine($"Priority for derived type: {priority}");
        }

        [Fact]
        public void Serialize_IgnoresConfiguredProperties()
        {
            // Arrange
            var typeName = typeof(TestTarget).FullName!;
            var ignoredProps = new[] { "Secret" };
            var converter = new LazyReflectionConverter(typeName, ignoredProperties: ignoredProps);
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
            var converter = new LazyReflectionConverter(typeName, ignoredFields: ignoredFields);
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
            Assert.Throws<ArgumentException>(() => new LazyReflectionConverter(null!));
            Assert.Throws<ArgumentException>(() => new LazyReflectionConverter(""));
            Assert.Throws<ArgumentException>(() => new LazyReflectionConverter("   "));
        }

        [Fact]
        public void Constructor_BackingConverterWithIgnoredMembers_ThrowsArgumentException()
        {
            var typeName = typeof(TestTarget).FullName!;
            var mockConverter = new MockConverter();

            // BackingConverter with ignoredProperties should throw
            Assert.Throws<ArgumentException>(() => new LazyReflectionConverter(
                typeName,
                ignoredProperties: new[] { "Secret" },
                backingConverter: mockConverter));

            // BackingConverter with ignoredFields should throw
            Assert.Throws<ArgumentException>(() => new LazyReflectionConverter(
                typeName,
                ignoredFields: new[] { "Secret" },
                backingConverter: mockConverter));

            // BackingConverter with both should throw
            Assert.Throws<ArgumentException>(() => new LazyReflectionConverter(
                typeName,
                ignoredProperties: new[] { "Secret" },
                ignoredFields: new[] { "Field" },
                backingConverter: mockConverter));

            // Empty arrays should NOT throw (they're functionally equivalent to null)
            var converter1 = new LazyReflectionConverter(
                typeName,
                ignoredProperties: Array.Empty<string>(),
                backingConverter: mockConverter);
            Assert.NotNull(converter1);

            var converter2 = new LazyReflectionConverter(
                typeName,
                ignoredFields: Array.Empty<string>(),
                backingConverter: mockConverter);
            Assert.NotNull(converter2);
        }

        [Fact]
        public void Serialize_DelegatesToBackingConverter()
        {
            // Arrange
            var typeName = typeof(TestTarget).FullName!;
            var mockConverter = new MockConverter();
            var converter = new LazyReflectionConverter(typeName, backingConverter: mockConverter);
            var reflector = new Reflector();
            reflector.Converters.Add(converter);

            var obj = new TestTarget { Name = "Delegated" };

            // Act
            var serialized = reflector.Serialize(obj);

            // Assert
            Assert.True(mockConverter.WasCalled);
            Assert.Equal("{}", serialized.valueJsonElement?.ToString());
        }

        class MockConverter : GenericReflectionConverter<TestTarget>
        {
            public bool WasCalled { get; private set; }

            protected override SerializedMember InternalSerialize(
                Reflector reflector,
                object? obj,
                Type type,
                string? name = null,
                bool recursive = true,
                System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                int depth = 0,
                Logs? logs = null,
                ILogger? logger = null,
                SerializationContext? context = null)
            {
                WasCalled = true;
                return SerializedMember.FromJson(type, "{}", name);
            }
        }
    }
}
