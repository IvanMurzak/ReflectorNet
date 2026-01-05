/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
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
        /// Default implementation returns public fields that are not marked with [Obsolete] or [NonSerialized].
        /// Derived classes can override to customize field selection.
        /// </summary>
        public virtual IEnumerable<FieldInfo>? GetSerializableFields(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            return objType.GetFields(flags)
                .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null)
                .Where(field => field.IsPublic)
                .Where(field => GetIgnoredFields().Contains(field.Name) == false);
        }

        /// <summary>
        /// Gets the serializable properties for the specified type.
        /// Default implementation returns readable properties that are not marked with [Obsolete].
        /// Derived classes can override to customize property selection.
        /// </summary>
        public virtual IEnumerable<PropertyInfo>? GetSerializableProperties(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            return objType.GetProperties(flags)
                .Where(prop => prop.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(prop => prop.CanRead)
                .Where(prop => GetIgnoredProperties().Contains(prop.Name) == false);
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