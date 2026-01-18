/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public interface IReflectionConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter supports direct value setting operations.
        /// When true, the converter can handle primitive-style value assignments.
        /// When false, the converter only supports field and property-based population.
        /// </summary>
        bool AllowSetValue { get; }

        /// <summary>
        /// Gets a value indicating whether this converter supports cascading serialization operations.
        /// When true, nested objects and collections are recursively serialized.
        /// When false, only shallow serialization is performed.
        /// </summary>
        bool AllowCascadeSerialization { get; }

        /// <summary>
        /// Gets a value indicating whether this converter should recursively convert field values.
        /// When true, field values that are complex objects are serialized recursively.
        /// When false, field values are serialized as simple JSON representations.
        /// </summary>
        bool AllowCascadeFieldsConversion { get; }

        /// <summary>
        /// Gets a value indicating whether this converter should recursively convert property values.
        /// When true, property values that are complex objects are serialized recursively.
        /// When false, property values are serialized as simple JSON representations.
        /// </summary>
        bool AllowCascadePropertiesConversion { get; }

        int SerializationPriority(Type type, ILogger? logger = null);

        object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null);

        SerializedMember Serialize(
            Reflector reflector,
            object? obj,
            Type? fallbackType = null,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null);

        bool TryPopulate(
            Reflector reflector,
            ref object? obj,
            SerializedMember data,
            Type type,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);

        bool SetField(
            Reflector reflector,
            ref object? obj,
            Type type,
            FieldInfo fieldInfo,
            SerializedMember? value,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);

        bool SetProperty(
            Reflector reflector,
            ref object? obj,
            Type type,
            PropertyInfo propertyInfo,
            SerializedMember? value,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);

        IEnumerable<FieldInfo>? GetSerializableFields(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);

        IEnumerable<PropertyInfo>? GetSerializableProperties(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);

        IEnumerable<string> GetAdditionalSerializableFields(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);

        IEnumerable<string> GetAdditionalSerializableProperties(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);

        object? CreateInstance(Reflector reflector, Type type);
        object? GetDefaultValue(Reflector reflector, Type type);
    }
}