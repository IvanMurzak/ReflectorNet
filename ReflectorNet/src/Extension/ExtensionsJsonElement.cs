using System;
using System.Collections;
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
            => JsonUtils.Deserialize<T>(jsonElement);
        public static object? Deserialize(this JsonElement? jsonElement, Type type)
            => JsonUtils.Deserialize(jsonElement, type);
        public static T? DeserializeSerializedMember<T>(this JsonElement? jsonElement, Reflector reflector, string? name = null, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
            => (T?)DeserializeSerializedMember(jsonElement, reflector, typeof(T), name, depth, stringBuilder, logger);
        public static object? DeserializeSerializedMember(this JsonElement? jsonElement, Reflector reflector, Type type, string? name = null, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (!jsonElement.HasValue)
                return TypeUtils.GetDefaultValue(type);

            var serializedMember = jsonElement.Deserialize<SerializedMember>();
            if (serializedMember == null)
                return TypeUtils.GetDefaultValue(type);

            return reflector.Deserialize(serializedMember, type, fallbackName: name, depth: depth, stringBuilder: stringBuilder, logger: logger);
        }
        public static bool TryDeserializeSerializedMemberList(this JsonElement? jsonElement, Reflector reflector, Type type, out IEnumerable? result, string? name = null, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            var paddingNext = StringUtils.GetPadding(depth + 1);
            try
            {
                name ??= JsonUtils.Null;
                var parsedList = jsonElement.Deserialize<SerializedMemberList>();

                if (stringBuilder != null)
                    stringBuilder.AppendLine(parsedList == null
                        ? $"{padding}Deserializing '{name}' enumerable with 'null' value."
                        : $"{padding}Deserializing '{name}' enumerable with {parsedList.Count} items.");

                var success = true;
                var enumerable = parsedList
                    ?.Select((element, i) =>
                    {
                        if (!element.TryDeserializeValue(reflector, out var parsedValue, out var errorMessage, depth: depth, stringBuilder: stringBuilder, logger: logger))
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

                var elementType = TypeUtils.GetEnumerableItemType(type);
                if (elementType == null)
                {
                    result = null;
                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Failed to determine element type for '{name}'.");
                    return false;
                }

                if (type.IsArray)
                {
                    if (enumerable != null)
                    {
                        var typedArray = Array.CreateInstance(elementType, parsedList!.Count);
                        var index = 0;
                        foreach (var item in enumerable)
                        {
                            typedArray.SetValue(item, index++);
                        }
                        result = typedArray;

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Deserialized '{name}' as an array with {typedArray.Length} items.");
                    }
                    else
                    {
                        var tempResult = enumerable?.ToArray();
                        result = tempResult;

                        if (stringBuilder != null)
                            stringBuilder.AppendLine(tempResult == null
                                ? $"{padding}[Success] Deserialized '{name}' as 'null' array."
                                : $"{padding}[Success] Deserialized '{name}' as an array with {tempResult!.Length} items.");
                    }
                }
                else
                {
                    var tempResult = enumerable?.ToList();
                    result = tempResult;
                    if (stringBuilder != null)
                        stringBuilder.AppendLine(tempResult == null
                            ? $"{padding}[Success] Deserialized '{name}' as 'null' list."
                            : $"{padding}[Success] Deserialized '{name}' as a list with {tempResult.Count} items.");
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