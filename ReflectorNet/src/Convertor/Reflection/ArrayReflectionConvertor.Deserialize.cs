/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public partial class ArrayReflectionConvertor : BaseReflectionConvertor<Array>
    {
        public override object? Deserialize(
            Reflector reflector,
            SerializedMember data,
            Type? fallbackType = null,
            string? fallbackName = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            // For arrays and lists, we need special handling since the value is a IList<SerializedMember>
            var type = TypeUtils.GetTypeWithNamePriority(data, fallbackType, out var error);
            if (type == null)
            {
                if (logger?.IsEnabled(LogLevel.Warning) == true)
                    logger.LogWarning($"{padding}{error}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Warning] {error}");

                return null;
            }

            if (logger?.IsEnabled(LogLevel.Trace) == true)
            {
                logger.LogTrace("{padding}{icon} Deserialize 'value', type='{typeName}', collectionType='{collectionType}'",
                    padding,
                    Consts.Emoji.Start,
                    type.GetTypeShortName(),
                    type.IsArray
                        ? "Array"
                        : IsGenericList(type, out var _)
                            ? "IList<>"
                            : "IEnumerable");
            }

            // Check if the value is actually an array
            if (data.valueJsonElement == null || data.valueJsonElement.Value.ValueKind != JsonValueKind.Array)
            {
                if (logger?.IsEnabled(LogLevel.Warning) == true)
                {
                    logger.LogWarning("{padding}{icon} Failed to deserialize 'value' json as Array. Value is null or not an array.",
                        padding,
                        Consts.Emoji.Warn);
                }
                stringBuilder?.AppendLine($"{padding}[Warning] Failed to deserialize 'value' json as Array.");
                return null;
            }

            var jsonArray = data.valueJsonElement.Value;
            var length = jsonArray.GetArrayLength();

            if (type.IsArray)
            {
                // Handle arrays
                var elementType = type.GetElementType();
                if (elementType == null)
                {
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                    {
                        logger.LogWarning("{padding}{icon} Failed to get element type for array type '{typeName}'",
                            padding,
                            Consts.Emoji.Warn,
                            type.GetTypeName(pretty: true));
                    }
                    stringBuilder?.AppendLine($"{padding}[Warning] Failed to get element type for array type '{type.GetTypeName(pretty: true)}'.");
                    return null;
                }

                var array = Array.CreateInstance(elementType, length);
                if (array == null)
                {
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                    {
                        logger.LogWarning("{padding}{icon} Failed to create array instance for type '{typeName}'",
                            padding,
                            Consts.Emoji.Warn,
                            type.GetTypeName(pretty: true));
                    }
                    stringBuilder?.AppendLine($"{padding}[Warning] Failed to create array instance for type '{type.GetTypeName(pretty: true)}'.");
                    return null;
                }

                int i = 0;
                foreach (var element in jsonArray.EnumerateArray())
                {
                    var member = ParseElementToMember(element);

                    var deserializedElement = reflector.Deserialize(
                        data: member,
                        fallbackType: elementType,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger);

                    if (deserializedElement != null)
                    {
                        array.SetValue(deserializedElement, i);
                    }
                    i++;
                }

                return array;
            }
            else if (IsGenericList(type, out var elementType))
            {
                // Handle generic IList<T>
                var list = reflector.CreateInstance(type);
                if (list == null)
                {
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                    {
                        logger.LogWarning("{padding}{icon} Failed to create list instance for type '{typeName}'",
                            padding,
                            Consts.Emoji.Warn,
                            type.GetTypeName(pretty: true));
                    }
                    return null;
                }

                var addMethod = type.GetMethod(nameof(IList<object>.Add));
                if (addMethod == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                    {
                        logger.LogError("{padding}{icon} Failed to find 'Add' method on list type='{typeName}'",
                            padding,
                            Consts.Emoji.Fail,
                            type.GetTypeName(pretty: true));
                    }
                    return null;
                }

                foreach (var element in jsonArray.EnumerateArray())
                {
                    var member = ParseElementToMember(element);

                    var deserializedElement = reflector.Deserialize(
                        member,
                        fallbackType: elementType,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger);

                    addMethod.Invoke(list, new[] { deserializedElement });
                }

                logger?.LogInformation("{padding}Successfully created list of type='{typeName}'", padding, list.GetType().GetTypeName(pretty: true));
                return list;
            }

            logger?.LogWarning("{padding}Type '{typeName}' is neither array nor generic list", padding, type.GetTypeName(pretty: true));
            return null;
        }

        protected override bool TryDeserializeValueInternal(
            Reflector reflector,
            SerializedMember serializedMember,
            out object? result,
            Type type,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
            {
                logger.LogTrace("{padding}TryDeserializeValueInternal type='{typeName}', name='{name}', AllowCascadeSerialize={AllowCascadeSerialize}, Convertor='{ConvertorName}'",
                    padding,
                    type.GetTypeName(pretty: true),
                    serializedMember.name.ValueOrNull(),
                    AllowCascadeSerialization,
                    GetType().Name);
            }

            if (AllowCascadeSerialization)
            {
                if (serializedMember.valueJsonElement == null ||
                    serializedMember.valueJsonElement.Value.ValueKind == JsonValueKind.Null)
                {
                    result = reflector.GetDefaultValue(type);
                    if (logger?.IsEnabled(LogLevel.Warning) == true)
                    {
                        logger.LogWarning("{padding}{icon} 'value' is null for type='{typeName}', name='{name}'. Convertor='{ConvertorName}'",
                            padding,
                            Consts.Emoji.Warn,
                            type.GetTypeName(pretty: false),
                            serializedMember.name.ValueOrNull(),
                            GetType().Name);
                    }
                    return true;
                }

                var isArray = serializedMember.valueJsonElement.Value.ValueKind == JsonValueKind.Array;
                if (!isArray)
                {
                    result = reflector.GetDefaultValue(type);

                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{Consts.Emoji.Fail} Only array deserialization is supported in this Convertor ({GetType().Name}).");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Only array deserialization is supported in this Convertor ({GetType().Name}).");

                    return false;
                }

                if (TryDeserializeValueListInternal(
                    reflector,
                    jsonElement: serializedMember.valueJsonElement,
                    type: type,
                    result: out var enumerableResult,
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

                result = reflector.CreateInstance(type);
                return false;
            }
            else
            {
                return base.TryDeserializeValueInternal(
                    reflector,
                    data: serializedMember,
                    result: out result,
                    type: type,
                    depth: depth,
                    stringBuilder: stringBuilder,
                    logger: logger);
            }
        }

        protected virtual bool TryDeserializeValueListInternal(
            Reflector reflector,
            JsonElement? jsonElement,
            Type type,
            out IEnumerable? result,
            string? name = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            var paddingNext = StringUtils.GetPadding(depth + 1);

            if (logger?.IsEnabled(LogLevel.Trace) == true)
            {
                logger.LogTrace("{padding}TryDeserializeValueListInternal name='{name}', type='{typeName}'",
                    padding,
                    name.ValueOrNull(),
                    type.GetTypeShortName());
            }

            try
            {
                name = name.ValueOrNull();

                if (jsonElement == null)
                {
                    result = null;
                    return true;
                }

                if (jsonElement.Value.ValueKind != JsonValueKind.Array)
                {
                    result = null;
                    return false;
                }

                var jsonArray = jsonElement.Value;
                var count = jsonArray.GetArrayLength();

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}Deserializing '{name}' enumerable with {count} items.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}Deserializing '{name}' enumerable with {count} items.");

                var itemType = TypeUtils.GetEnumerableItemType(type);
                if (itemType == null)
                {
                    result = null;
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Failed to determine element type for '{name}' of type '{type.GetTypeShortName()}'.");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Failed to determine element type for '{name}'.");
                    return false;
                }

                var list = new List<object?>();
                int i = 0;
                foreach (var element in jsonArray.EnumerateArray())
                {
                    var member = ParseElementToMember(element);
                    var parsedValue = reflector.Deserialize(
                        data: member,
                        fallbackType: itemType,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger
                    );

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{paddingNext}Enumerable[{i}] deserialized successfully: {parsedValue?.GetType().GetTypeShortName()}");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{paddingNext}Enumerable[{i}] deserialized successfully.");

                    list.Add(parsedValue);
                    i++;
                }

                if (type.IsArray)
                {
                    var typedArray = Array.CreateInstance(itemType, list.Count);
                    for (int j = 0; j < list.Count; j++)
                    {
                        typedArray.SetValue(list[j], j);
                    }
                    result = typedArray;

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}Deserialized '{name}' as an array with {typedArray.Length} items.");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Success] Deserialized '{name}' as an array with {typedArray.Length} items.");
                }
                else
                {
                    // Note: This creates a List<object?>. If the target is List<T>, this might need adjustment
                    // but TryDeserializeValueListInternal returns IEnumerable, so it's usually iterated or cast later.
                    result = list;

                    if (logger?.IsEnabled(LogLevel.Trace) == true)
                        logger.LogTrace($"{padding}Deserialized '{name}' as a list with {list.Count} items.");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Success] Deserialized '{name}' as a list with {list.Count} items.");
                }

                return true;
            }
            catch (Exception ex)
            {
                result = null;

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Failed to deserialize '{name}': {ex.Message}\n{ex.StackTrace}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Failed to deserialize '{name}': {ex.Message}");

                return false;
            }
        }

        protected virtual SerializedMember ParseElementToMember(JsonElement element)
        {
            SerializedMember? member = null;
            if (element.ValueKind == JsonValueKind.Object &&
                (
                    element.TryGetProperty(nameof(SerializedMember.typeName), out _) ||
                    element.TryGetProperty(nameof(SerializedMember.fields), out _) ||
                    element.TryGetProperty(nameof(SerializedMember.props), out _))
                )
            {
                try
                {
                    member = System.Text.Json.JsonSerializer.Deserialize<SerializedMember>(element.GetRawText());
                    if (member != null && element.TryGetProperty("value", out var valueProp))
                    {
                        member.valueJsonElement = valueProp;
                    }
                }
                catch
                {
                    // Ignore deserialization errors here
                }
            }
            if (member == null)
            {
                member = new SerializedMember
                {
                    valueJsonElement = element
                };
            }
            return member;
        }
    }
}