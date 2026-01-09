/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * SPDX-License-Identifier: Apache-2.0
 * Copyright (c) 2024-2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        /// <summary>
        /// Deserializes a SerializedMember to an object.
        /// This method provides flexible deserialization with optional type fallback support.
        ///
        /// Behavior:
        /// - If data is null and type is provided: returns the default value of the specified type
        /// - If data is null and no type provided: throws ArgumentException
        /// - If data.typeName is provided and not empty: uses data.typeName for type resolution (original behavior)
        /// - If data.typeName is null/empty but type parameter is provided: uses the provided type as fallback
        /// - If both data.typeName is null/empty and no type parameter provided: throws ArgumentException
        ///
        /// This allows for more flexible deserialization scenarios where type information might come
        /// from different sources or where default values are needed for missing data.
        /// </summary>
        /// <param name="data">The SerializedMember containing the serialized data. Can be null if type is provided.</param>
        /// <param name="depth">The current depth level in the object hierarchy, used for error message indentation. Default is 0.</param>
        /// <param name="stringBuilder">Optional StringBuilder to accumulate error messages and status information. A new one is created if not provided.</param>
        /// <param name="logger">Optional logger for tracing deserialization operations.</param>
        /// <returns>The deserialized object, or the default value of the type if data is null and type is provided.</returns>
        /// <exception cref="ArgumentException">Thrown when both data and type are null, or when type resolution fails.</exception>
        public T? Deserialize<T>(
            SerializedMember data,
            string? fallbackName = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null) where T : class
        {
            return Deserialize(
                data,
                fallbackType: typeof(T),
                fallbackName: fallbackName,
                depth: depth,
                logs: logs,
                logger: logger,
                context: context) as T;
        }

        /// <summary>
        /// Deserializes a SerializedMember to an object.
        /// This method provides flexible deserialization with optional type fallback support.
        ///
        /// Behavior:
        /// - If data is null and type is provided: returns the default value of the specified type
        /// - If data is null and no type provided: throws ArgumentException
        /// - If data.typeName is provided and not empty: uses data.typeName for type resolution (original behavior)
        /// - If data.typeName is null/empty but type parameter is provided: uses the provided type as fallback
        /// - If both data.typeName is null/empty and no type parameter provided: throws ArgumentException
        ///
        /// This allows for more flexible deserialization scenarios where type information might come
        /// from different sources or where default values are needed for missing data.
        /// </summary>
        /// <param name="data">The SerializedMember containing the serialized data. Can be null if type is provided.</param>
        /// <param name="fallbackType">Optional type to use as fallback when data.typeName is missing or when data is null.</param>
        /// <param name="depth">The current depth level in the object hierarchy, used for error message indentation. Default is 0.</param>
        /// <param name="stringBuilder">Optional StringBuilder to accumulate error messages and status information. A new one is created if not provided.</param>
        /// <param name="logger">Optional logger for tracing deserialization operations.</param>
        /// <returns>The deserialized object, or the default value of the type if data is null and type is provided.</returns>
        /// <exception cref="ArgumentException">Thrown when both data and type are null, or when type resolution fails.</exception>
        public object? Deserialize(
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null)
        {
            if (data == null)
            {
                // If data is null and type is provided, return default value of the type
                if (fallbackType != null)
                    return GetDefaultValue(fallbackType);

                // If data is null and no type provided, throw exception
                throw new ArgumentException(Error.DataTypeIsEmpty());
            }

            var padding = StringUtils.GetPadding(depth);
            var name = StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name;

            // Create context at root level
            context ??= new DeserializationContext();

            // Check for reference type BEFORE normal deserialization
            if (data.typeName == JsonSchema.Reference)
            {
                return ResolveReference(data, context, depth, logs, logger);
            }

            // Enter the current path segment for tracking
            context.Enter(name);

            try
            {
                var type = TypeUtils.GetTypeWithNamePriority(data, fallbackType, out var error);
                if (type == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError("{padding}{error}", padding, error ?? "Unknown error");
                    logs?.Error(error ?? "Unknown error", depth);

                    throw new ArgumentException(error);
                }

                if (Converters.IsTypeBlacklisted(type))
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}Deserialize. Type '{type.GetTypeId()}' is blacklisted, skipping.");
                    return GetDefaultValue(type);
                }

                var jsonConverter = JsonSerializer.GetJsonConverter(type);
                if (jsonConverter != null)
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}{Consts.Emoji.Launch} Deserialize type='{type.GetTypeId()}' name='{name.ValueOrNull()}' JsonConverter: {jsonConverter.GetType().GetTypeShortName()}");

                    return data.valueJsonElement.Deserialize(type, this);
                }

                var converter = Converters.GetConverter(type);
                if (converter == null)
                {
                    if (type.IsInterface) // Interfaces cannot be instantiated
                    {
                        if (data.IsNull())
                            return null; // return null for interface types when data is null

                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding}{Consts.Emoji.Launch} Deserialize type='{type.GetTypeId()}' name='{name.ValueOrNull()}'. No converter can handle interface types with not null values.");

                        throw new TypeInstantiationException($"Cannot deserialize interface type '{type.GetTypeId()}'", type);
                    }
                    if (type.IsAbstract) // Abstract classes cannot be instantiated
                    {
                        if (data.IsNull())
                            return null; // return null for abstract types when data is null

                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding}{Consts.Emoji.Launch} Deserialize type='{type.GetTypeId()}' name='{name.ValueOrNull()}'. No converter can handle abstract types with not null values.");

                        throw new TypeInstantiationException($"Cannot deserialize abstract type '{type.GetTypeId()}'", type);
                    }

                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{Consts.Emoji.Launch} Deserialize type='{type.GetTypeId()}' name='{name.ValueOrNull()}'. No converter found for type.");

                    throw new ArgumentException($"Type '{type.GetTypeId().ValueOrNull()}' not supported for deserialization.");
                }

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Launch} Deserialize type='{type.GetTypeId()}' name='{name.ValueOrNull()}' converter='{converter.GetType().GetTypeShortName()}'");

                var result = converter.Deserialize(
                    this,
                    data,
                    type,
                    fallbackName,
                    depth: depth + 1,
                    logs: logs,
                    logger: logger,
                    context: context
                );

                return result;
            }
            finally
            {
                context.Exit(name);
            }
        }
    }
}
