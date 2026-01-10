/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /// <summary>
    /// Provides cached access to type member information (fields and properties) for improved performance.
    /// This utility class caches reflection lookups to avoid repeated costly reflection calls when
    /// accessing the same members multiple times.
    /// </summary>
    /// <remarks>
    /// The caches are static and shared across all callers. This is safe because the underlying
    /// reflection operations (<see cref="Type.GetField(string, BindingFlags)"/> and
    /// <see cref="Type.GetProperty(string, BindingFlags)"/>) are deterministic and don't depend
    /// on caller context. Uses LRU eviction to prevent unbounded memory growth.
    /// </remarks>
    public static class TypeMemberUtils
    {
        /// <summary>
        /// Maximum capacity for the field lookup cache.
        /// </summary>
        public const int FieldCacheCapacity = 10000;

        /// <summary>
        /// Maximum capacity for the property lookup cache.
        /// </summary>
        public const int PropertyCacheCapacity = 10000;

        // LRU cache for field lookups: (Type, BindingFlags, fieldName) -> FieldInfo?
        private static readonly LruCache<(Type, BindingFlags, string), FieldInfo?> _fieldCache = new(FieldCacheCapacity);

        // LRU cache for property lookups: (Type, BindingFlags, propertyName) -> PropertyInfo?
        private static readonly LruCache<(Type, BindingFlags, string), PropertyInfo?> _propertyCache = new(PropertyCacheCapacity);

        /// <summary>
        /// Gets a cached <see cref="FieldInfo"/> for the specified type, binding flags, and field name.
        /// </summary>
        /// <param name="type">The type to search for the field.</param>
        /// <param name="flags">The binding flags that control the search.</param>
        /// <param name="fieldName">The name of the field to find.</param>
        /// <returns>The <see cref="FieldInfo"/> for the field, or <c>null</c> if not found.</returns>
        /// <remarks>
        /// Results are cached to improve performance for repeated lookups of the same field.
        /// Use <see cref="ClearFieldCache"/> to clear the cache if needed.
        /// </remarks>
        public static FieldInfo? GetField(Type type, BindingFlags flags, string fieldName)
        {
            var cacheKey = (type, flags, fieldName);
            return _fieldCache.GetOrAdd(cacheKey, key => key.Item1.GetField(key.Item3, key.Item2));
        }

        /// <summary>
        /// Gets a cached <see cref="PropertyInfo"/> for the specified type, binding flags, and property name.
        /// </summary>
        /// <param name="type">The type to search for the property.</param>
        /// <param name="flags">The binding flags that control the search.</param>
        /// <param name="propertyName">The name of the property to find.</param>
        /// <returns>The <see cref="PropertyInfo"/> for the property, or <c>null</c> if not found.</returns>
        /// <remarks>
        /// Results are cached to improve performance for repeated lookups of the same property.
        /// Use <see cref="ClearPropertyCache"/> to clear the cache if needed.
        /// </remarks>
        public static PropertyInfo? GetProperty(Type type, BindingFlags flags, string propertyName)
        {
            var cacheKey = (type, flags, propertyName);
            return _propertyCache.GetOrAdd(cacheKey, key => key.Item1.GetProperty(key.Item3, key.Item2));
        }

        /// <summary>
        /// Clears the field lookup cache.
        /// </summary>
        /// <remarks>
        /// Call this method to release cached field metadata in long-running or memory-sensitive scenarios.
        /// </remarks>
        public static void ClearFieldCache(ILogger? logger = null)
        {
            logger?.LogDebug("Clearing field lookup cache with {_fieldCacheCount} entries (capacity: {_fieldCacheCapacity}).",
                _fieldCache.Count, _fieldCache.Capacity);
            _fieldCache.Clear();
        }

        /// <summary>
        /// Clears the property lookup cache.
        /// </summary>
        /// <remarks>
        /// Call this method to release cached property metadata in long-running or memory-sensitive scenarios.
        /// </remarks>
        public static void ClearPropertyCache(ILogger? logger = null)
        {
            logger?.LogDebug("Clearing property lookup cache with {_propertyCacheCount} entries (capacity: {_propertyCacheCapacity}).",
                _propertyCache.Count, _propertyCache.Capacity);
            _propertyCache.Clear();
        }

        /// <summary>
        /// Clears both the field and property lookup caches.
        /// </summary>
        /// <remarks>
        /// Call this method to release all cached member metadata in long-running or memory-sensitive scenarios.
        /// </remarks>
        public static void ClearAllCaches(ILogger? logger = null)
        {
            ClearFieldCache(logger);
            ClearPropertyCache(logger);
        }
    }
}
