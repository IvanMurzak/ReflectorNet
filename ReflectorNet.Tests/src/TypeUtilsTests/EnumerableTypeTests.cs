using System;
using System.Collections.Generic;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.TypeUtilsTests
{
    public class EnumerableTypeTests : BaseTest
    {
        public EnumerableTypeTests(ITestOutputHelper output) : base(output) { }

        // Unique types for testing to avoid collisions with other parallel tests
        private class TestList : List<int> { }
        private class TestArray { } // Not enumerable
        private class TestEnumerable : IEnumerable<Guid>
        {
            public IEnumerator<Guid> GetEnumerator() => throw new NotImplementedException();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new NotImplementedException();
        }

        private LruCache<Type, Type?> GetCache()
        {
            var field = typeof(TypeUtils).GetField("_enumerableItemTypeCache", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
            {
                throw new InvalidOperationException("Field _enumerableItemTypeCache not found in TypeUtils");
            }
            return (LruCache<Type, Type?>)field.GetValue(null)!;
        }

        [Fact]
        public void GetEnumerableItemType_ShouldReturnCorrectType_ForGenericList()
        {
            var type = typeof(List<string>);
            var itemType = TypeUtils.GetEnumerableItemType(type);
            Assert.Equal(typeof(string), itemType);
        }

        [Fact]
        public void GetEnumerableItemType_ShouldReturnCorrectType_ForArray()
        {
            var type = typeof(int[]);
            var itemType = TypeUtils.GetEnumerableItemType(type);
            Assert.Equal(typeof(int), itemType);
        }

        [Fact]
        public void GetEnumerableItemType_ShouldReturnCorrectType_ForIEnumerable()
        {
            var type = typeof(IEnumerable<double>);
            var itemType = TypeUtils.GetEnumerableItemType(type);
            Assert.Equal(typeof(double), itemType);
        }

        [Fact]
        public void GetEnumerableItemType_ShouldReturnNull_ForNonEnumerable()
        {
            var type = typeof(DateTime);
            var itemType = TypeUtils.GetEnumerableItemType(type);
            Assert.Null(itemType);
        }

        [Fact]
        public void GetEnumerableItemType_ShouldCacheResults()
        {
            var cache = GetCache();
            var type = typeof(TestList); // Unique type

            // Ensure not in cache (though might be if test re-runs, so we can't strict assert false if we don't clear)
            // But we can check after we add it.

            // First call
            var itemType1 = TypeUtils.GetEnumerableItemType(type);
            Assert.Equal(typeof(int), itemType1);

            Assert.True(cache.ContainsKey(type), "Cache should contain the type after access");

            // Second call
            var itemType2 = TypeUtils.GetEnumerableItemType(type);
            Assert.Equal(typeof(int), itemType2);
            Assert.True(cache.ContainsKey(type));
        }

        [Fact]
        public void GetEnumerableItemType_ShouldCacheNullResults()
        {
            var cache = GetCache();
            var type = typeof(TestArray); // Unique non-enumerable type (name is misleading, it's just object)

            // First call
            var itemType1 = TypeUtils.GetEnumerableItemType(type);
            Assert.Null(itemType1);

            Assert.True(cache.ContainsKey(type), "Cache should contain the type even if result is null");
        }

        [Fact]
        public void ClearEnumerableItemTypeCache_ShouldClearCache()
        {
            var cache = GetCache();
            var type = typeof(TestEnumerable);

            // Populate
            TypeUtils.GetEnumerableItemType(type);
            Assert.True(cache.ContainsKey(type));

            // Clear
            TypeUtils.ClearEnumerableItemTypeCache();

            // Verify cleared (for this specific key at least)
            // Note: Parallel tests might have re-added OTHER keys, but our unique key should be gone.
            Assert.False(cache.ContainsKey(type), "Cache should not contain the key after clear");
        }
    }
}
