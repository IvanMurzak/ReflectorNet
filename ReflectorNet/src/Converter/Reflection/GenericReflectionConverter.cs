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
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public partial class GenericReflectionConverter<T> : NotArrayReflectionConverter<T>
    {
        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            if (obj == null)
                return SerializedMember.FromJson(type, json: null, name: name);

            var isStruct = type.IsValueType && !type.IsPrimitive && !type.IsEnum;
            if (type.IsClass || isStruct)
            {
                return recursive
                    ? new SerializedMember()
                    {
                        name = name,
                        typeName = type.GetTypeName(pretty: false) ?? string.Empty,
                        fields = base.SerializeFields(reflector, obj, flags, depth: depth, logs: logs, logger: logger, context: context),
                        props = base.SerializeProperties(reflector, obj, flags, depth: depth, logs: logs, logger: logger, context: context),
                        valueJsonElement = new JsonObject().ToJsonElement()
                    }
                    : SerializedMember.FromJson(type, obj.ToJson(reflector), name: name);
            }
            throw new ArgumentException($"Unsupported type: '{type.GetTypeName(pretty: false)}' for converter '{GetType().GetTypeShortName()}'.");
        }
        // GetSerializableFields and GetSerializableProperties inherited from BaseReflectionConverter

        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            JsonElement? value,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            // TODO: This place ignores possibility to parse json as SerializedMember or SerializedMemberList.
            // Need to be sure it won't make any issues.
            var parsedValue = value.Deserialize(type, reflector);

            Print.SetNewValue(ref obj, ref parsedValue, type, depth, logs);
            obj = parsedValue;
            return true;
        }

        public override bool SetField(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            FieldInfo fieldInfo,
            SerializedMember? value,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!TryDeserializeValue(
                reflector,
                data: value,
                result: out var parsedValue,
                type: out var type,
                fallbackType: fallbackType,
                depth: depth,
                logs: logs,
                logger: logger))
            {
                logs?.Error($"Failed to deserialize value for field '{fieldInfo.Name}'.", depth);
                return false;
            }

            // Check if field type matches parsed value type
            if (!TypeUtils.IsCastable(type, fieldInfo.FieldType))
            {
                logs?.Error($"Parsed value type '{type.GetTypeName(pretty: false)}' is not assignable to field type '{fieldInfo.FieldType.GetTypeName(pretty: false)}' for field '{fieldInfo.Name}'.", depth);
                return false;
            }

            // TODO: Print previous and new value in logs
            fieldInfo.SetValue(obj, parsedValue);
            if (logs != null)
                logs.Success($"Field '{fieldInfo.Name}' modified to '{parsedValue}'.", depth);
            return true;
        }

        public override bool SetProperty(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            PropertyInfo propertyInfo,
            SerializedMember? value,
            int depth = 0,
            Logs? logs = null,
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

            if (!TryDeserializeValue(
                reflector,
                data: value,
                result: out var parsedValue,
                type: out var type,
                fallbackType: fallbackType,
                depth: depth,
                logs: logs,
                logger: logger))
            {
                logs?.Error($"Failed to deserialize value for property '{propertyInfo.Name}'.", depth);
                return false;
            }

            // Check if property type matches parsed value type
            if (!TypeUtils.IsCastable(type, propertyInfo.PropertyType))
            {
                logs?.Error($"Parsed value type '{type.GetTypeName(pretty: false)}' is not assignable to property type '{propertyInfo.PropertyType.GetTypeName(pretty: false)}' for property '{propertyInfo.Name}'.", depth);
                return false;
            }

            // TODO: Print previous and new value in logs
            propertyInfo.SetValue(obj, parsedValue);
            if (logs != null)
                logs.Success($"Property '{propertyInfo.Name}' modified to '{parsedValue}'.", depth);
            return true;
        }
    }
}