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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public partial class GenericReflectionConvertor<T> : NotArrayReflectionConvertor<T>
    {
        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
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
                        fields = base.SerializeFields(reflector, obj, flags, depth: depth, stringBuilder: stringBuilder, logger: logger),
                        props = base.SerializeProperties(reflector, obj, flags, depth: depth, stringBuilder: stringBuilder, logger: logger),
                        valueJsonElement = new JsonObject().ToJsonElement()
                    }
                    : SerializedMember.FromJson(type, obj.ToJson(reflector), name: name);
            }
            throw new ArgumentException($"Unsupported type: '{type.GetTypeName(pretty: false)}' for convertor '{GetType().Name}'.");
        }
        public override IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => objType.GetFields(flags)
                .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(field => field.IsPublic);

        public override IEnumerable<PropertyInfo>? GetSerializableProperties(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => objType.GetProperties(flags)
                .Where(prop => prop.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(prop => prop.CanRead);

        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            JsonElement? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            // TODO: This place ignores possibility to parse json as SerializedMember or SerializedMemberList.
            // Need to be sure it won't make any issues.
            var parsedValue = value.Deserialize(type, reflector);

            Print.SetNewValue(ref obj, ref parsedValue, type, depth, stringBuilder);
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
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!TryDeserializeValue(
                reflector,
                serializedMember: value,
                result: out var parsedValue,
                type: out var type,
                fallbackType: fallbackType,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to deserialize value for field '{fieldInfo.Name}'.");
                return false;
            }
            // TODO: Print previous and new value in stringBuilder
            fieldInfo.SetValue(obj, parsedValue);
            if (stringBuilder != null)
                stringBuilder.AppendLine($"{padding}[Success] Field '{fieldInfo.Name}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetProperty(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            PropertyInfo propertyInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!TryDeserializeValue(
                reflector,
                serializedMember: value,
                result: out var parsedValue,
                type: out var type,
                fallbackType: fallbackType,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to deserialize value for property '{propertyInfo.Name}'.");
                return false;
            }
            // TODO: Print previous and new value in stringBuilder
            propertyInfo.SetValue(obj, parsedValue);
            if (stringBuilder != null)
                stringBuilder.AppendLine($"{padding}[Success] Property '{propertyInfo.Name}' modified to '{parsedValue}'.");
            return true;
        }
    }
}