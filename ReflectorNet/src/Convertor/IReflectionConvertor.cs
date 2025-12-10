/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public interface IReflectionConvertor
    {
        bool AllowCascadeSerialization { get; }

        int SerializationPriority(Type type, ILogger? logger = null);

        object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
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
            StringBuilder? stringBuilder = null,
            ILogger? logger = null,
            SerializationContext? context = null);

        bool TryPopulate(
            Reflector reflector,
            ref object? obj,
            SerializedMember data,
            Type? fallbackType = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);

        bool SetField(
            Reflector reflector,
            ref object? obj,
            Type type,
            FieldInfo fieldInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null);

        bool SetProperty(
            Reflector reflector,
            ref object? obj,
            Type type,
            PropertyInfo propertyInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
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