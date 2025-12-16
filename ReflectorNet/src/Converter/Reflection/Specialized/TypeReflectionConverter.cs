/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    /// <summary>
    /// Specialized converter for System.Type that serializes types as their fully qualified name
    /// and deserializes them back using TypeUtils.GetType(). This ensures that Type instances
    /// can be serialized and deserialized without data loss while treating them as read-only.
    /// </summary>
    public class TypeReflectionConverter : IgnoreFieldsAndPropertiesReflectionConverter<Type>
    {
        public TypeReflectionConverter() : base(ignoreFields: true, ignoreProperties: true)
        {
        }

        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            if (obj is Type typeObj)
            {
                var typeName = typeObj.GetTypeName(pretty: false);
                return SerializedMember.FromValue(reflector, type, typeName, name: name);
            }

            return base.InternalSerialize(reflector, obj, type, name, recursive, flags, depth, stringBuilder, logger, context);
        }

        public override object? CreateInstance(Reflector reflector, Type type)
        {
            // Return a placeholder - actual deserialization happens in TryDeserializeValueInternal
            return typeof(object);
        }

        protected override bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember data,
            out object? result,
            Type type,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            result = null;

            if (!data.valueJsonElement.HasValue)
            {
                return false;
            }

            try
            {
                var typeName = data.valueJsonElement.Value.GetString();
                if (!string.IsNullOrEmpty(typeName))
                {
                    var deserializedType = TypeUtils.GetType(typeName);
                    if (deserializedType != null)
                    {
                        result = deserializedType;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("Failed to deserialize Type from value '{Value}': {Message}", data.valueJsonElement, ex.Message);
                return false;
            }

            return false;
        }

        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            System.Text.Json.JsonElement? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (!value.HasValue)
            {
                return false;
            }

            try
            {
                var typeName = value.Value.GetString();
                if (!string.IsNullOrEmpty(typeName))
                {
                    var deserializedType = TypeUtils.GetType(typeName);
                    if (deserializedType != null)
                    {
                        obj = deserializedType;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError("Failed to deserialize Type from value '{Value}': {Message}", value, ex.Message);
                return false;
            }

            return false;
        }
    }
}
