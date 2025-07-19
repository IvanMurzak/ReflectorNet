using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

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
        public static bool TryDeserializeEnumerable(this JsonElement? jsonElement, Type type, out IEnumerable<object?>? result, string? name = null, int depth = 0, StringBuilder? stringBuilder = null)
        {
            try
            {
                name ??= "null";
                var parsedList = jsonElement.Deserialize<SerializedMemberList>();

                if (stringBuilder != null)
                    stringBuilder.AppendLine(parsedList == null
                        ? new string(' ', depth) + $"Deserializing '{name}' enumerable with 'null' value."
                        : new string(' ', depth) + $"Deserializing '{name}' enumerable with {parsedList.Count} items.");

                var success = true;
                var enumerable = parsedList
                    ?.Select((element, i) =>
                    {
                        if (!element.TryDeserialize(out var parsedValue, out var errorMessage))
                        {
                            success = false;
                            if (stringBuilder != null)
                                stringBuilder.AppendLine(new string(' ', depth + 1) + $"[Error] Enumerable[{i}] deserialization failed: {errorMessage}");
                            return null;
                        }
                        if (stringBuilder != null)
                            stringBuilder.AppendLine(new string(' ', depth + 1) + $"Enumerable[{i}] deserialized successfully.");
                        return parsedValue;
                    });

                if (!success)
                {
                    result = null;
                    if (stringBuilder != null)
                        stringBuilder.AppendLine(new string(' ', depth) + $"[Error] Failed to deserialize '{name}': Some elements could not be deserialized.");
                    return false;
                }

                if (type.IsArray)
                {
                    result = enumerable?.ToArray();
                    if (stringBuilder != null)
                        stringBuilder.AppendLine(new string(' ', depth) + $"[Success] Deserialized '{name}' as an array with {result.Count()} items.");
                }
                else // if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    result = enumerable?.ToList();
                    if (stringBuilder != null)
                        stringBuilder.AppendLine(new string(' ', depth) + $"[Success] Deserialized '{name}' as a list with {result.Count()} items.");
                }

                return true;
            }
            catch (Exception ex)
            {
                result = null;
                if (stringBuilder != null)
                    stringBuilder.AppendLine(new string(' ', depth) + $"[Error] Failed to deserialize '{name}': {ex.Message}");
                return false;
            }
        }
    }
}