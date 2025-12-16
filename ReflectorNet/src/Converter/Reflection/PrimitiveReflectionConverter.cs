/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public partial class PrimitiveReflectionConverter : NotArrayReflectionConverter<object>
    {
        public override bool AllowCascadeSerialization => false;

        public override int SerializationPriority(Type type, ILogger? logger = null)
        {
            var isPrimitive = TypeUtils.IsPrimitive(type);

            return isPrimitive
                ? MAX_DEPTH + 1
                : 0;
        }
        protected override SerializedMember InternalSerialize(Reflector reflector, object? obj, Type type, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0, Logs? logs = null,
            ILogger? logger = null, SerializationContext? context = null)
        {
            if (obj == null)
                return SerializedMember.FromJson(type, json: null, name: name);

            return SerializedMember.FromValue(reflector, type, obj, name: name);
        }

        public override IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => null;

        public override IEnumerable<PropertyInfo>? GetSerializableProperties(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => null;

        protected override bool SetValue(Reflector reflector, ref object? obj, Type type, JsonElement? value, int depth = 0, Logs? logs = null, ILogger? logger = null)
        {
            var parsedValue = value.Deserialize(type, reflector);
            Print.SetNewValue(ref obj, ref parsedValue, type, depth, logs, logger);
            obj = parsedValue;
            return true;
        }

        public override bool SetField(Reflector reflector, ref object? obj, Type fallbackType, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, Logs? logs = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!TryDeserializeValue(reflector, value, out var parsedValue, out var type, fallbackType: fallbackType, depth: depth, logs: logs, logger: logger))
            {
                logs?.Error($"Failed to deserialize value for field '{value?.name.ValueOrNull()}'.", depth);
                return false;
            }

            // Check if field type matches parsed value type
            if (!TypeUtils.IsCastable(type, fieldInfo.FieldType))
            {
                logs?.Error($"Parsed value type '{type.GetTypeName(pretty: false)}' is not assignable to field type '{fieldInfo.FieldType.GetTypeName(pretty: false)}' for field '{fieldInfo.Name}'.", depth);
                return false;
            }

            fieldInfo.SetValue(obj, parsedValue);
            logs?.Success($"Field '{fieldInfo.Name}' modified to '{parsedValue}'.", depth);
            return true;
        }

        public override bool SetProperty(Reflector reflector, ref object? obj, Type fallbackType, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, Logs? logs = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            // Check if property is writable
            if (!propertyInfo.CanWrite)
            {
                logs?.Error($"Property '{propertyInfo.Name}' is read-only.", depth);
                return false;
            }

            if (!TryDeserializeValue(reflector, value, out var parsedValue, out var type, fallbackType: fallbackType, depth: depth, logs: logs, logger: logger))
            {
                logs?.Error($"Failed to deserialize value for property '{value?.name.ValueOrNull()}'.", depth);
                return false;
            }

            // Check if property type matches parsed value type
            if (!TypeUtils.IsCastable(type, propertyInfo.PropertyType))
            {
                logs?.Error($"Parsed value type '{type.GetTypeName(pretty: false)}' is not assignable to property type '{propertyInfo.PropertyType.GetTypeName(pretty: false)}' for property '{propertyInfo.Name}'.", depth);
                return false;
            }

            propertyInfo.SetValue(obj, parsedValue);
            logs?.Success($"Property '{propertyInfo.Name}' modified to '{parsedValue}'.", depth);
            return true;
        }
    }
}