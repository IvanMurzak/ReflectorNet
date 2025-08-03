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
            if (!TryDeserializeValue(reflector, // error is here happens in the array of celestial bodies
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
                    result ??= CreateInstance(reflector, type!);

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
                    if (fieldInfo == null)
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Field '{field.name}' not found on type '{type.GetTypeShortName()}'.");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Warning] Field '{field.name}' not found on type '{type.GetTypeShortName()}'.");

                        continue;
                    }
                    fieldInfo.SetValue(result, fieldValue);
                }
            }
            if (data.props != null)
            {
                if (data.props.Count > 0)
                    result ??= CreateInstance(reflector, type!);

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
                    if (propertyInfo == null)
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Property '{property.name}' not found on type '{type.GetTypeShortName()}'.");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Warning] Property '{property.name}' not found on type '{type.GetTypeShortName()}'.");

                        continue;
                    }
                    if (!propertyInfo.CanWrite)
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}{Consts.Emoji.Warn} Property '{property.name}' on type '{type.GetTypeShortName()}' is read-only and cannot be set.");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Warning] Property '{property.name}' on type '{type.GetTypeShortName()}' is read-only and cannot be set.");

                        continue;
                    }
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

            var success = TryDeserializeValueInternal(
                reflector,
                data: serializedMember,
                result: out result,
                type: type,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger);

            if (success)
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized '{type.GetTypeShortName()}'.");
            }
            else
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{Consts.Emoji.Fail} Deserialization '{type.GetTypeShortName()}' failed. Converter: {GetType().GetTypeShortName()}");
            }

            return success;
        }
        protected virtual bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember data,
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
                    if (data.valueJsonElement == null)
                    {
                        if (logger?.IsEnabled(LogLevel.Trace) == true)
                            logger.LogTrace($"{padding} 'value' is null. Converter: {GetType().GetTypeShortName()}");

                        result = GetDefaultValue(reflector, type);
                        return true;
                    }
                    if (data.valueJsonElement.Value.ValueKind != JsonValueKind.Object)
                    {
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding} 'value' is not an object. It is '{data.valueJsonElement?.ValueKind}'. Converter: {GetType().GetTypeShortName()}");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Error] 'value' is not an object. Attempting to deserialize as SerializedMember.");

                        result = reflector.GetDefaultValue(type);
                        return false;
                    }

                    result = data.valueJsonElement.DeserializeValueSerializedMember(
                        reflector,
                        type: type,
                        name: data.name,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger);
                    return true;
                }
                catch (JsonException ex)
                {
                    stringBuilder?.AppendLine($"{padding}[Warning] Failed to deserialize member '{data.name.ValueOrNull()}' of type '{type.GetTypeName(pretty: true)}':\n{padding}{ex.Message}");
                    logger?.LogCritical($"{padding}{Consts.Emoji.Warn} Deserialize 'value', type='{type.GetTypeShortName()}' name='{data.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
                }
                catch (NotSupportedException ex)
                {
                    stringBuilder?.AppendLine($"{padding}[Warning] Unsupported type '{type.GetTypeName(pretty: true)}' for member '{data.name.ValueOrNull()}':\n{padding}{ex.Message}");
                    logger?.LogCritical($"{padding}{Consts.Emoji.Warn} Deserialize 'value', type='{type.GetTypeShortName()}' name='{data.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
                }

                result = reflector.GetDefaultValue(type);
                return false;
            }
            else
            {
                try
                {
                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding} Deserialize as json. Converter: {GetType().GetTypeShortName()}");

                    result = DeserializeValueAsJsonElement(
                        reflector: reflector,
                        data: data,
                        type: type,
                        depth: depth,
                        stringBuilder: stringBuilder,
                        logger: logger);

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}{Consts.Emoji.Done} Deserialized as json: {data.valueJsonElement}");

                    return true;
                }
                catch (Exception ex)
                {
                    stringBuilder?.AppendLine($"{padding}[Error] Failed to deserialize value'{data.name.ValueOrNull()}' of type '{type.GetTypeName(pretty: true)}':\n{padding}{ex.Message}");
                    logger?.LogCritical($"{padding}{Consts.Emoji.Fail} Deserialize 'value', type='{type.GetTypeShortName()}' name='{data.name.ValueOrNull()}':\n{padding}{ex.Message}\n{ex.StackTrace}");
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
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            return JsonUtils.Deserialize(reflector, data.valueJsonElement, type);
        }
    }
}