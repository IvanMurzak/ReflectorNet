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
        /// When false, the converter only supports field and property-based modification.
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

        /// <summary>
        /// Gets a value indicating whether this converter should access pointer fields.
        /// When true, the converter can access pointer fields.
        /// When false, pointer-type fields are excluded from serialization while non-pointer fields remain accessible.
        /// </summary>
        bool AllowPointerFieldsAccess { get; }

        /// <summary>
        /// Gets a value indicating whether this converter should access pointer properties.
        /// When true, the converter can access pointer properties.
        /// When false, pointer-type properties are excluded from serialization while non-pointer public properties remain accessible.
        /// </summary>
        bool AllowPointerPropertiesAccess { get; }

        /// <summary>
        /// Gets a value indicating whether a JSON-object patch node targeting <paramref name="type"/>
        /// should be handed to this converter as an atomic value rather than being structurally
        /// descended into (key-by-key) by the JSON Merge Patch engine.
        /// <para>
        /// When <c>false</c> (the default), <see cref="Reflector.TryPatch(ref object?, string, Type?, int, Logs?, BindingFlags, ILogger?)"/>
        /// treats a JSON object as a bag of members and navigates into each key — correct for plain POCOs.
        /// </para>
        /// <para>
        /// When <c>true</c>, a JSON object whose target type is handled by this converter is routed
        /// through <see cref="Reflector.TryModify(ref object?, SerializedMember, Type?, int, Logs?, BindingFlags, ILogger?)"/>
        /// (the same path componentDiff / pathPatches use), so the converter resolves the whole node
        /// itself (e.g. a Unity object reference <c>{"instanceID":"…"}</c> resolved via an asset lookup).
        /// A node carrying a <c>"$type"</c> hint is exempt — polymorphic replacement still flows through
        /// the structural path.
        /// </para>
        /// </summary>
        /// <param name="type">The target type the JSON-object patch node is being applied to.</param>
        /// <returns><c>true</c> to treat a JSON object as an atomic converter value; otherwise <c>false</c>.</returns>
        bool TreatJsonObjectAsAtomicValue(Type type);

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

        bool TryModify(
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