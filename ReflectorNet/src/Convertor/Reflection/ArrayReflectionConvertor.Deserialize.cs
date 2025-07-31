using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public partial class ArrayReflectionConvertor : BaseReflectionConvertor<Array>
    {
        public override object? Deserialize(Reflector reflector,
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
                logger?.LogWarning($"{padding}{error}");
                stringBuilder?.AppendLine($"{padding}[Warning] {error}");
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

            // Try to deserialize the value as a SerializedMemberList
            var serializedMemberList = data.valueJsonElement.Deserialize<SerializedMemberList>();
            // TODO: Need to support 'null' value. For the case when LLM needs to set exactly 'null' value for an array or list.
            if (serializedMemberList == null)
            {
                if (logger?.IsEnabled(LogLevel.Warning) == true)
                {
                    logger.LogWarning("{padding}{icon} Failed to deserialize 'value' json as '{typeName}'",
                        padding,
                        Consts.Emoji.Warn,
                        nameof(SerializedMemberList));
                }
                stringBuilder?.AppendLine($"{padding}[Warning] Failed to deserialize 'value' json as {nameof(SerializedMemberList)}.");
                return null;
            }

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

                var array = Array.CreateInstance(elementType, serializedMemberList.Count);
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

                for (int i = 0; i < serializedMemberList.Count; i++)
                {
                    var element = serializedMemberList[i];
                    var deserializedElement = reflector.Deserialize(element,
                        fallbackType: elementType,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger);

                    if (deserializedElement != null)
                    {
                        array.SetValue(deserializedElement, i);
                    }
                }

                return array;
            }
            else if (IsGenericList(type, out var elementType))
            {
                // Handle generic IList<T>
                var list = Activator.CreateInstance(type);
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

                foreach (var element in serializedMemberList)
                {
                    // logger?.LogTrace("{padding}Deserializing element: {ElementName}, typeName: {TypeName}", padding, element.name, element.typeName);
                    var deserializedElement = reflector.Deserialize(element,
                        fallbackType: elementType,
                        depth: depth + 1,
                        stringBuilder: stringBuilder,
                        logger: logger);

                    addMethod.Invoke(list, new[] { deserializedElement });

                    // if (deserializedElement != null)
                    // {
                    //     logger?.LogTrace("{padding}Element deserialized successfully: {Element}", padding, deserializedElement);
                    //     addMethod.Invoke(list, new[] { deserializedElement });
                    // }
                    // else
                    // {
                    //     logger?.LogWarning("{padding}Failed to deserialize element: {ElementName}", padding, element.name.ValueOrNull());
                    // }
                }

                logger?.LogInformation("{padding}Successfully created list of type='{typeName}'", padding, list.GetType().GetTypeName(pretty: true));
                return list;
            }

            logger?.LogWarning("{padding}Type '{typeName}' is neither array nor generic list", padding, type.GetTypeName(pretty: true));
            return null;
        }

        protected override bool TryDeserializeValueInternal(Reflector reflector,
            SerializedMember serializedMember,
            out object? result,
            Type type,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (serializedMember.valueJsonElement == null ||
                serializedMember.valueJsonElement.Value.ValueKind == JsonValueKind.Null)
            {
                result = TypeUtils.GetDefaultValue(type);
                if (logger?.IsEnabled(LogLevel.Warning) == true)
                {
                    logger.LogWarning("{padding}{icon} 'value' is null for type '{typeName}'",
                        padding,
                        Consts.Emoji.Warn,
                        type.GetTypeName(pretty: false));
                }
                return true;
            }

            var isArray = serializedMember.valueJsonElement.Value.ValueKind == JsonValueKind.Array;
            if (!isArray)
            {
                result = TypeUtils.GetDefaultValue(type);
                throw new NotSupportedException("Array deserialization is not supported in this Convertor. Use the ArrayReflectionConvertor for array types.");
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

            result = TypeUtils.GetDefaultNonNullValue(type);
            return false;
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
                        if (!TryDeserializeValue(
                            reflector,
                            element,
                            out var parsedValue,
                            out var errorMessage,
                            depth: depth,
                            stringBuilder: stringBuilder,
                            logger: logger))
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