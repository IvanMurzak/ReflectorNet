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
    /// <summary>
    /// Provides a comprehensive reflection-based serialization and deserialization framework for .NET objects.
    /// The Reflector class serves as the main entry point for converting objects to/from serialized representations
    /// using a flexible, extensible converter chain system.
    ///
    /// Core Functionality:
    /// - Serialization: Converts objects to SerializedMember representations with type preservation
    /// - Deserialization: Reconstructs objects from SerializedMember data with flexible type resolution
    /// - Population: In-place updates of existing objects with serialized data
    /// - Introspection: Discovery of serializable fields and properties for a given type
    /// - Error Handling: Comprehensive validation and detailed error reporting with hierarchical formatting
    ///
    /// Architecture:
    /// - Chain of Responsibility: Uses registered converter chains for serialization, deserialization, and population
    /// - Extensibility: Supports custom converters for specialized types and serialization logic
    /// - Type Safety: Performs extensive type validation and compatibility checking
    /// - Logging Integration: Built-in support for Microsoft.Extensions.Logging throughout operations
    /// - Singleton Pattern: Provides static Instance property for global access while allowing multiple instances
    ///
    /// Key Features:
    /// - Automatic type detection with manual override support
    /// - Recursive serialization of nested objects and collections
    /// - Flexible BindingFlags control for member visibility (public, private, static, instance)
    /// - Null-safe operations with appropriate default value handling
    /// - Hierarchical error reporting with depth-based indentation
    /// - Property-specific population for fine-grained deserialization control
    /// - Support for both complete deserialization and incremental population scenarios
    ///
    /// The class is designed as a partial class to allow for extension and modularization of functionality.
    /// It maintains a Registry of converters that handle the actual serialization/deserialization logic,
    /// making the system highly extensible and customizable for different object types and scenarios.
    /// </summary>
    public partial class Reflector
    {
        public Registry Converters { get; }

        public Reflector()
        {
            Converters = new Registry();
            jsonSerializer = new(this);
        }

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
                        logger.LogTrace($"{StringUtils.GetPadding(depth)} Serialize. Type '{type.GetTypeId()}' is blacklisted, skipping.");
                    return SerializedMember.Null(type, name);
                }

                var converter = Converters.GetConverter(type);
                if (converter == null)
                    throw new ArgumentException($"Failed to serialize '{name.ValueOrNull()}'. Type '{type.GetTypeId().ValueOrNull()}' not supported for serialization.");

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{StringUtils.GetPadding(depth)} Serialize '{name.ValueOrNull()}' of type '{type.GetTypeId()}'. Converter: {converter.GetType().GetTypeShortName()}");

                return converter.Serialize(
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
                    logger?.LogError($"{padding}{error}");
                    logs?.Error(error ?? "Unknown error", depth);

                    throw new ArgumentException(error);
                }

                if (Converters.IsTypeBlacklisted(type))
                {
                    logger?.LogTrace($"{padding} Deserialize. Type '{type.GetTypeShortName()}' is blacklisted, skipping.");
                    return GetDefaultValue(type);
                }

                var converter = Converters.GetConverter(type);
                if (converter == null)
                    throw new ArgumentException($"[Error] Type '{type?.GetTypeId().ValueOrNull()}' not supported for deserialization.");

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Launch} Deserialize type='{type.GetTypeShortName()}' name='{name.ValueOrNull()}' converter='{converter.GetType().GetTypeShortName()}'");

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

        private object? ResolveReference(
            SerializedMember data,
            DeserializationContext context,
            int depth,
            Logs? logs,
            ILogger? logger)
        {
            var padding = StringUtils.GetPadding(depth);

            // Parse the $ref path from valueJsonElement
            if (data.valueJsonElement == null)
            {
                if (logger?.IsEnabled(LogLevel.Warning) == true)
                    logger.LogWarning($"{padding}{Consts.Emoji.Warn} Reference has no value");
                logs?.Warning("Reference has no value", depth);
                return null;
            }

            if (!data.valueJsonElement.Value.TryGetProperty(JsonSchema.Ref, out var refElement))
            {
                if (logger?.IsEnabled(LogLevel.Warning) == true)
                    logger.LogWarning($"{padding}{Consts.Emoji.Warn} Reference value missing {JsonSchema.Ref} property");
                logs?.Warning($"Reference value missing {JsonSchema.Ref} property", depth);
                return null;
            }

            var refPath = refElement.GetString();
            if (string.IsNullOrEmpty(refPath))
            {
                if (logger?.IsEnabled(LogLevel.Warning) == true)
                    logger.LogWarning($"{padding}{Consts.Emoji.Warn} Reference path is null or empty");
                logs?.Warning("Reference path is null or empty", depth);
                return null;
            }

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}{Consts.Emoji.Start} Resolving reference: {refPath}");

            if (context.TryResolve(refPath, out var resolved))
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Done} Resolved reference to object of type: {resolved?.GetType().GetTypeShortName()}");
                return resolved;
            }

            // Reference not yet resolved - this could happen with forward references
            if (logger?.IsEnabled(LogLevel.Warning) == true)
                logger.LogWarning($"{padding}{Consts.Emoji.Warn} Unable to resolve reference: {refPath}");
            logs?.Warning($"Unable to resolve reference: {refPath}", depth);
            return null;
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
        /// <param name="depth">The current depth level in the object hierarchy, used for error message indentation. Default is 0.</param>
        /// <param name="stringBuilder">Optional StringBuilder to accumulate error messages and status information. A new one is created if not provided.</param>
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
        /// <param name="depth">The current depth level in the object hierarchy, used for error message indentation. Default is 0.</param>
        /// <param name="stringBuilder">Optional StringBuilder to accumulate error messages and status information. A new one is created if not provided.</param>
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
