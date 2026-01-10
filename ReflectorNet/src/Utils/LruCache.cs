/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Threading;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /// <summary>
    /// A thread-safe Least Recently Used (LRU) cache with a configurable maximum capacity.
    /// When the cache reaches its capacity, the least recently accessed items are evicted.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public sealed class LruCache<TKey, TValue> where TKey : notnull
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        private readonly struct CacheItem
        {
            public readonly TKey Key;
            public readonly TValue Value;

            public CacheItem(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LruCache{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="capacity">The maximum number of items the cache can hold.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is less than 1.</exception>
        public LruCache(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");

            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// Gets the current number of items in the cache.
        /// </summary>
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _cache.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets the maximum capacity of the cache.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Tries to get a value from the cache. If found, the item is marked as recently used.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The value if found; otherwise, the default value.</param>
        /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    // Move to front (most recently used)
                    _lock.EnterWriteLock();
                    try
                    {
                        _lruList.Remove(node);
                        _lruList.AddFirst(node);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }

                    value = node.Value.Value;
                    return true;
                }

                value = default!;
                return false;
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Gets or adds a value to the cache. If the key exists, returns the existing value
        /// and marks it as recently used. If the key doesn't exist, calls the factory to create
        /// the value, adds it to the cache (potentially evicting the LRU item), and returns it.
        /// </summary>
        /// <param name="key">The key to look up or add.</param>
        /// <param name="valueFactory">A factory function to create the value if the key doesn't exist.</param>
        /// <returns>The existing or newly created value.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            // First, try read-only access
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    // Move to front (most recently used)
                    _lock.EnterWriteLock();
                    try
                    {
                        _lruList.Remove(existingNode);
                        _lruList.AddFirst(existingNode);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }

                    return existingNode.Value.Value;
                }

                // Key not found, need to add
                var value = valueFactory(key);

                _lock.EnterWriteLock();
                try
                {
                    // Double-check after acquiring write lock
                    if (_cache.TryGetValue(key, out existingNode))
                    {
                        _lruList.Remove(existingNode);
                        _lruList.AddFirst(existingNode);
                        return existingNode.Value.Value;
                    }

                    // Evict LRU items if at capacity
                    while (_cache.Count >= _capacity && _lruList.Last != null)
                    {
                        var lruNode = _lruList.Last;
                        _cache.Remove(lruNode.Value.Key);
                        _lruList.RemoveLast();
                    }

                    // Add new item at front
                    var cacheItem = new CacheItem(key, value);
                    var newNode = new LinkedListNode<CacheItem>(cacheItem);
                    _lruList.AddFirst(newNode);
                    _cache[key] = newNode;

                    return value;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Clears all items from the cache.
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _cache.Clear();
                _lruList.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets or sets a value in the cache using indexer syntax.
        /// Getting a value marks it as recently used.
        /// Setting a value adds or updates it in the cache (potentially evicting the LRU item).
        /// </summary>
        /// <param name="key">The key to get or set.</param>
        /// <returns>The value associated with the key.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when getting a key that doesn't exist.</exception>
        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                    return value;
                throw new KeyNotFoundException($"The key '{key}' was not found in the cache.");
            }
            set
            {
                _lock.EnterWriteLock();
                try
                {
                    // Remove existing if present
                    if (_cache.TryGetValue(key, out var existingNode))
                    {
                        _lruList.Remove(existingNode);
                        _cache.Remove(key);
                    }

                    // Evict LRU items if at capacity
                    while (_cache.Count >= _capacity && _lruList.Last != null)
                    {
                        var lruNode = _lruList.Last;
                        _cache.Remove(lruNode.Value.Key);
                        _lruList.RemoveLast();
                    }

                    // Add new item at front
                    var cacheItem = new CacheItem(key, value);
                    var newNode = new LinkedListNode<CacheItem>(cacheItem);
                    _lruList.AddFirst(newNode);
                    _cache[key] = newNode;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Checks if the cache contains the specified key.
        /// Note: This does NOT update the LRU order.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(TKey key)
        {
            _lock.EnterReadLock();
            try
            {
                return _cache.ContainsKey(key);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
