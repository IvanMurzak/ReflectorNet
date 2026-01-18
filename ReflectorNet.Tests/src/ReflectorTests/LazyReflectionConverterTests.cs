using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Converter;
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

            // Register manually to ensure it's used (though since it has high priority it should be picked up if added to registry)
            // But here we can use the converter directly to test its internal logic via helper or just check what properties it returns

            // Let's use it via Reflector to simulate full integration
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
        public void Constructor_NullOrEmptyTypeName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LazyReflectionConverter(null!));
            Assert.Throws<ArgumentException>(() => new LazyReflectionConverter(""));
            Assert.Throws<ArgumentException>(() => new LazyReflectionConverter("   "));
        }
    }
}
