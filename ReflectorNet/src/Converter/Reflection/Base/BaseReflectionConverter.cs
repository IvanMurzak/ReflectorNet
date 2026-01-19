/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    /// <summary>
    /// Abstract base class for reflection-based converters that handle serialization, deserialization, and population
    /// operations for specific types. This class provides the foundation for the converter chain pattern used
    /// throughout ReflectorNet's type handling system.
    ///
    /// Core Responsibilities:
    /// - Defines common behavior and configuration options for all reflection converters
    /// - Provides priority-based type matching for converter selection
    /// - Manages cascading serialization and population settings
    /// - Establishes the contract for type-specific conversion operations
    ///
    /// Architecture:
    /// - Generic type parameter T constrains the converter to handle specific types or type families
    /// - Virtual properties allow derived classes to customize behavior
    /// - Priority system ensures the most appropriate converter is selected for each type
    /// - Cascade controls enable fine-grained control over recursive operations
    ///
    /// The converter system uses a Chain of Responsibility pattern where multiple converters
    /// are registered and evaluated based on their priority scores for handling specific types.
    /// Higher priority converters are preferred when multiple converters can handle a type.
    /// </summary>
    /// <typeparam name="T">The base type that this converter is designed to handle.</typeparam>
    public abstract partial class BaseReflectionConverter<T> : IReflectionConverter
    {
        protected const int MAX_DEPTH = 10000;
        protected const int CACHE_CAPACITY = 10000;

        // Cache for serializable fields: (Type, BindingFlags) -> FieldInfo[]
        // Instance-based cache to support derived classes with different GetSerializableFieldsInternal implementations
        private readonly LruCache<(Type, BindingFlags), FieldInfo[]> _serializableFieldsCache = new(CACHE_CAPACITY);

        // Cache for serializable properties: (Type, BindingFlags) -> PropertyInfo[]
        // Instance-based cache to support derived classes with different GetSerializablePropertiesInternal implementations
        private readonly LruCache<(Type, BindingFlags), PropertyInfo[]> _serializablePropertiesCache = new(CACHE_CAPACITY);

        // Cache for serializable member names (for error messages): (Type, BindingFlags) -> (fieldNames, propertyNames)
        // Instance-based cache because it relies on virtual methods (GetSerializableFields, GetSerializableProperties, etc.)
        private readonly LruCache<(Type, BindingFlags), (List<string> fieldNames, List<string> propertyNames)> _serializableMemberNamesCache = new(CACHE_CAPACITY);


        /// <summary>
        /// Clears the reflection metadata caches used by this converter instance.
        /// </summary>
        /// <remarks>
        /// This method is provided for API consistency with other caching utilities such as
        /// <see cref="TypeUtils.ClearTypeCache"/> and
        /// <see cref="TypeUtils.ClearEnumerableItemTypeCache"/>, and allows callers to
        /// explicitly release cached reflection data in long-running or memory-sensitive scenarios.
        /// </remarks>
        public void ClearReflectionCache(ILogger? logger = null)
        {
            logger?.LogDebug("Clearing reflection caches: {_serializableFieldsCacheCount} field entries, {_serializablePropertiesCacheCount} property entries, {_serializableMemberNamesCacheCount} member name entries.",
                _serializableFieldsCache.Count,
                _serializablePropertiesCache.Count,
                _serializableMemberNamesCache.Count);
            _serializableFieldsCache.Clear();
            _serializablePropertiesCache.Clear();
            _serializableMemberNamesCache.Clear();
        }

        /// <summary>
        /// Gets a value indicating whether this converter supports direct value setting operations.
        /// When true, the converter can handle primitive-style value assignments.
        /// When false, the converter only supports field and property-based population.
        /// </summary>
        public virtual bool AllowSetValue => true;

        /// <summary>
        /// Gets a value indicating whether this converter supports cascading serialization operations.
        /// When true, nested objects and collections are recursively serialized.
        /// When false, only shallow serialization is performed.
        /// </summary>
        public virtual bool AllowCascadeSerialization => true;

        /// <summary>
        /// Gets a value indicating whether this converter should recursively convert field values.
        /// When true, field values that are complex objects are serialized recursively.
        /// When false, field values are serialized as simple JSON representations.
        /// </summary>
        public virtual bool AllowCascadeFieldsConversion => true;

        /// <summary>
        /// Gets a value indicating whether this converter should recursively convert property values.
        /// When true, property values that are complex objects are serialized recursively.
        /// When false, property values are serialized as simple JSON representations.
        /// </summary>
        public virtual bool AllowCascadePropertiesConversion => true;

        /// <summary>
        /// Gets a value indicating whether this converter should access pointer fields.
        /// When true, the converter can access pointer fields.
        /// When false, the converter can only access public fields.
        /// </summary>
        public virtual bool AllowPointerFieldsAccess => true;

        /// <summary>
        /// Gets a value indicating whether this converter should access pointer properties.
        /// When true, the converter can access pointer properties.
        /// When false, the converter can only access public properties.
        /// </summary>
        public virtual bool AllowPointerPropertiesAccess => true;

        /// <summary>
        /// Calculates the priority score for this converter when handling the specified type.
        /// Higher scores indicate stronger compatibility and preference for handling the type.
        /// This method implements a distance-based scoring system where closer type relationships
        /// result in higher priority scores.
        ///
        /// Scoring Logic:
        /// - Exact type match (T == type): Returns MAX_DEPTH + 1 (highest priority)
        /// - Inheritance relationship: Returns MAX_DEPTH minus inheritance distance
        /// - No relationship: Returns 0 (cannot handle the type)
        ///
        /// The inheritance distance calculation considers the number of steps in the inheritance
        /// hierarchy between the converter's target type T and the requested type. This ensures
        /// that the most specific converter is selected when multiple converters can handle a type.
        /// </summary>
        /// <param name="type">The type to evaluate for compatibility with this converter.</param>
        /// <param name="logger">Optional logger for tracing priority calculation operations.</param>
        /// <returns>A priority score where higher values indicate stronger compatibility.</returns>
        public virtual int SerializationPriority(Type type, ILogger? logger = null)
        {
            if (type == typeof(T))
                return MAX_DEPTH + 1;

            var distance = TypeUtils.GetInheritanceDistance(baseType: typeof(T), targetType: type);

            return distance >= 0
                ? MAX_DEPTH - distance
                : 0;
        }

        /// <summary>
        /// Gets the serializable fields for the specified type.
        /// Results are cached for performance. Uses <see cref="GetSerializableFieldsInternal"/>
        /// which can be overridden by derived classes to customize field selection.
        /// The <see cref="GetIgnoredFields"/> filter is applied and baked into the cache.
        /// </summary>
        /// <remarks>
        /// Results are cached to improve performance for repeated lookups of the same type.
        /// Use <see cref="ClearReflectionCache"/> to clear the cache if needed.
        /// </remarks>
        public IEnumerable<FieldInfo>? GetSerializableFields(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var cacheKey = (objType, flags);
            var cached = _serializableFieldsCache.GetOrAdd(cacheKey, key =>
            {
                var ignoredFields = GetIgnoredFields();
                return GetSerializableFieldsInternal(reflector, key.Item1, key.Item2, logger)
                    ?.Where(field => !ignoredFields.Contains(field.Name))
                    .ToArray() ?? Array.Empty<FieldInfo>();
            });

            return cached.Length > 0 ? cached : null;
        }

        /// <summary>
        /// Gets the serializable fields for the specified type.
        /// Default implementation returns public fields that are not marked with [Obsolete] or [NonSerialized].
        /// Derived classes can override to customize field selection.
        /// </summary>
        protected virtual IEnumerable<FieldInfo>? GetSerializableFieldsInternal(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var result = objType.GetFields(flags)
                .Where(field => field.IsPublic)
                .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null);

            if (!AllowPointerFieldsAccess)
                result = result.Where(field => !field.FieldType.IsPointer);

            return result;
        }

        /// <summary>
        /// Gets the serializable properties for the specified type.
        /// Results are cached for performance. Uses <see cref="GetSerializablePropertiesInternal"/>
        /// which can be overridden by derived classes to customize property selection.
        /// The <see cref="GetIgnoredProperties"/> filter is applied and baked into the cache.
        /// </summary>
        /// <remarks>
        /// Results are cached to improve performance for repeated lookups of the same type.
        /// Use <see cref="ClearReflectionCache"/> to clear the cache if needed.
        /// </remarks>
        public IEnumerable<PropertyInfo>? GetSerializableProperties(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var cacheKey = (objType, flags);
            var cached = _serializablePropertiesCache.GetOrAdd(cacheKey, key =>
            {
                var ignoredProperties = GetIgnoredProperties();
                return GetSerializablePropertiesInternal(reflector, key.Item1, key.Item2, logger)
                    ?.Where(prop => !ignoredProperties.Contains(prop.Name))
                    .ToArray() ?? Array.Empty<PropertyInfo>();
            });

            return cached.Length > 0 ? cached : null;
        }

        /// <summary>
        /// Gets the serializable properties for the specified type.
        /// Default implementation returns readable properties that are not marked with [Obsolete].
        /// Derived classes can override to customize property selection.
        /// </summary>
        protected virtual IEnumerable<PropertyInfo>? GetSerializablePropertiesInternal(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var result = objType.GetProperties(flags)
                .Where(prop => prop.CanRead)
                .Where(prop => prop.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(prop => prop.GetIndexParameters().Length == 0); // Filter out indexer properties

            if (!AllowPointerPropertiesAccess)
                result = result.Where(prop => !prop.PropertyType.IsPointer);

            return result;
        }

        public virtual IEnumerable<string> GetAdditionalSerializableFields(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            return Enumerable.Empty<string>();
        }

        public virtual IEnumerable<string> GetAdditionalSerializableProperties(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            return Enumerable.Empty<string>();
        }
    }
}