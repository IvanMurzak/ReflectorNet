using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class TypeUtils
    {
        /// <summary>
        /// Maximum capacity for the type name resolution cache.
        /// </summary>
        public const int TypeCacheCapacity = 1000;

        /// <summary>
        /// Maximum capacity for the enumerable item type cache.
        /// </summary>
        public const int EnumerableItemTypeCacheCapacity = 500;

        /// <summary>
        /// Gets all types from all loaded assemblies.
        /// </summary>
        public static IEnumerable<Type> AllTypes => AssemblyUtils.AllTypes;

        // Characters used to separate nested type names (e.g. `Outer+Inner` or `Outer.Inner`).
        private static readonly char[] NestedTypeSeparators = new[] { '+', '.' };

        // LRU cache for resolved type names to avoid repeated AllTypes enumeration (thread-safe)
        private static readonly LruCache<string, Type?> _typeCache = new(TypeCacheCapacity);

        // LRU cache for assembly-prefixed type lookups (thread-safe)
        // Key format: "assemblyPrefix|typeName"
        private static readonly LruCache<string, Type?> _assemblyTypeCache = new(TypeCacheCapacity);

        // LRU cache for exact assembly type lookups (thread-safe)
        // Key format: "assemblyFullName|typeName"
        private static readonly LruCache<string, Type?> _exactAssemblyTypeCache = new(TypeCacheCapacity);

        // LRU cache for enumerable item types to avoid repeated interface/inheritance walks (thread-safe)
        private static readonly LruCache<Type, Type?> _enumerableItemTypeCache = new(EnumerableItemTypeCacheCapacity);

        /// <summary>
        /// Clears the type name resolution cache.
        /// </summary>
        public static void ClearTypeCache(ILogger? logger = null)
        {
            logger?.LogDebug("Clearing type resolution cache with {count} entries (capacity: {capacity}).",
                _typeCache.Count, _typeCache.Capacity);
            _typeCache.Clear();
        }

        /// <summary>
        /// Clears the enumerable item type cache.
        /// </summary>
        public static void ClearEnumerableItemTypeCache(ILogger? logger = null)
        {
            logger?.LogDebug("Clearing enumerable item type cache with {count} entries (capacity: {capacity}).",
                _enumerableItemTypeCache.Count, _enumerableItemTypeCache.Capacity);
            _enumerableItemTypeCache.Clear();
        }

        /// <summary>
        /// Clears the assembly-prefixed type resolution cache.
        /// </summary>
        public static void ClearAssemblyTypeCache(ILogger? logger = null)
        {
            logger?.LogDebug("Clearing assembly-prefixed type resolution cache with {count} entries (capacity: {capacity}).",
                _assemblyTypeCache.Count, _assemblyTypeCache.Capacity);
            _assemblyTypeCache.Clear();
        }

        /// <summary>
        /// Clears the exact assembly type resolution cache.
        /// </summary>
        public static void ClearExactAssemblyTypeCache(ILogger? logger = null)
        {
            logger?.LogDebug("Clearing exact assembly type resolution cache with {count} entries (capacity: {capacity}).",
                _exactAssemblyTypeCache.Count, _exactAssemblyTypeCache.Capacity);
            _exactAssemblyTypeCache.Clear();
        }
    }
}
