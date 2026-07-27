using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    /// <summary>
    /// <c>TypeMemberUtils</c> caches into PROCESS-GLOBAL statics, and xunit runs test classes in
    /// parallel, so these tests assert on cache CONTENT - "is this key cached, and is the cached
    /// instance reused?" - never on the cache's total <c>Count</c>. A count assertion silently
    /// depends on no other test class touching the cache at that moment; since
    /// <c>TypeMemberUtils.GetField</c>/<c>GetProperty</c> sit on the main <c>TryModify</c> path,
    /// that is a race any new test elsewhere can lose (observed: a red CI run on exactly that).
    /// The content assertions are also strictly stronger - they pin WHICH key was cached, and that
    /// a cached null really is a cached null, where a count only claimed that something was cached.
    /// </summary>
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

            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var fieldName = nameof(TestEntity.Field1);
            var key = (type, flags, fieldName);

            Assert.False(cache.ContainsKey(key));

            // First call - should populate the cache entry for THIS key
            var field1 = TypeMemberUtils.GetField(type, flags, fieldName);
            Assert.NotNull(field1);
            Assert.Equal(fieldName, field1!.Name);
            Assert.True(cache.ContainsKey(key));

            // Second call - should retrieve from cache (and refer to same instance although FieldInfo is likely same anyway)
            var field2 = TypeMemberUtils.GetField(type, flags, fieldName);
            Assert.Same(field1, field2);
            Assert.True(cache.TryGetValue(key, out var cachedField));
            Assert.Same(field1, cachedField);
        }

        [Fact]
        public void GetField_ShouldCacheNulls_WhenFieldNotFound()
        {
            TypeMemberUtils.ClearFieldCache();
            var cache = GetFieldCache();

            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var fieldName = "NonExistentField";
            var key = (type, flags, fieldName);

            // First call
            var field1 = TypeMemberUtils.GetField(type, flags, fieldName);
            Assert.Null(field1);

            // The null itself must be cached - that is the whole point of this test, and asserting
            // the key holds a null says so directly instead of inferring it from a count.
            Assert.True(cache.TryGetValue(key, out var cachedField));
            Assert.Null(cachedField);

            // Second call
            var field2 = TypeMemberUtils.GetField(type, flags, fieldName);
            Assert.Null(field2);
            Assert.True(cache.ContainsKey(key));
        }

        [Fact]
        public void GetProperty_ShouldCacheResults()
        {
            TypeMemberUtils.ClearPropertyCache();
            var cache = GetPropertyCache();

            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var propName = nameof(TestEntity.Property1);
            var key = (type, flags, propName);

            Assert.False(cache.ContainsKey(key));

            // First call
            var prop1 = TypeMemberUtils.GetProperty(type, flags, propName);
            Assert.NotNull(prop1);
            Assert.Equal(propName, prop1!.Name);
            Assert.True(cache.ContainsKey(key));

            // Second call
            var prop2 = TypeMemberUtils.GetProperty(type, flags, propName);
            Assert.Same(prop1, prop2);
            Assert.True(cache.TryGetValue(key, out var cachedProp));
            Assert.Same(prop1, cachedProp);
        }

        [Fact]
        public void GetProperty_ShouldCacheNulls_WhenPropertyNotFound()
        {
            TypeMemberUtils.ClearPropertyCache();
            var cache = GetPropertyCache();

            var type = typeof(TestEntity);
            var flags = BindingFlags.Public | BindingFlags.Instance;
            var propName = "NonExistentProperty";
            var key = (type, flags, propName);

            // First call
            var prop1 = TypeMemberUtils.GetProperty(type, flags, propName);
            Assert.Null(prop1);

            // The null itself must be cached - assert that directly rather than via a count.
            Assert.True(cache.TryGetValue(key, out var cachedProp));
            Assert.Null(cachedProp);

            // Second call
            var prop2 = TypeMemberUtils.GetProperty(type, flags, propName);
            Assert.Null(prop2);
            Assert.True(cache.ContainsKey(key));
        }

        [Fact]
        public void ClearFieldCache_ShouldClear()
        {
            var key = (typeof(TestEntity), BindingFlags.Public | BindingFlags.Instance, nameof(TestEntity.Field1));

            TypeMemberUtils.GetField(key.Item1, key.Item2, key.Item3);
            var cache = GetFieldCache();
            Assert.True(cache.ContainsKey(key));

            TypeMemberUtils.ClearFieldCache();
            Assert.False(cache.ContainsKey(key));
        }

        [Fact]
        public void ClearPropertyCache_ShouldClear()
        {
            var key = (typeof(TestEntity), BindingFlags.Public | BindingFlags.Instance, nameof(TestEntity.Property1));

            TypeMemberUtils.GetProperty(key.Item1, key.Item2, key.Item3);
            var cache = GetPropertyCache();
            Assert.True(cache.ContainsKey(key));

            TypeMemberUtils.ClearPropertyCache();
            Assert.False(cache.ContainsKey(key));
        }

        [Fact]
        public void ClearAllCaches_ShouldClearBoth()
        {
            var fieldKey = (typeof(TestEntity), BindingFlags.Public | BindingFlags.Instance, nameof(TestEntity.Field1));
            var propKey = (typeof(TestEntity), BindingFlags.Public | BindingFlags.Instance, nameof(TestEntity.Property1));

            TypeMemberUtils.GetField(fieldKey.Item1, fieldKey.Item2, fieldKey.Item3);
            TypeMemberUtils.GetProperty(propKey.Item1, propKey.Item2, propKey.Item3);
            Assert.True(GetFieldCache().ContainsKey(fieldKey));
            Assert.True(GetPropertyCache().ContainsKey(propKey));

            TypeMemberUtils.ClearAllCaches();

            Assert.False(GetFieldCache().ContainsKey(fieldKey));
            Assert.False(GetPropertyCache().ContainsKey(propKey));
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
