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
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public abstract partial class BaseReflectionConverter<T> : IReflectionConverter
    {
        protected virtual IEnumerable<string> GetIgnoredFields() => Enumerable.Empty<string>();
        protected virtual IEnumerable<string> GetIgnoredProperties() => Enumerable.Empty<string>();

        public virtual SerializedMember Serialize(
            Reflector reflector,
            object? obj,
            Type? fallbackType = null,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            var actualType = fallbackType ?? obj?.GetType() ?? typeof(T);

            return InternalSerialize(
                reflector: reflector,
                obj: obj,
                type: actualType,
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
                var fieldType = field.FieldType;

                if (reflector.Converters.IsTypeBlacklisted(fieldType))
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace("{padding}Skipping serialization of field '{fieldName}' of type '{type}' in '{objType}' because its type is blacklisted.\nPath: {path}",
                            StringUtils.GetPadding(depth + 1), field.Name, fieldType.GetTypeId(), objType.GetTypeId(), context?.GetPath(obj));
                    continue;
                }
                try
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace("{padding}Serializing field '{fieldName}' of type '{type}' in '{objType}'.\nPath: {path}",
                            StringUtils.GetPadding(depth + 1), field.Name, fieldType.GetTypeId(), objType.GetTypeId(), context?.GetPath(obj));

                    var value = field.GetValue(obj);

                    var serialized = reflector.Serialize(
                        obj: value,
                        fallbackType: fieldType,
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
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                        logger.LogWarning(ex.GetBaseException(), "{padding}Failed to serialize field '{fieldName}' of type '{type}' in '{objType}'. Converter: {converter}. Path: {path}",
                             StringUtils.GetPadding(depth + 1), field.Name, fieldType.GetTypeId(), objType.GetTypeId(), GetType().GetTypeShortName(), context?.GetPath(obj));
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
                var propType = prop.PropertyType;

                if (reflector.Converters.IsTypeBlacklisted(propType))
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace("{padding}Skipping serialization of property '{propertyName}' of type '{type}' in '{objType}' because its type is blacklisted.\nPath: {path}",
                            StringUtils.GetPadding(depth + 1), prop.Name, propType.GetTypeId(), objType.GetTypeId(), context?.GetPath(obj));
                    continue;
                }
                try
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace("{padding}Serializing property '{propertyName}' of type '{type}' in '{objType}'.\nPath: {path}",
                            StringUtils.GetPadding(depth + 1), prop.Name, propType.GetTypeId(), objType.GetTypeId(), context?.GetPath(obj));

                    var value = prop.GetValue(obj);

                    var serialized = reflector.Serialize(
                        obj: value,
                        fallbackType: propType,
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
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                        logger.LogWarning(ex.GetBaseException(), "{padding}Failed to serialize property '{propertyName}' of type '{type}' in '{objType}'. Converter: {converter}. Path: {path}",
                             StringUtils.GetPadding(depth + 1), prop.Name, propType.GetTypeId(), objType.GetTypeId(), GetType().GetTypeShortName(), context?.GetPath(obj));
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