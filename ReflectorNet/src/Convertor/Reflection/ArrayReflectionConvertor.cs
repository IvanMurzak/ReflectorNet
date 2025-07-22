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
        bool IsGenericList(Type type, out Type? elementType)
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

        protected override SerializedMember InternalSerialize(Reflector reflector, object obj, Type type, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            int index = 0;
            var serializedList = new List<SerializedMember>();
            var enumerable = (System.Collections.IEnumerable)obj;

            // Determine the element type for handling null elements
            Type? elementType = null;
            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                if (genericArgs.Length > 0)
                    elementType = genericArgs[0];
            }
            else if (type.BaseType != null && type.BaseType.IsGenericType)
            {
                var genericArgs = type.BaseType.GetGenericArguments();
                if (genericArgs.Length > 0)
                    elementType = genericArgs[0];
            }

            foreach (var element in enumerable)
            {
                var elementTypeToUse = element?.GetType() ?? elementType;
                serializedList.Add(reflector.Serialize(element, type: elementTypeToUse, name: $"[{index++}]", recursive: recursive, flags: flags, logger: logger));
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
            if (!value.TryDeserializeEnumerable(type, out var parsedValue, depth: depth + 1, stringBuilder: stringBuilder))
            {
                Print.FailedToSetNewValue(ref obj, type, depth, stringBuilder);
                return false;
            }

            Print.SetNewValueEnumerable(ref obj, ref parsedValue, type, depth, stringBuilder);
            obj = parsedValue;
            return true;
        }

        public override object? Deserialize(Reflector reflector, SerializedMember data, ILogger? logger = null)
        {
            // For arrays and lists, we need special handling since the value is a IList<SerializedMember>
            var type = TypeUtils.GetType(data.typeName);
            if (type == null)
            {
                logger?.LogError("Type '{TypeName}' not found", data.typeName);
                return null;
            }

            // Try to deserialize the value as a SerializedMemberList
            var serializedMemberList = data.valueJsonElement.Deserialize<SerializedMemberList>();
            if (serializedMemberList == null)
            {
                logger?.LogError("Failed to deserialize as SerializedMemberList");
                return null;
            }

            logger?.LogDebug("Deserializing type: {TypeFullName}, IsArray: {IsArray}, IsList: {IsList}, InheritsFromList: {InheritsFromList}, ElementCount: {ElementCount}",
                type.GetTypeName(pretty: true),
                type.IsArray,
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>),
                type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(IList<>),
                serializedMemberList.Count);

            if (type.IsArray)
            {
                // Handle arrays
                var elementType = type.GetElementType();
                if (elementType == null)
                    return null;

                var array = Array.CreateInstance(elementType, serializedMemberList.Count);

                for (int i = 0; i < serializedMemberList.Count; i++)
                {
                    var element = serializedMemberList[i];
                    var deserializedElement = reflector.Deserialize(element);
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
                    logger?.LogError("Failed to create list instance for type: {TypeFullName}", type.GetTypeName(pretty: true));
                    return null;
                }

                var addMethod = type.GetMethod(nameof(IList<object>.Add));
                if (addMethod == null)
                {
                    logger?.LogError("Failed to find Add method on list type: {TypeFullName}", type.GetTypeName(pretty: true));
                    return null;
                }

                foreach (var element in serializedMemberList)
                {
                    logger?.LogDebug("Deserializing element: {ElementName}, typeName: {TypeName}", element.name, element.typeName);
                    var deserializedElement = reflector.Deserialize(element);
                    if (deserializedElement != null)
                    {
                        logger?.LogDebug("Element deserialized successfully: {Element}", deserializedElement);
                        addMethod.Invoke(list, new[] { deserializedElement });
                    }
                    else
                    {
                        logger?.LogWarning("Failed to deserialize element: {ElementName}", element.name);
                    }
                }

                logger?.LogInformation("Successfully created list of type: {TypeFullName}", list.GetType().GetTypeName(pretty: true));
                return list;
            }

            logger?.LogWarning("Type {TypeFullName} is neither array nor generic list", type.GetTypeName(pretty: true));
            return null;
        }

        public override bool SetAsField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!value.TryDeserializeEnumerable(type, out var enumerable, depth: depth + 1, stringBuilder: stringBuilder, logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to set field '{value?.name}'");
                return false;
            }

            fieldInfo.SetValue(obj, enumerable);

            stringBuilder?.AppendLine(enumerable == null
                ? $"{padding}[Success] Field '{value?.name}' modified to 'null'."
                : $"{padding}[Success] Field '{value?.name}' modified to '[{string.Join(", ", enumerable)}]'.");
            return true;
        }

        public override bool SetAsProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!value.TryDeserializeEnumerable(type, out var parsedValue, depth: depth + 1, stringBuilder: stringBuilder, logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to set property '{value?.name}'");
                return false;
            }

            propertyInfo.SetValue(obj, parsedValue);

            stringBuilder?.AppendLine($"{padding}[Success] Property '{value?.name}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!value.TryDeserialize(type, out var parsedValue, logger: logger))
                return false;

            // TODO: Print previous and new value in stringBuilder
            fieldInfo.SetValue(obj, parsedValue);
            return true;
        }

        public override bool SetProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!value.TryDeserialize(type, out var parsedValue, logger: logger))
                return false;

            // TODO: Print previous and new value in stringBuilder
            propertyInfo.SetValue(obj, parsedValue);
            return true;
        }
    }
}