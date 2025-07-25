using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsJsonElement
    {
        public static T? Deserialize<T>(this JsonElement? jsonElement)
        {
            return jsonElement != null && jsonElement.HasValue
                ? JsonUtils.Deserialize<T>(jsonElement.Value)
                : TypeUtils.GetDefaultValue<T>();
        }
        public static object? Deserialize(this JsonElement? jsonElement, Type type)
        {
            return jsonElement != null && jsonElement.HasValue
                ? JsonUtils.Deserialize(jsonElement.Value, type)
                : TypeUtils.GetDefaultValue(type);
        }
        public static bool TryDeserializeEnumerable(this JsonElement? jsonElement, Type type, out IEnumerable<object?>? result, string? name = null, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            var paddingNext = StringUtils.GetPadding(depth + 1);
            try
            {
                name ??= "null";
                var parsedList = jsonElement.Deserialize<SerializedMemberList>();

                if (stringBuilder != null)
                    stringBuilder.AppendLine(parsedList == null
                        ? $"{padding}Deserializing '{name}' enumerable with 'null' value."
                        : $"{padding}Deserializing '{name}' enumerable with {parsedList.Count} items.");

                var success = true;
                var enumerable = parsedList
                    ?.Select((element, i) =>
                    {
                        if (!element.TryDeserialize(out var parsedValue, out var errorMessage, logger: logger))
                        {
                            success = false;
                            if (stringBuilder != null)
                                stringBuilder.AppendLine($"{paddingNext}[Error] Enumerable[{i}] deserialization failed: {errorMessage}");
                            return null;
                        }
                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{paddingNext}Enumerable[{i}] deserialized successfully.");
                        return parsedValue;
                    });

                if (!success)
                {
                    result = null;
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Failed to deserialize '{name}': Some elements could not be deserialized.");
                    return false;
                }

                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    if (elementType != null && enumerable != null)
                    {
                        var typedArray = Array.CreateInstance(elementType, enumerable.Count());
                        var index = 0;
                        foreach (var item in enumerable)
                        {
                            typedArray.SetValue(item, index++);
                        }
                        result = typedArray.Cast<object?>();
                    }
                    else
                    {
                        result = enumerable?.ToArray();
                    }
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Success] Deserialized '{name}' as an array with {result?.Count() ?? 0} items.");
                }
                else // if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    result = enumerable?.ToList();
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Success] Deserialized '{name}' as a list with {result?.Count() ?? 0} items.");
                }

                return true;
            }
            catch (Exception ex)
            {
                result = null;
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Failed to deserialize '{name}': {ex.Message}");
                logger?.LogCritical($"[Error] Failed to deserialize '{name}': {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
    }
}