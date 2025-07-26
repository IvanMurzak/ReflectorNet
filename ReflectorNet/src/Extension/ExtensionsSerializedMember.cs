using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsSerializedMember
    {
        public static bool TryDeserializeValue(this SerializedMember? serializedMember, Reflector reflector, out object? result, out Type? type, Type? fallbackType = null, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (serializedMember == null)
            {
                result = null;
                type = null;
                return false;
            }

            var padding = StringUtils.GetPadding(depth);

            // Get the most appropriate type for deserialization
            type = TypeUtils.GetTypeWithNamePriority(serializedMember, fallbackType, out var error);
            if (type == null)
            {
                result = null;
                stringBuilder?.AppendLine($"{padding}[Error] {error}");
                logger?.LogError($"{padding}{error}");
                return false;
            }

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace($"{padding}{Consts.Emoji.Start} Deserialize Value type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}'.");

            try
            {
                var isArray = serializedMember.valueJsonElement?.ValueKind == JsonValueKind.Array;
                if (isArray)
                {
                    // Try to deserialize is as an SerializedMemberList first
                    if (serializedMember.valueJsonElement.TryDeserializeSerializedMemberList(reflector, type, out var enumerableResult, serializedMember.name, depth: depth + 1, stringBuilder: stringBuilder, logger: logger))
                    {
                        result = enumerableResult;
                        if (logger?.IsEnabled(LogLevel.Trace) == true)
                            logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialize Value type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}' as an enumerable.");
                        return true;
                    }
                }
                else if (serializedMember.valueJsonElement?.ValueKind == JsonValueKind.Object)
                {
                    // If that fails, try to deserialize as a single SerializedMember object
                    result = serializedMember.valueJsonElement.DeserializeSerializedMember(reflector, type, depth: depth + 1, stringBuilder: stringBuilder, logger: logger);
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialize Value type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}' as {nameof(SerializedMember)}.");
                    return true;
                }
            }
            catch (JsonException ex)
            {
                stringBuilder?.AppendLine($"{padding}[Warning] Failed to deserialize member '{serializedMember.name.ValueOrNull()}' of type '{type.GetTypeName(pretty: true)}':\n{padding}{ex.Message}");
                logger?.LogCritical($"{padding}{Consts.Emoji.Fail} Deserialize Value type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
            }
            catch (NotSupportedException ex)
            {
                stringBuilder?.AppendLine($"{padding}[Warning] Unsupported type '{type.GetTypeName(pretty: true)}' for member '{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}");
                logger?.LogCritical($"{padding}{Consts.Emoji.Fail} Deserialize Value type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
            }

            // If we reach here, it means deserialization failed, try to deserialize as a simple json
            try
            {
                result = serializedMember.valueJsonElement.Deserialize(type);
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialize Value type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}' as json. From the JSON: {serializedMember.valueJsonElement}");

                return true;
            }
            catch (Exception ex)
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to deserialize value'{serializedMember.name.ValueOrNull()}' of type '{type.GetTypeName(pretty: true)}':\n{padding}{ex.Message}");
                logger?.LogCritical($"{padding}{Consts.Emoji.Fail} Deserialize Value type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
                result = null;
                return false;
            }
        }
        // public static bool TryDeserializeValue(this SerializedMember? serializedMember, Type targetType, out object? result, ILogger? logger = null)
        // {
        //     if (serializedMember == null)
        //     {
        //         result = null;
        //         return false;
        //     }

        //     var padding = StringUtils.GetPadding(depth);

        //     try
        //     {
        //         result = serializedMember.valueJsonElement.Deserialize(targetType);
        //         if (logger?.IsEnabled(LogLevel.Trace) == true)
        //             logger.LogTrace($"Successfully deserialized '{serializedMember.name.ValueOrNull()}' of type '{targetType.GetTypeShortName()}' as json. From: {serializedMember.valueJsonElement}");
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         logger?.LogCritical($"Failed to deserialize '{serializedMember.name.ValueOrNull()}' of type '{targetType.GetTypeName(pretty: true)}':\n{padding}{ex.Message}\n{ex.StackTrace}");
        //         result = null;
        //         return false;
        //     }
        // }

        // public static bool TryDeserializeValue<T>(this SerializedMember? serializedMember, out T? result, ILogger? logger = null)
        // {
        //     if (serializedMember == null)
        //     {
        //         result = default;
        //         return false;
        //     }
        //     try
        //     {
        //         result = serializedMember.valueJsonElement.Deserialize<T>();
        //         if (logger?.IsEnabled(LogLevel.Trace) == true)
        //             logger.LogTrace($"Successfully deserialized '{serializedMember.name.ValueOrNull()}' of type '{typeof(T).GetTypeShortName()}' as json. From: {serializedMember.valueJsonElement}");
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         logger?.LogCritical($"Failed to deserialize '{serializedMember.name.ValueOrNull()}' of type '{typeof(T).GetTypeName(pretty: true)}': {ex.Message}\n{ex.StackTrace}");
        //         result = default;
        //         return false;
        //     }
        // }

        // public static bool TryDeserializeValue(this SerializedMember? serializedMember, out object? result, out Type? type, out string? error, ILogger? logger = null)
        // {
        //     if (serializedMember == null)
        //     {
        //         result = null;
        //         type = null;
        //         error = "SerializedMember is null.";
        //         return false;
        //     }
        //     type = TypeUtils.GetType(serializedMember.typeName);
        //     if (type == null)
        //     {
        //         result = null;
        //         error = $"Type '{serializedMember.typeName}' not found for '{serializedMember.name.ValueOrNull()}'.";
        //         return false;
        //     }
        //     try
        //     {
        //         result = serializedMember.valueJsonElement.Deserialize(type);
        //         if (logger?.IsEnabled(LogLevel.Trace) == true)
        //             logger.LogTrace($"Successfully deserialized '{serializedMember.name.ValueOrNull()}' of type '{type.GetTypeShortName()}' as json. From: {serializedMember.valueJsonElement}");
        //         error = null;
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         error = $"Failed to deserialize '{serializedMember.name.ValueOrNull()}' of type '{serializedMember.typeName.ValueOrNull()}': {ex.Message}";
        //         logger?.LogCritical($"Failed to deserialize '{serializedMember.name.ValueOrNull()}' of type '{serializedMember.typeName.ValueOrNull()}': {ex.Message}\n{ex.StackTrace}");
        //         result = null;
        //         return false;
        //     }
        // }
        // public static bool TryDeserializeValue(this SerializedMember? serializedMember, Type targetType, out object? result, out string? error, ILogger? logger = null)
        // {
        //     if (serializedMember == null)
        //     {
        //         result = null;
        //         error = "SerializedMember is null.";
        //         return false;
        //     }
        //     try
        //     {
        //         result = serializedMember.valueJsonElement.Deserialize(targetType);
        //         if (logger?.IsEnabled(LogLevel.Trace) == true)
        //             logger.LogTrace($"Successfully deserialized '{serializedMember.name.ValueOrNull()}' of type '{targetType.GetTypeShortName()}' as json. From: {serializedMember.valueJsonElement}");
        //         error = null;
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         error = $"Failed to deserialize '{serializedMember.name.ValueOrNull()}' of type '{serializedMember.typeName.ValueOrNull()}': {ex.Message}";
        //         logger?.LogCritical($"Failed to deserialize '{serializedMember.name.ValueOrNull()}' of type '{serializedMember.typeName.ValueOrNull()}': {ex.Message}\n{ex.StackTrace}");
        //         result = null;
        //         return false;
        //     }
        // }

        // public static bool TryDeserializeValue<T>(this SerializedMember? serializedMember, out T? result, out string? error, ILogger? logger = null)
        // {
        //     if (serializedMember == null)
        //     {
        //         result = default;
        //         error = "SerializedMember is null.";
        //         return false;
        //     }
        //     try
        //     {
        //         result = serializedMember.valueJsonElement.Deserialize<T>();
        //         error = null;
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         error = $"Failed to deserialize member '{serializedMember.name.ValueOrNull()}' of type '{serializedMember.typeName.ValueOrNull()}': {ex.Message}";
        //         logger?.LogCritical($"Failed to deserialize member '{serializedMember.name.ValueOrNull()}' of type '{serializedMember.typeName.ValueOrNull()}': {ex.Message}\n{ex.StackTrace}");
        //         result = default;
        //         return false;
        //     }
        // }
        public static bool TryDeserializeValueSerializedMemberList(this SerializedMember? serializedMember, Reflector reflector, Type type, out IEnumerable<object?>? result, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (serializedMember == null)
            {
                result = default;
                stringBuilder?.AppendLine("SerializedMember is null.");
                return false;
            }

            return serializedMember.valueJsonElement.TryDeserializeSerializedMemberList(reflector, type, out result, serializedMember.name, depth: depth, stringBuilder: stringBuilder, logger: logger);
        }
    }
}