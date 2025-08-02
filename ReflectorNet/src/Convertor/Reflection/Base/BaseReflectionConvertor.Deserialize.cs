using System;
using System.Text;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        public virtual object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (!TryDeserializeValue(reflector,
                serializedMember: data,
                result: out var result,
                type: out var type,
                fallbackType: fallbackType,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger))
            {
                return result;
            }

            var padding = StringUtils.GetPadding(depth);

            if (data.fields != null)
            {
                if (data.fields.Count > 0)
                    result ??= Activator.CreateInstance(type!);

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Field} Deserialize '{nameof(SerializedMember.fields)}' type='{type.GetTypeShortName()}' name='{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'.");

                foreach (var field in data.fields)
                {
                    if (string.IsNullOrEmpty(field.name))
                    {
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Warning] Field name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.");
                        continue;
                    }

                    var fieldValue = reflector.Deserialize(field, depth: depth + 1, stringBuilder: stringBuilder, logger: logger);

                    var fieldInfo = type!.GetField(field.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                        fieldInfo.SetValue(result, fieldValue);
                }
            }
            if (data.props != null)
            {
                if (data.props.Count > 0)
                    result ??= Activator.CreateInstance(type!);

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Property} Deserialize '{nameof(SerializedMember.props)}' type='{type.GetTypeShortName()}' name='{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'.");

                foreach (var property in data.props)
                {
                    if (string.IsNullOrEmpty(property.name))
                    {
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Warning] Property name is null or empty in serialized data: '{(StringUtils.IsNullOrEmpty(data.name) ? fallbackName : data.name).ValueOrNull()}'. Skipping.");
                        continue;
                    }

                    var propertyValue = reflector.Deserialize(
                        property,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger);

                    var propertyInfo = type!.GetProperty(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                        propertyInfo.SetValue(result, propertyValue);
                }
            }

            return result;
        }

        protected virtual bool TryDeserializeValue(
            Reflector reflector,
            SerializedMember? serializedMember,
            out object? result,
            out Type? type,
            Type? fallbackType = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
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

            var success = TryDeserializeValueInternal(reflector,
                serializedMember: serializedMember,
                result: out result,
                type: type,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger);

            if (success)
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized.");
            }
            else
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} Deserialization failed.");
            }

            return success;
        }
        protected virtual bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember serializedMember,
            out object? result,
            Type type,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (AllowCascadeSerialization)
            {
                try
                {
                    if (serializedMember.valueJsonElement?.ValueKind == JsonValueKind.Object)
                    {
                        // If that fails, try to deserialize as a single SerializedMember object
                        result = serializedMember.valueJsonElement.DeserializeValueSerializedMember(reflector,
                            type: type,
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
                    logger?.LogCritical($"{padding}{Consts.Emoji.Warn} Deserialize 'value', type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
                }
                catch (NotSupportedException ex)
                {
                    stringBuilder?.AppendLine($"{padding}[Warning] Unsupported type '{type.GetTypeName(pretty: true)}' for member '{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}");
                    logger?.LogCritical($"{padding}{Consts.Emoji.Warn} Deserialize 'value', type='{type.GetTypeShortName()}' name='{serializedMember.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
                }
                result = TypeUtils.GetDefaultValue(type);
                return false;
            }
            else
            {
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
                    result = TypeUtils.GetDefaultValue(type);
                    return false;
                }
            }
        }
    }
}