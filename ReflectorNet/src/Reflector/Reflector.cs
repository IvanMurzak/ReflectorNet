/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * SPDX-License-Identifier: Apache-2.0
 * Copyright (c) 2024-2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

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
    }
}
