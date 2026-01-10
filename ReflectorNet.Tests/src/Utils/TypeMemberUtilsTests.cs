using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class TypeMemberUtilsTests : BaseTest
    {
        public TypeMemberUtilsTests(ITestOutputHelper output) : base(output)
        {
        }

        private class TestEntity
        {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            public int Field1;
            public string? Field2;
#pragma warning restore CS0649
            public int Property1 { get; set; }
            public string? Property2 { get; set; }
        }

        [Fact]
        public void GetField_ShouldCacheResults()
        {
            // Clean slate
            TypeMemberUtils.ClearFieldCache();
            var cache = GetFieldCache();
            Assert.Equal(0, cache.Count);

            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var fieldName = nameof(TestEntity.Field1);

            // First call - should populate cache
            var field1 = TypeMemberUtils.GetField(type, flags, fieldName);
            Assert.NotNull(field1);
            Assert.Equal(fieldName, field1!.Name);
            Assert.Equal(1, cache.Count);

            // Second call - should retrieve from cache (and refer to same instance although FieldInfo is likely same anyway)
            var field2 = TypeMemberUtils.GetField(type, flags, fieldName);
            Assert.Same(field1, field2);
            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public void GetField_ShouldCacheNulls_WhenFieldNotFound()
        {
            TypeMemberUtils.ClearFieldCache();
            var cache = GetFieldCache();

            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var fieldName = "NonExistentField";

            // First call
            var field1 = TypeMemberUtils.GetField(type, flags, fieldName);
            Assert.Null(field1);
            Assert.Equal(1, cache.Count);

            // Second call
            var field2 = TypeMemberUtils.GetField(type, flags, fieldName);
            Assert.Null(field2);
            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public void GetProperty_ShouldCacheResults()
        {
            TypeMemberUtils.ClearPropertyCache();
            var cache = GetPropertyCache();
            Assert.Equal(0, cache.Count);

            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var propName = nameof(TestEntity.Property1);

            // First call
            var prop1 = TypeMemberUtils.GetProperty(type, flags, propName);
            Assert.NotNull(prop1);
            Assert.Equal(propName, prop1!.Name);
            Assert.Equal(1, cache.Count);

            // Second call
            var prop2 = TypeMemberUtils.GetProperty(type, flags, propName);
            Assert.Same(prop1, prop2);
            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public void GetProperty_ShouldCacheNulls_WhenPropertyNotFound()
        {
            TypeMemberUtils.ClearPropertyCache();
            var cache = GetPropertyCache();

            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var propName = "NonExistentProperty";

            // First call
            var prop1 = TypeMemberUtils.GetProperty(type, flags, propName);
            Assert.Null(prop1);
            Assert.Equal(1, cache.Count);

            // Second call
            var prop2 = TypeMemberUtils.GetProperty(type, flags, propName);
            Assert.Null(prop2);
            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public void ClearFieldCache_ShouldClear()
        {
            TypeMemberUtils.GetField(typeof(TestEntity), BindingFlags.Public | BindingFlags.Instance, nameof(TestEntity.Field1));
            var cache = GetFieldCache();
            Assert.True(cache.Count > 0);

            TypeMemberUtils.ClearFieldCache();
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void ClearPropertyCache_ShouldClear()
        {
            TypeMemberUtils.GetProperty(typeof(TestEntity), BindingFlags.Public | BindingFlags.Instance, nameof(TestEntity.Property1));
            var cache = GetPropertyCache();
            Assert.True(cache.Count > 0);

            TypeMemberUtils.ClearPropertyCache();
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void ClearAllCaches_ShouldClearBoth()
        {
            TypeMemberUtils.GetField(typeof(TestEntity), BindingFlags.Public | BindingFlags.Instance, nameof(TestEntity.Field1));
            TypeMemberUtils.GetProperty(typeof(TestEntity), BindingFlags.Public | BindingFlags.Instance, nameof(TestEntity.Property1));

            TypeMemberUtils.ClearAllCaches();

            Assert.Equal(0, GetFieldCache().Count);
            Assert.Equal(0, GetPropertyCache().Count);
        }

        [Fact]
        public async Task ThreadSafety_ConcurrentAccess()
        {
            // Clear caches first to ensure we start clean
            TypeMemberUtils.ClearAllCaches();

            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var fieldName = nameof(TestEntity.Field1);
            var propName = nameof(TestEntity.Property1);

            var tasks = new List<Task>();

            // Spawn multiple threads accessing the same fields/properties
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        var f = TypeMemberUtils.GetField(type, flags, fieldName);
                        Assert.NotNull(f);

                        var p = TypeMemberUtils.GetProperty(type, flags, propName);
                        Assert.NotNull(p);
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Verify items are in cache
            var fieldCache = GetFieldCache();
            // We expect at least one item.
            // Note: Since we're using reflection to get the cache, and it's an LruCache,
            // inspecting it is thread-safe only if LruCache itself is thread-safe (which it is).
            Assert.True(fieldCache.ContainsKey((type, flags, fieldName)));
        }

        // Helper methods to access private caches
        private LruCache<(Type, BindingFlags, string), FieldInfo?> GetFieldCache()
        {
            var field = typeof(TypeMemberUtils).GetField("_fieldCache", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null) throw new InvalidOperationException("_fieldCache not found");
            return (LruCache<(Type, BindingFlags, string), FieldInfo?>)field.GetValue(null)!;
        }

        private LruCache<(Type, BindingFlags, string), PropertyInfo?> GetPropertyCache()
        {
            var field = typeof(TypeMemberUtils).GetField("_propertyCache", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null) throw new InvalidOperationException("_propertyCache not found");
            return (LruCache<(Type, BindingFlags, string), PropertyInfo?>)field.GetValue(null)!;
        }
    }
}
