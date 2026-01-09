/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public abstract partial class BaseReflectionConverter<T> : IReflectionConverter
    {
        /// <summary>
        /// Performs comprehensive deserialization of SerializedMember data into strongly-typed objects.
        /// This method serves as the main entry point for converting serialized representations back into
        /// live .NET objects with full type preservation and validation.
        ///
        /// Deserialization Process:
        /// 1. Value Deserialization: Attempts to deserialize the core value using TryDeserializeValue
        /// 2. Field Population: Iterates through serialized fields and applies them to the target object
        /// 3. Property Population: Iterates through serialized properties and applies them to the target object
        /// 4. Type Validation: Ensures field/property types are compatible with target object
        /// 5. Instance Creation: Creates object instances as needed during the deserialization process
        /// 6. Error Handling: Provides comprehensive error reporting with hierarchical formatting
        ///
        /// Field and Property Handling:
        /// - Uses reflection to locate corresponding fields/properties on the target type
        /// - Supports both public and non-public members based on BindingFlags
        /// - Validates writability for properties before attempting to set values
        /// - Provides detailed warnings for missing or incompatible members
        /// - Recursive deserialization for complex nested objects
        ///
        /// Error Recovery:
        /// - Continues processing remaining members even if individual members fail
        /// - Provides detailed error messages with proper indentation for nested structures
        /// - Logs warnings for non-critical issues while preserving overall deserialization
        /// </summary>
        /// <param name="reflector">The Reflector instance used for recursive deserialization operations.</param>
        /// <param name="data">SerializedMember containing the data to deserialize.</param>
        /// <param name="fallbackType">Optional type to use when type information is missing from data.</param>
        /// <param name="fallbackName">Optional name to use for logging when name is missing from data.</param>
        /// <param name="depth">Current depth in the object hierarchy for proper error message indentation.</param>
        /// <param name="stringBuilder">Optional StringBuilder for accumulating detailed operation logs.</param>
        /// <param name="logger">Optional logger for tracing deserialization operations.</param>
        /// <returns>The deserialized object instance, or null if deserialization fails.</returns>
        public virtual object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            DeserializationContext? context = null)
        {
            if (!TryDeserializeValue(
                reflector,
                data: data,
                result: out var result,
                type: out var type,
                fallbackType: fallbackType,
                depth: depth,
                logs: logs,
                logger: logger))
            {
                return result;
            }

            var padding = StringUtils.GetPadding(depth);

            // Register the object early (before deserializing children) so child references can resolve
            if (result != null && context != null)
                context.Register(result);

            if (data.fields != null)
            {
                if (data.fields.Count > 0)
                    result ??= CreateInstance(reflector, type!);

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Field} Deserialize '{nameof(SerializedMember.fields)}' type='{type?.GetTypeId().ValueOrNull()}' name='{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'.");

                foreach (var field in data.fields)
                {
                    if (string.IsNullOrEmpty(field.name))
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Field name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.");

                        logs?.Warning($"Field name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.", depth);

                        continue;
                    }

                    var fieldInfo = type!.GetField(field.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo == null)
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Field '{field.name}' not found on type '{type.GetTypeId()}'.");

                        logs?.Warning($"Field '{field.name}' not found on type '{type.GetTypeId()}'.", depth);

                        continue;
                    }

                    var fieldValue = reflector.Deserialize(
                        data: field,
                        fallbackType: fieldInfo.FieldType,
                        depth: depth + 1,
                        logs: logs,
                        logger: logger,
                        context: context);
                    fieldInfo.SetValue(result, fieldValue);
                }
            }
            if (data.props != null)
            {
                if (data.props.Count > 0)
                    result ??= CreateInstance(reflector, type!);

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Property} Deserialize '{nameof(SerializedMember.props)}' type='{type?.GetTypeId().ValueOrNull()}' name='{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'.");

                foreach (var property in data.props)
                {
                    if (string.IsNullOrEmpty(property.name))
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Property name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.");

                        logs?.Warning($"Property name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.", depth);

                        continue;
                    }

                    var propertyInfo = type!.GetProperty(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (propertyInfo == null)
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Property '{property.name}' not found on type '{type.GetTypeId()}'.");

                        logs?.Warning($"Property '{property.name}' not found on type '{type.GetTypeId()}'.", depth);

                        continue;
                    }
                    if (!propertyInfo.CanWrite)
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Property '{property.name}' on type '{type.GetTypeId()}' is read-only and cannot be set.");

                        logs?.Warning($"Property '{property.name}' on type '{type.GetTypeId()}' is read-only and cannot be set.", depth);

                        continue;
                    }

                    var propertyValue = reflector.Deserialize(
                        property,
                        fallbackType: propertyInfo.PropertyType,
                        depth: depth + 1,
                        logs: logs,
                        logger: logger,
                        context: context);

                    propertyInfo.SetValue(result, propertyValue);
                }
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize the value portion of a SerializedMember with comprehensive type resolution and validation.
        /// This method orchestrates the value deserialization process including type resolution, converter selection,
        /// and delegation to appropriate internal deserialization methods.
        ///
        /// Type Resolution Process:
        /// 1. Prioritizes type information from SerializedMember.typeName
        /// 2. Falls back to provided fallbackType parameter
        /// 3. Validates type compatibility and existence
        /// 4. Handles nullable type unwrapping automatically
        ///
        /// Deserialization Strategies:
        /// - Cascade mode: Attempts to deserialize as SerializedMember structure for complex objects
        /// - Direct mode: Deserializes directly from JSON for primitive and simple types
        /// - Error recovery: Provides meaningful error messages and fallback values
        /// - Logging integration: Comprehensive trace logging for debugging and monitoring
        ///
        /// Success/Failure Handling:
        /// - Returns true for successful deserialization with valid result
        /// - Returns false for failed deserialization with appropriate error logging
        /// - Provides detailed error information in StringBuilder for analysis
        /// - Maintains type safety throughout the deserialization process
        /// </summary>
        /// <param name="reflector">The Reflector instance used for type resolution and recursive operations.</param>
        /// <param name="data">The SerializedMember containing the data to deserialize.</param>
        /// <param name="result">Output parameter containing the deserialized object on success.</param>
        /// <param name="type">Output parameter containing the resolved target type.</param>
        /// <param name="fallbackType">Optional fallback type when type resolution from data fails.</param>
        /// <param name="depth">Current depth in object hierarchy for proper error message indentation.</param>
        /// <param name="stringBuilder">Optional StringBuilder for accumulating detailed operation logs.</param>
        /// <param name="logger">Optional logger for tracing deserialization operations.</param>
        /// <returns>True if deserialization succeeded, false otherwise.</returns>
        protected virtual bool TryDeserializeValue(
            Reflector reflector,
            SerializedMember? data,
            out object? result,
            out Type? type,
            Type? fallbackType = null,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            if (data == null)
            {
                result = null;
                type = null;
                return false;
            }

            var padding = StringUtils.GetPadding(depth);

            // Get the most appropriate type for deserialization
            type = TypeUtils.GetTypeWithNamePriority(data, fallbackType, out var error);
            if (type == null)
            {
                result = null;
                logs?.Error(error ?? "Unknown error", depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{error}");
                return false;
            }

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}{Consts.Emoji.Start} Deserialize 'value', type='{type.GetTypeId()}' name='{data.name.ValueOrNull()}'.");

            var success = TryDeserializeValueInternal(
                reflector,
                data: data,
                result: out result,
                type: type,
                depth: depth,
                logs: logs,
                logger: logger);

            if (success)
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized '{type.GetTypeId()}'.");
            }
            else
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} Deserialization '{type.GetTypeId()}' failed. Converter: {GetType().GetTypeShortName()}");
            }

            return success;
        }
        protected virtual bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember data,
            out object? result,
            Type type,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (AllowCascadeSerialization)
            {
                try
                {
                    if (data.valueJsonElement == null)
                    {
                        if (logger?.IsEnabled(LogLevel.Trace) == true)
                            logger.LogTrace($"{padding}'value' is null. Converter: {GetType().GetTypeShortName()}");

                        result = GetDefaultValue(reflector, type);
                        return true;
                    }
                    if (data.valueJsonElement.Value.ValueKind != JsonValueKind.Object)
                    {
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding}'value' is not an object. It is '{data.valueJsonElement?.ValueKind}'. Converter: {GetType().GetTypeShortName()}");

                        logs?.Error("'value' is not an object. Attempting to deserialize as SerializedMember.", depth);

                        result = reflector.GetDefaultValue(type);
                        return false;
                    }

                    result = data.valueJsonElement.DeserializeValueSerializedMember(
                        reflector,
                        type: type,
                        name: data.name,
                        depth: depth + 1,
                        logs: logs,
                        logger: logger);
                    return true;
                }
                catch (JsonException ex)
                {
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                        logger.LogWarning($"{padding}{Consts.Emoji.Warn} Deserialize 'value', type='{type.GetTypeId()}' name='{data.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");

                    logs?.Warning($"Failed to deserialize member '{data.name.ValueOrNull()}' of type '{type.GetTypeId()}':\n{ex.Message}", depth);
                }
                catch (NotSupportedException ex)
                {
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                        logger.LogWarning($"{padding}{Consts.Emoji.Warn} Deserialize 'value', type='{type.GetTypeId()}' name='{data.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");

                    logs?.Warning($"Unsupported type '{type.GetTypeId()}' for member '{data.name.ValueOrNull()}':\n{ex.Message}", depth);
                }

                result = reflector.GetDefaultValue(type);
                return false;
            }
            else
            {
                try
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}Deserialize as json. Converter: {GetType().GetTypeShortName()}");

                    result = DeserializeValueAsJsonElement(
                        reflector: reflector,
                        data: data,
                        type: type,
                        depth: depth,
                        logs: logs,
                        logger: logger);

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized as json: {data.valueJsonElement}");

                    return true;
                }
                catch (Exception ex)
                {
                    logs?.Error($"Failed to deserialize value'{data.name.ValueOrNull()}' of type '{type.GetTypeId()}':\n{ex.Message}", depth);
                    if (logger?.IsEnabled(LogLevel.Critical) == true)
                        logger.LogCritical($"{padding}{Consts.Emoji.Fail} Deserialize 'value', type='{type.GetTypeId()}' name='{data.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
                    result = reflector.GetDefaultValue(type);
                    return false;
                }
            }
        }

        protected virtual object? DeserializeValueAsJsonElement(
            Reflector reflector,
            SerializedMember data,
            Type type,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null)
        {
            return reflector.JsonSerializer.Deserialize(
                reflector,
                data.valueJsonElement,
                type);
        }
    }
}