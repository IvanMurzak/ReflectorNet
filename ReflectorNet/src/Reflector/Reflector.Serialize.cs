/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * SPDX-License-Identifier: Apache-2.0
 * Copyright (c) 2024-2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        /// <summary>
        /// Serializes an object to a SerializedMember using the registered converter chain.
        /// This method provides flexible serialization with automatic type detection and customizable behavior.
        ///
        /// Behavior:
        /// - Type resolution: Uses provided type parameter, or automatically detects from the object if not specified
        /// - Null handling: For null objects, returns a SerializedMember with null JSON data but preserves type information
        /// - Converter chain: Iterates through registered serializers until one successfully handles the type
        /// - Recursive support: Can serialize nested objects and collections based on the recursive parameter
        /// - Reflection control: Uses BindingFlags to control which fields/properties are serialized
        /// - Logging: Provides detailed tracing of which serializers are used for each type
        ///
        /// The method leverages a chain of responsibility pattern where multiple serializers are tried
        /// in sequence until one successfully handles the given type. This allows for extensible
        /// serialization support for different object types and custom serialization logic.
        /// </summary>
        /// <param name="obj">The object to serialize. Can be null, in which case type information is preserved.</param>
        /// <param name="fallbackType">Optional explicit type to use for serialization. If null, type is inferred from obj.</param>
        /// <param name="name">Optional name to assign to the serialized member for identification purposes.</param>
        /// <param name="recursive">Whether to recursively serialize nested objects and collections. Default is true.</param>
        /// <param name="flags">BindingFlags controlling which fields and properties are included in serialization. Default includes public and non-public instance members.</param>
        /// <param name="depth">The current depth level in the object hierarchy, used for error message indentation. Default is 0.</param>
        /// <param name="stringBuilder">Optional StringBuilder to accumulate error messages and status information. A new one is created if not provided.</param>
        /// <param name="logger">Optional logger for tracing serialization operations and troubleshooting.</param>
        /// <returns>A SerializedMember containing the serialized representation of the object.</returns>
        /// <exception cref="ArgumentException">Thrown when no type can be determined or when no registered serializer supports the type.</exception>
        public SerializedMember Serialize(
            object? obj,
            Type? fallbackType = null,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            int depth = 0, Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            context ??= new SerializationContext();

            if (obj != null && !context.Enter(obj, name))
            {
                var path = context.GetPath(obj);
                return SerializedMember.FromReference(path, name);
            }

            try
            {
                var type = TypeUtils.GetTypeWithObjectPriority(obj, fallbackType, out var error);
                if (type == null)
                    throw new ArgumentException(error);

                if (Converters.IsTypeBlacklisted(type))
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace("{padding}Serialize skip for '{name}' of type '{type}', it is blacklisted type.",
                            StringUtils.GetPadding(depth), name.ValueOrNull(), type.GetTypeId().ValueOrNull());
                    return SerializedMember.Null(type, name);
                }

                // Handle null object case
                if (obj == null)
                {
                    if (type.IsInterface) // Interfaces cannot be instantiated
                    {
                        if (logger?.IsEnabled(LogLevel.Trace) == true)
                            logger.LogTrace("{padding}Serialize null '{name}' of interface type '{type}'.",
                                StringUtils.GetPadding(depth), name.ValueOrNull(), type.GetTypeId().ValueOrNull());

                        return SerializedMember.Null(type, name);
                    }
                    if (type.IsAbstract) // Abstract classes cannot be instantiated
                    {
                        if (logger?.IsEnabled(LogLevel.Trace) == true)
                            logger.LogTrace("{padding}Serialize null '{name}' of abstract type '{type}'.",
                                StringUtils.GetPadding(depth), name.ValueOrNull(), type.GetTypeId().ValueOrNull());

                        return SerializedMember.Null(type, name);
                    }
                }

                var jsonConverter = JsonSerializer.GetJsonConverter(type);
                if (jsonConverter != null)
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace("{padding}Serialize '{name}' of type '{type}'. JsonConverter: {converter}",
                            StringUtils.GetPadding(depth), name.ValueOrNull(), type.GetTypeId().ValueOrNull(), jsonConverter.GetType().GetTypeId().ValueOrNull());

                    return SerializedMember.FromJson(
                        type: type,
                        json: obj.ToJson(this, depth: depth, logger: logger),
                        name: name);
                }

                var reflectionConverter = Converters.GetConverter(type);

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace("{padding}Serialize '{name}' of type '{type}'. ReflectionConverter: {converter}",
                        StringUtils.GetPadding(depth), name.ValueOrNull(), type.GetTypeId().ValueOrNull(), reflectionConverter?.GetType().GetTypeShortName()?.ValueOrNull());

                if (reflectionConverter == null)
                    throw new ArgumentException($"Failed to serialize '{name.ValueOrNull()}'. Type '{type.GetTypeId().ValueOrNull()}' not supported for serialization.");

                return reflectionConverter.Serialize(
                    this,
                    obj,
                    fallbackType: type,
                    name: name,
                    recursive,
                    flags,
                    depth: depth,
                    logs: logs,
                    logger: logger,
                    context: context);
            }
            finally
            {
                if (obj != null)
                {
                    context.Exit(obj, name);
                }
            }
        }

        /// <summary>
        /// Retrieves all serializable fields from the specified type using the registered deserializer chain.
        /// This method delegates to the appropriate deserializer to determine which fields should be included
        /// in serialization operations based on the type's specific serialization rules.
        ///
        /// Behavior:
        /// - Uses the deserializer chain to find the appropriate handler for the given type
        /// - Each deserializer defines its own criteria for what constitutes a serializable field
        /// - Respects BindingFlags to control field visibility (public, private, static, instance, etc.)
        /// - Returns null if no deserializer is found for the type
        /// - Provides logging support for troubleshooting field discovery
        ///
        /// This method is particularly useful for introspection scenarios where you need to understand
        /// what fields will be serialized for a given type, or for building custom serialization logic
        /// that needs to iterate over the same fields that the serializer would process.
        /// </summary>
        /// <param name="type">The type to analyze for serializable fields.</param>
        /// <param name="flags">BindingFlags controlling which fields are considered (public, private, static, instance, etc.). Default includes public and non-public instance fields.</param>
        /// <param name="logger">Optional logger for tracing field discovery operations.</param>
        /// <returns>An enumerable of FieldInfo objects representing serializable fields, or null if no deserializer supports the type.</returns>
        public IEnumerable<FieldInfo>? GetSerializableFields(
            Type type,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            return Converters.GetConverter(type)
                ?.GetSerializableFields(this, type, flags, logger);
        }

        /// <summary>
        /// Retrieves all serializable properties from the specified type using the registered deserializer chain.
        /// This method delegates to the appropriate deserializer to determine which properties should be included
        /// in serialization operations based on the type's specific serialization rules.
        ///
        /// Behavior:
        /// - Uses the deserializer chain to find the appropriate handler for the given type
        /// - Each deserializer defines its own criteria for what constitutes a serializable property
        /// - Respects BindingFlags to control property visibility and accessibility (public, private, readable, writable, etc.)
        /// - Returns null if no deserializer is found for the type
        /// - Provides logging support for troubleshooting property discovery
        ///
        /// This method is particularly useful for introspection scenarios where you need to understand
        /// what properties will be serialized for a given type, or for building custom serialization logic
        /// that needs to iterate over the same properties that the serializer would process.
        /// </summary>
        /// <param name="type">The type to analyze for serializable properties.</param>
        /// <param name="flags">BindingFlags controlling which properties are considered (public, private, static, instance, etc.). Default includes public and non-public instance properties.</param>
        /// <param name="logger">Optional logger for tracing property discovery operations.</param>
        /// <returns>An enumerable of PropertyInfo objects representing serializable properties, or null if no deserializer supports the type.</returns>
        public IEnumerable<PropertyInfo>? GetSerializableProperties(
            Type type,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            return Converters.GetConverter(type)
                ?.GetSerializableProperties(this, type, flags, logger);
        }
    }
}
