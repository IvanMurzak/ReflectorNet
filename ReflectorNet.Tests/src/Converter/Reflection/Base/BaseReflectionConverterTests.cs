using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Converter.Reflection.Base
{
    public class BaseReflectionConverterTests : BaseTest
    {
        public BaseReflectionConverterTests(ITestOutputHelper output) : base(output)
        {
        }

        // Test entity to reflect upon
        private class TestEntity
        {
            public int PublicField;
            private int PrivateField;
            public int PublicProp { get; set; }
            private int PrivateProp { get; set; }
        }

        // Concrete implementation of the abstract base class
        private class TestableReflectionConverter : BaseReflectionConverter<TestEntity>
        {
            protected override SerializedMember? InternalSerialize(
                Reflector reflector,
                object? obj,
                Type type,
                string? name,
                bool recursive,
                BindingFlags flags,
                int depth,
                Logs? logs,
                ILogger? logger,
                SerializationContext? context)
            {
                return null;
            }

            protected override bool SetValue(
                Reflector reflector,
                ref object? instance,
                Type type,
                JsonElement? element,
                int depth,
                Logs? logs,
                ILogger? logger)
            {
                return true;
            }

            public override bool SetField(
                Reflector reflector,
                ref object? instance,
                Type type,
                FieldInfo field,
                SerializedMember? member,
                int depth,
                Logs? logs,
                BindingFlags flags,
                ILogger? logger)
            {
                return true;
            }

            public override bool SetProperty(
                Reflector reflector,
                ref object? instance,
                Type type,
                PropertyInfo prop,
                SerializedMember? member,
                int depth,
                Logs? logs,
                BindingFlags flags,
                ILogger? logger)
            {
                return true;
            }
        }

        [Fact]
        public void GetSerializableFields_ShouldCacheResults()
        {
            var converter = new TestableReflectionConverter();
            var reflector = new Reflector();
            var type = typeof(TestEntity);
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            // First call - should populate cache
            var fields1 = converter.GetSerializableFields(reflector, type, bindingFlags);

            // Verify cache is populated
            var cache = GetFieldCache(converter);
            Assert.Single(cache);
            Assert.True(cache.ContainsKey((type, bindingFlags)));

            // Second call - should retrieve from cache
            var fields2 = converter.GetSerializableFields(reflector, type, bindingFlags);

            // Assertions
            Assert.NotNull(fields1);
            Assert.NotNull(fields2);
            Assert.Same(fields1, fields2); // Should be same array instance from cache
        }

        [Fact]
        public void GetSerializableProperties_ShouldCacheResults()
        {
            var converter = new TestableReflectionConverter();
            var reflector = new Reflector();
            var type = typeof(TestEntity);
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            // First call - should populate cache
            var props1 = converter.GetSerializableProperties(reflector, type, bindingFlags);

            // Verify cache is populated
            var cache = GetPropertyCache(converter);
            Assert.Single(cache);
            Assert.True(cache.ContainsKey((type, bindingFlags)));

            // Second call - should retrieve from cache
            var props2 = converter.GetSerializableProperties(reflector, type, bindingFlags);

            // Assertions
            Assert.NotNull(props1);
            Assert.NotNull(props2);
            Assert.Same(props1, props2); // Should be same array instance from cache
        }

        [Fact]
        public void Caches_ShouldDifferentiateByBindingFlags()
        {
            var converter = new TestableReflectionConverter();
            var reflector = new Reflector();
            var type = typeof(TestEntity);
            var flagsPublic = BindingFlags.Public | BindingFlags.Instance;
            var flagsNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;

            // Call with public flags
            converter.GetSerializableFields(reflector, type, flagsPublic);

            // Call with non-public flags
            converter.GetSerializableFields(reflector, type, flagsNonPublic);

            // Verify cache contains both entries
            var cache = GetFieldCache(converter);
            Assert.Equal(2, cache.Count);
            Assert.True(cache.ContainsKey((type, flagsPublic)));
            Assert.True(cache.ContainsKey((type, flagsNonPublic)));
        }

        [Fact]
        public void Caches_ShouldDifferentiateByTypes()
        {
            var converter = new TestableReflectionConverter();
            var reflector = new Reflector();
            var type1 = typeof(TestEntity);
            var type2 = typeof(string); // Arbitrary other type
            var flags = BindingFlags.Public | BindingFlags.Instance;

            converter.GetSerializableFields(reflector, type1, flags);
            converter.GetSerializableFields(reflector, type2, flags);

            var cache = GetFieldCache(converter);
            Assert.Equal(2, cache.Count);
            Assert.True(cache.ContainsKey((type1, flags)));
            Assert.True(cache.ContainsKey((type2, flags)));
        }

        [Fact]
        public void ClearReflectionCache_ShouldClearBothCaches()
        {
            var converter = new TestableReflectionConverter();
            var reflector = new Reflector();
            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;

            // Populate caches
            converter.GetSerializableFields(reflector, type, flags);
            converter.GetSerializableProperties(reflector, type, flags);

            // Verify populated
            var fieldCache = GetFieldCache(converter);
            var propCache = GetPropertyCache(converter);
            Assert.NotEmpty(fieldCache);
            Assert.NotEmpty(propCache);

            // Act
            converter.ClearReflectionCache();

            // Assert
            Assert.Empty(fieldCache);
            Assert.Empty(propCache);
        }

        // Helper methods to access private caches via reflection
        private ConcurrentDictionary<(Type, BindingFlags), FieldInfo[]> GetFieldCache(object converter)
        {
            var field = typeof(BaseReflectionConverter<TestEntity>)
                .GetField("_serializableFieldsCache", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new InvalidOperationException("Field _serializableFieldsCache not found");
            return (ConcurrentDictionary<(Type, BindingFlags), FieldInfo[]>)field.GetValue(converter)!;
        }

        private ConcurrentDictionary<(Type, BindingFlags), PropertyInfo[]> GetPropertyCache(object converter)
        {
            var field = typeof(BaseReflectionConverter<TestEntity>)
                .GetField("_serializablePropertiesCache", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new InvalidOperationException("Field _serializablePropertiesCache not found");
            return (ConcurrentDictionary<(Type, BindingFlags), PropertyInfo[]>)field.GetValue(converter)!;
        }
    }
}
