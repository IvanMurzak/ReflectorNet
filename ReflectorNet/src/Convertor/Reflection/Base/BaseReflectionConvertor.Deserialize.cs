using System;
using System.Reflection;
using System.Text;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        public virtual object? Deserialize(Reflector reflector, SerializedMember data, Type? fallbackType = null, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (!data.TryDeserializeValue(reflector, out var result, out var type, fallbackType: fallbackType, depth: depth, stringBuilder: stringBuilder, logger: logger))
                return null;

            var padding = StringUtils.GetPadding(depth);

            if (data.fields != null)
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Field} Deserialize Fields type='{type.GetTypeShortName()}' name='{data.name.ValueOrNull()}'.");

                foreach (var field in data.fields)
                {
                    if (string.IsNullOrEmpty(field.name))
                    {
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Warning] Field name is null or empty in serialized data: '{data.name.ValueOrNull()}'. Skipping.");
                        continue;
                    }

                    var fieldValue = Deserialize(reflector, field, depth: depth + 1, stringBuilder: stringBuilder, logger: logger);

                    var fieldInfo = type!.GetField(field.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                        fieldInfo.SetValue(result, fieldValue);
                }
            }
            if (data.props != null)
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}{Consts.Emoji.Property} Deserialize Properties type='{type.GetTypeShortName()}' name='{data.name.ValueOrNull()}'.");

                foreach (var property in data.props)
                {
                    if (string.IsNullOrEmpty(property.name))
                    {
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Warning] Property name is null or empty in serialized data: '{data.name.ValueOrNull()}'. Skipping.");
                        continue;
                    }

                    var propertyValue = Deserialize(reflector, property, depth: depth + 1, stringBuilder: stringBuilder, logger: logger);

                    var propertyInfo = type!.GetProperty(property.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                        propertyInfo.SetValue(result, propertyValue);
                }
            }

            return result;
        }
    }
}