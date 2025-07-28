using System;
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
        protected virtual bool IsGenericList(Type type, out Type? elementType)
        {
            var iList = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

            if (iList == null)
            {
                elementType = null;
                return false;
            }
            elementType = iList.GetGenericArguments()[0];
            return true;
        }

        public override int SerializationPriority(Type type, ILogger? logger = null)
        {
            if (type.IsArray)
                return MAX_DEPTH + 1;

            var isGenericList = IsGenericList(type, out var elementType);
            if (isGenericList)
                return MAX_DEPTH + 1;

            var isArray = typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);
            return isArray
                ? MAX_DEPTH / 4
                : 0;
        }

        protected override SerializedMember InternalSerialize(Reflector reflector, object? obj, Type type, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0, StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (obj == null)
                return SerializedMember.FromJson(type, json: null, name: name);

            int index = 0;
            var enumerable = (System.Collections.IEnumerable)obj;
            var serializedList = new SerializedMemberList();

            // Determine the element type for handling null elements
            var elementType = TypeUtils.GetEnumerableItemType(type);

            foreach (var element in enumerable)
            {
                var elementTypeToUse = element?.GetType() ?? elementType;
                serializedList.Add(reflector.Serialize(element, type: elementTypeToUse, name: $"[{index++}]", recursive: recursive, flags: flags, depth: depth, stringBuilder: stringBuilder, logger: logger));
            }

            return SerializedMember.FromValue(type, serializedList, name: name);
        }

        public override IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => objType.GetFields(flags)
                .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(field => field.IsPublic);

        public override IEnumerable<PropertyInfo>? GetSerializableProperties(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => objType.GetProperties(flags)
                .Where(prop => prop.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(prop => prop.CanRead);

        protected override bool SetValue(Reflector reflector, ref object? obj, Type type, JsonElement? value, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (!value.TryDeserializeSerializedMemberList(reflector, type, out var parsedValue, depth: depth + 1, stringBuilder: stringBuilder))
            {
                Print.FailedToSetNewValue(ref obj, type, depth, stringBuilder);
                return false;
            }

            Print.SetNewValueEnumerable(ref obj, ref parsedValue, type, depth, stringBuilder);
            obj = parsedValue;
            return true;
        }

        public override object? Deserialize(Reflector reflector, SerializedMember data, Type? fallbackType = null, string? fallbackName = null, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
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
                    var deserializedElement = reflector.Deserialize(element, depth: depth + 1, stringBuilder: stringBuilder, logger: logger);
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

        public override bool SetAsField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!value.TryDeserializeValueSerializedMemberList(reflector, type, out var enumerable, depth: depth + 1, stringBuilder: stringBuilder, logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to set field '{value?.name.ValueOrNull()}'");
                return false;
            }

            fieldInfo.SetValue(obj, enumerable);

            stringBuilder?.AppendLine(enumerable == null
                ? $"{padding}[Success] Field '{value?.name.ValueOrNull()}' modified to 'null'."
                : $"{padding}[Success] Field '{value?.name.ValueOrNull()}' modified to '[{string.Join(", ", enumerable)}]'.");
            return true;
        }

        public override bool SetAsProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!value.TryDeserializeValueSerializedMemberList(reflector, type, out var parsedValue, depth: depth + 1, stringBuilder: stringBuilder, logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to set property '{value?.name.ValueOrNull()}'");
                return false;
            }

            propertyInfo.SetValue(obj, parsedValue);

            stringBuilder?.AppendLine($"{padding}[Success] Property '{value?.name.ValueOrNull()}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetField(Reflector reflector, ref object? obj, Type fallbackType, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!value.TryDeserializeValue(reflector, out var parsedValue, out var type, fallbackType: fallbackType, depth: depth, stringBuilder: stringBuilder, logger: logger))
                return false;

            // TODO: Print previous and new value in stringBuilder
            fieldInfo.SetValue(obj, parsedValue);
            return true;
        }

        public override bool SetProperty(Reflector reflector, ref object? obj, Type fallbackType, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!value.TryDeserializeValue(reflector, out var parsedValue, out var type, fallbackType: fallbackType, depth: depth, stringBuilder: stringBuilder, logger: logger))
                return false;

            // TODO: Print previous and new value in stringBuilder
            propertyInfo.SetValue(obj, parsedValue);
            return true;
        }
    }
}