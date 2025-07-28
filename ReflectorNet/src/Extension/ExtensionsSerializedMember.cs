using System;
using System.Collections;
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
                logger.LogTrace($"{padding}{Consts.Emoji.Start} Deserialize 'value', type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}'.");

            try
            {
                var isArray = serializedMember.valueJsonElement?.ValueKind == JsonValueKind.Array;
                if (isArray)
                {
                    // Try to deserialize is as an SerializedMemberList first
                    if (serializedMember.valueJsonElement.TryDeserializeValueSerializedMemberList(reflector, type, out var enumerableResult,
                        name: serializedMember.name,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger))
                    {
                        result = enumerableResult;
                        if (logger?.IsEnabled(LogLevel.Trace) == true)
                            logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized as an enumerable.");
                        return true;
                    }
                }
                else if (serializedMember.valueJsonElement?.ValueKind == JsonValueKind.Object)
                {
                    // If that fails, try to deserialize as a single SerializedMember object
                    result = serializedMember.valueJsonElement.DeserializeValueSerializedMember(reflector, type,
                        name: serializedMember.name,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger);
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized as {nameof(SerializedMember)}.");
                    return true;
                }
            }
            catch (JsonException ex)
            {
                stringBuilder?.AppendLine($"{padding}[Warning] Failed to deserialize member '{serializedMember.name.ValueOrNull()}' of type '{type.GetTypeName(pretty: true)}':\n{padding}{ex.Message}");
                logger?.LogCritical($"{padding}{Consts.Emoji.Fail} Deserialize 'value', type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
            }
            catch (NotSupportedException ex)
            {
                stringBuilder?.AppendLine($"{padding}[Warning] Unsupported type '{type.GetTypeName(pretty: true)}' for member '{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}");
                logger?.LogCritical($"{padding}{Consts.Emoji.Fail} Deserialize 'value', type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
            }

            // If we reach here, it means deserialization failed, try to deserialize as a simple json
            try
            {
                result = serializedMember.valueJsonElement.Deserialize(type);
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized as json: {serializedMember.valueJsonElement}");

                return true;
            }
            catch (Exception ex)
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to deserialize value'{serializedMember.name.ValueOrNull()}' of type '{type.GetTypeName(pretty: true)}':\n{padding}{ex.Message}");
                logger?.LogCritical($"{padding}{Consts.Emoji.Fail} Deserialize 'value', type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
                result = null;
                return false;
            }
        }

        public static bool TryDeserializeValueSerializedMemberList(this SerializedMember? serializedMember, Reflector reflector, Type type, out IEnumerable? result, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (serializedMember == null)
            {
                result = default;
                stringBuilder?.AppendLine("SerializedMember is null.");
                return false;
            }

            return serializedMember.valueJsonElement.TryDeserializeValueSerializedMemberList(reflector, type, out result, serializedMember.name, depth: depth, stringBuilder: stringBuilder, logger: logger);
        }
    }
}