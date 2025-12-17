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
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public partial class ArrayReflectionConverter : BaseReflectionConverter<Array>
    {
        protected virtual bool IsGenericList(Type type, out Type? elementType)
        {
            var iList = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

            if (iList == null)
            {
                elementType = null;
                return false;
            }
            elementType = iList.GetGenericArguments()[0];
            return true;
        }

        public override int SerializationPriority(Type type, ILogger? logger = null)
        {
            if (type.IsArray)
                return MAX_DEPTH + 1;

            var isGenericList = IsGenericList(type, out var elementType);
            if (isGenericList)
                return MAX_DEPTH + 1;

            if (type == typeof(string))
                return 0;

            var isArray = typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
            return isArray
                ? MAX_DEPTH / 4
                : 0;
        }

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

            if (recursive)
            {
                int index = 0;
                var enumerable = (System.Collections.IEnumerable)obj;
                var serializedList = new SerializedMemberList();

                // Determine the element type for handling null elements
                var elementType = TypeUtils.GetEnumerableItemType(type);

                foreach (var element in enumerable)
                {
                    serializedList.Add(reflector.Serialize(
                        element,
                        fallbackType: element?.GetType() ?? elementType,
                        name: $"[{index++}]",
                        recursive: recursive,
                        flags: flags,
                        depth: depth,
                        logs: logs,
                        logger: logger,
                        context: context));
                }

                return SerializedMember.FromValue(
                    reflector: reflector,
                    type: type,
                    value: serializedList,
                    name: name);
            }
            else
            {
                // Handle non-recursive serialization
                return SerializedMember.FromJson(
                    type: type,
                    json: obj.ToJson(reflector),
                    name: name);
            }
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
            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{StringUtils.GetPadding(depth)}Set value type='{type.GetTypeShortName()}'. Converter='{GetType().GetTypeShortName()}'.");

            if (!TryDeserializeValueListInternal(
                reflector,
                jsonElement: value,
                type: type,
                result: out var parsedValue,
                depth: depth + 1,
                logs: logs,
                logger: logger))
            {
                Print.FailedToSetNewValue(ref obj, type, depth, logs);
                return false;
            }

            Print.SetNewValueEnumerable(ref obj, ref parsedValue, type, depth, logs);
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
                logs?.Error($"Parsed value type '{type?.GetTypeId().ValueOrNull()}' is not assignable to field type '{fieldInfo.FieldType.GetTypeId()}' for field '{fieldInfo.Name}'.", depth);
                return false;
            }

            fieldInfo.SetValue(obj, parsedValue);
            logs?.Success($"Field '{fieldInfo.Name}' modified to '{parsedValue}'.", depth);
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
            // if (logger?.IsEnabled(LogLevel.Trace) == true)
            //     logger.LogTrace($"{padding}Set property type='{propertyInfo.PropertyType.GetTypeShortName()}', name='{propertyInfo.Name}'. Converter='{GetType().GetTypeShortName()}'.");

            // Check if property is writable
            if (!propertyInfo.CanWrite)
            {
                logs?.Error($"Property '{propertyInfo.Name}' is read-only.", depth);
                return false;
            }

            if (!TryDeserializeValue(reflector,
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
                logs?.Error($"Parsed value type '{type?.GetTypeId().ValueOrNull()}' is not assignable to property type '{propertyInfo.PropertyType.GetTypeId()}' for property '{propertyInfo.Name}'.", depth);
                return false;
            }

            propertyInfo.SetValue(obj, parsedValue);
            logs?.Success($"Property '{propertyInfo.Name}' modified to '{parsedValue}'.", depth);
            return true;
        }
    }
}