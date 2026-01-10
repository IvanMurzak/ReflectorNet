using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Utils;
using Xunit;
using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests.Utils
{
    public class LruCacheTests : BaseTest
    {
        public LruCacheTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Constructor_WithValidCapacity_ShouldInitialize()
        {
            var cache = new LruCache<string, int>(10);
            Assert.Equal(10, cache.Capacity);
            Assert.Equal(0, cache.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidCapacity_ShouldThrowArgumentOutOfRangeException(int capacity)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LruCache<string, int>(capacity));
        }

        [Fact]
        public void AddAndRetrieve_ShouldWorkCorrectly()
        {
            var cache = new LruCache<int, string>(3);
            cache[1] = "One";
            cache[2] = "Two";

            Assert.Equal(2, cache.Count);
            Assert.Equal("One", cache[1]);
            Assert.Equal("Two", cache[2]);
            Assert.True(cache.ContainsKey(1));
            Assert.True(cache.ContainsKey(2));
        }

        [Fact]
        public void TryGetValue_ShouldReturnValuesCorrectly()
        {
            var cache = new LruCache<int, string>(3);
            cache[1] = "One";

            Assert.True(cache.TryGetValue(1, out var value));
            Assert.Equal("One", value);

            Assert.False(cache.TryGetValue(99, out var missingValue));
            Assert.Null(missingValue); // Assuming string is reference type or default
        }

        [Fact]
        public void Eviction_ShouldRemoveLeastRecentlyUsed_WhenAddingNewItems()
        {
            // Capacity 3
            var cache = new LruCache<int, string>(3);

            // Add 1, 2, 3
            cache[1] = "One";
            cache[2] = "Two";
            cache[3] = "Three";

            // Cache is full: [3, 2, 1] (Most recent -> Least recent)
            Assert.Equal(3, cache.Count);

            // Add 4. 1 should be evicted.
            cache[4] = "Four";

            // State: [4, 3, 2]
            Assert.True(cache.ContainsKey(4));
            Assert.True(cache.ContainsKey(3));
            Assert.True(cache.ContainsKey(2));
            Assert.False(cache.ContainsKey(1), "Key 1 should have been evicted");
        }

        [Fact]
        public void Access_ShouldUpdateLRUOrder()
        {
            // Capacity 3
            var cache = new LruCache<int, string>(3);

            cache[1] = "One"; // [1]
            cache[2] = "Two"; // [2, 1]
            cache[3] = "Three"; // [3, 2, 1]

            // Access 1 via indexer. It becomes MRU.
            _ = cache[1]; // [1, 3, 2]

            // Add 4. 2 should be evicted (as it is now LRU).
            cache[4] = "Four"; // [4, 1, 3]

            Assert.True(cache.ContainsKey(4));
            Assert.True(cache.ContainsKey(1));
            Assert.True(cache.ContainsKey(3));
            Assert.False(cache.ContainsKey(2), "Key 2 should have been evicted after Key 1 was accessed");
        }

        [Fact]
        public void AccessViaTryGetValue_ShouldUpdateLRUOrder()
        {
            var cache = new LruCache<int, string>(3);
            cache[1] = "One";
            cache[2] = "Two";
            cache[3] = "Three"; // [3, 2, 1]

            // Access 1 via TryGetValue. Becomes MRU.
            cache.TryGetValue(1, out _); // [1, 3, 2]

            cache[4] = "Four"; // [4, 1, 3] erases 2

            Assert.False(cache.ContainsKey(2));
            Assert.True(cache.ContainsKey(1));
        }

        [Fact]
        public void GetOrAdd_ShouldUpdateLRUOrder_WhenKeyExists()
        {
            var cache = new LruCache<int, string>(3);
            cache[1] = "One";
            cache[2] = "Two";
            cache[3] = "Three"; // [3, 2, 1]

            // GetOrAdd 1. Should return "One" and make it MRU.
            var val = cache.GetOrAdd(1, k => "NewOne"); // [1, 3, 2]
            Assert.Equal("One", val);

            cache[4] = "Four"; // [4, 1, 3], evicts 2

            Assert.False(cache.ContainsKey(2));
            Assert.True(cache.ContainsKey(1));
        }

        [Fact]
        public void GetOrAdd_ShouldAddNewItem_WhenKeyDoesNotExist()
        {
            var cache = new LruCache<int, string>(3);
            cache[1] = "One";
            cache[2] = "Two"; // [2, 1]

            // GetOrAdd 3.
            var val = cache.GetOrAdd(3, k => "Three"); // [3, 2, 1]
            Assert.Equal("Three", val);
            Assert.Equal(3, cache.Count);

            // Add 4, evicts 1
            cache[4] = "Four"; // [4, 3, 2]

            Assert.False(cache.ContainsKey(1));
            Assert.True(cache.ContainsKey(3));
        }

        [Fact]
        public void GetOrAdd_ShouldEvict_WhenAddingNewItemAtCapacity()
        {
            var cache = new LruCache<int, string>(2);
            cache[1] = "One";
            cache[2] = "Two"; // [2, 1]

            // GetOrAdd 3. Should evict 1.
            _ = cache.GetOrAdd(3, k => "Three"); // [3, 2]

            Assert.Equal(2, cache.Count);
            Assert.False(cache.ContainsKey(1));
            Assert.True(cache.ContainsKey(2));
            Assert.True(cache.ContainsKey(3));
        }

        [Fact]
        public void IndexerSet_ShouldUpdateExistingValue_AndMoveToFront()
        {
            var cache = new LruCache<int, string>(3);
            cache[1] = "One";
            cache[2] = "Two";
            cache[3] = "Three"; // [3, 2, 1]

            // Update 2
            cache[2] = "TwoUpdated"; // [2, 3, 1]

            // Add 4, evicts 1
            cache[4] = "Four"; // [4, 2, 3]

            Assert.False(cache.ContainsKey(1));
            Assert.True(cache.ContainsKey(2));
            Assert.Equal("TwoUpdated", cache[2]);
        }

        [Fact]
        public void IndexerGet_ShouldThrow_WhenKeyNotFound()
        {
            var cache = new LruCache<int, string>(10);
            Assert.Throws<KeyNotFoundException>(() =>
            {
                _ = cache[99];
            });
        }

        [Fact]
        public void Clear_ShouldEmptyCache()
        {
            var cache = new LruCache<int, string>(5);
            cache[1] = "One";
            cache[2] = "Two";

            Assert.Equal(2, cache.Count);
            cache.Clear();
            Assert.Equal(0, cache.Count);
            Assert.False(cache.ContainsKey(1));
        }

        [Fact]
        public void ContainsKey_ShouldNotUpdateLRU()
        {
            var cache = new LruCache<int, string>(2);
            cache[1] = "One";
            cache[2] = "Two"; // [2, 1]

            // ContainsKey 1. Should NOT make it MRU. Order remains [2, 1]
            bool exists = cache.ContainsKey(1);
            Assert.True(exists);

            // Add 3. Should evict 1 because 1 is still LRU.
            cache[3] = "Three"; // [3, 2]

            Assert.False(cache.ContainsKey(1));
            Assert.True(cache.ContainsKey(2));
            Assert.True(cache.ContainsKey(3));
        }

        [Fact]
        public void ThreadSafety_AddFromFileMultipleThreads()
        {
            // Stress test for concurrency
            int capacity = 100;
            int itemsToAdd = 10000;
            var cache = new LruCache<int, int>(capacity);

            Parallel.For(0, itemsToAdd, i =>
            {
                cache.GetOrAdd(i % (capacity * 2), k => k * 2);
            });

            Assert.True(cache.Count <= capacity);
            Assert.True(cache.Count > 0);
        }

        [Fact]
        public async Task ThreadSafety_ReadWriteConcurrency()
        {
            int capacity = 50;
            var cache = new LruCache<int, string>(capacity);
            bool stop = false;

            // Writer task
            var writer = Task.Run(async () =>
            {
                int counter = 0;
                while (!stop)
                {
                    cache[counter % 100] = $"Value-{counter}";
                    counter++;
                    if (counter % 10 == 0) await Task.Delay(1);
                }
            });

            // Reader task
            var reader = Task.Run(async () =>
            {
                int counter = 0;
                while (!stop)
                {
                    if (cache.TryGetValue(counter % 100, out var val))
                    {
                        Assert.StartsWith("Value-", val);
                    }
                    counter++;
                    if (counter % 10 == 0) await Task.Delay(1);
                }
            });

            // Let them run for a bit
            await Task.Delay(500);
            stop = true;
            await Task.WhenAll(writer, reader);

            Assert.True(cache.Count <= capacity);
        }
    }
}
