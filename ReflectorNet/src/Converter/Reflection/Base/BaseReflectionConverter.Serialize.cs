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
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public abstract partial class BaseReflectionConverter<T> : IReflectionConverter
    {
        protected virtual IEnumerable<string> GetIgnoredFields() => Enumerable.Empty<string>();
        protected virtual IEnumerable<string> GetIgnoredProperties() => Enumerable.Empty<string>();

        public virtual SerializedMember Serialize(
            Reflector reflector,
            object? obj,
            Type? type = null,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            return InternalSerialize(reflector, obj,
                type: type ?? obj?.GetType() ?? typeof(T),
                name: name,
                recursive: recursive,
                flags: flags,
                depth: depth,
                logs: logs,
                logger: logger,
                context: context);
        }

        protected virtual SerializedMemberList? SerializeFields(
            Reflector reflector,
            object obj,
            BindingFlags flags,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            var serializedFields = default(SerializedMemberList);
            var objType = obj.GetType();

            var fields = GetSerializableFields(reflector, objType, flags, logger);
            if (fields == null)
                return null;

            foreach (var field in fields)
            {
                if (GetIgnoredFields().Contains(field.Name))
                    continue;
                try
                {
                    var value = field.GetValue(obj);
                    var fieldType = field.FieldType;

                    var serialized = reflector.Serialize(value, fieldType,
                        name: field.Name,
                        recursive: AllowCascadeFieldsConversion,
                        flags: flags,
                        depth: depth + 1,
                        logs: logs,
                        logger: logger,
                        context: context);

                    serializedFields ??= new SerializedMemberList();
                    serializedFields.Add(serialized);
                }
                catch (Exception ex)
                {
                    // skip inaccessible field
                    logger?.LogWarning(ex.GetBaseException(), "Failed to serialize field '{fieldName}' of type '{type}' in '{objType}'. Path: {path}",
                         field.Name, field.FieldType.GetTypeId(), objType.GetTypeId(), context?.GetPath(obj));
                }
            }
            return serializedFields;
        }

        protected virtual SerializedMemberList? SerializeProperties(
            Reflector reflector,
            object obj,
            BindingFlags flags,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            var serializedProperties = default(SerializedMemberList);
            var objType = obj.GetType();

            var properties = GetSerializableProperties(reflector, objType, flags, logger);
            if (properties == null)
                return null;

            foreach (var prop in properties)
            {
                if (GetIgnoredProperties().Contains(prop.Name))
                    continue;
                try
                {
                    var value = prop.GetValue(obj);
                    var propType = prop.PropertyType;

                    var serialized = reflector.Serialize(value, propType,
                        name: prop.Name,
                        recursive: AllowCascadePropertiesConversion,
                        flags: flags,
                        depth: depth + 1,
                        logs: logs,
                        logger: logger,
                        context: context);

                    serializedProperties ??= new SerializedMemberList();
                    serializedProperties.Add(serialized);
                }
                catch (Exception ex)
                {
                    // skip inaccessible property
                    logger?.LogWarning(ex.GetBaseException(), "Failed to serialize property '{propertyName}' of type '{type}' in '{objType}'. Path: {path}",
                         prop.Name, prop.PropertyType.GetTypeId(), objType.GetTypeId(), context?.GetPath(obj));
                }
            }
            return serializedProperties;
        }

        protected abstract SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null);
    }
}