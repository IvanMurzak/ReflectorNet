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

        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0,
            StringBuilder? stringBuilder = null,
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
                serializedList.Add(reflector.Serialize(element, fallbackType: elementTypeToUse, name: $"[{index++}]", recursive: recursive, flags: flags, depth: depth, stringBuilder: stringBuilder, logger: logger));
            }

            return SerializedMember.FromValue(type, serializedList, name: name);
        }

        public override IEnumerable<FieldInfo>? GetSerializableFields(
            Reflector reflector,
            Type objType,
            BindingFlags flags,
            ILogger? logger = null)
        {
            return objType.GetFields(flags)
                .Where(field => field.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(field => field.IsPublic);
        }

        public override IEnumerable<PropertyInfo>? GetSerializableProperties(
            Reflector reflector,
            Type objType,
            BindingFlags flags,
            ILogger? logger = null)
        {
            return objType.GetProperties(flags)
                .Where(prop => prop.GetCustomAttribute<ObsoleteAttribute>() == null)
                .Where(prop => prop.CanRead);
        }

        protected override bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            JsonElement? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null)
        {
            if (!TryDeserializeValueListInternal(
                reflector,
                jsonElement: value,
                type: type,
                result: out var parsedValue,
                depth: depth + 1,
                stringBuilder: stringBuilder,
                logger: logger))
            {
                Print.FailedToSetNewValue(ref obj, type, depth, stringBuilder);
                return false;
            }

            Print.SetNewValueEnumerable(ref obj, ref parsedValue, type, depth, stringBuilder);
            obj = parsedValue;
            return true;
        }

        public override bool SetAsField(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            FieldInfo fieldInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (value == null)
            {
                stringBuilder?.AppendLine($"{padding}[Error] SerializedMember is null for field '{fieldInfo.Name}'.");
                return false;
            }

            var type = TypeUtils.GetTypeWithNamePriority(value, fallbackType, out var error);
            if (type == null)
            {
                stringBuilder?.AppendLine($"{padding}[Error] {error}");
                return false;
            }

            if (!TryDeserializeValueListInternal(
                reflector,
                jsonElement: value.valueJsonElement,
                name: fieldInfo.Name,
                type: fallbackType,
                result: out var enumerable,
                depth: depth + 1,
                stringBuilder: stringBuilder,
                logger: logger))
            {
                Print.FailedToSetField(ref obj, type, fieldInfo, depth, stringBuilder);
                return false;
            }

            fieldInfo.SetValue(obj, enumerable);

            stringBuilder?.AppendLine(enumerable == null
                ? $"{padding}[Success] Field '{value?.name.ValueOrNull()}' modified to 'null'."
                : $"{padding}[Success] Field '{value?.name.ValueOrNull()}' modified to '[{string.Join(", ", enumerable)}]'.");
            return true;
        }

        public override bool SetAsProperty(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            PropertyInfo propertyInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (value == null)
            {
                stringBuilder?.AppendLine($"{padding}[Error] SerializedMember is null for property '{propertyInfo.Name}'.");
                return false;
            }

            var type = TypeUtils.GetTypeWithNamePriority(value, fallbackType, out var error);
            if (type == null)
            {
                stringBuilder?.AppendLine($"{padding}[Error] {error}");
                return false;
            }

            if (!TryDeserializeValueListInternal(
                reflector,
                jsonElement: value.valueJsonElement,
                type: type,
                name: propertyInfo.Name,
                result: out var enumerable,
                depth: depth + 1,
                stringBuilder: stringBuilder,
                logger: logger))
            {
                Print.FailedToSetProperty(ref obj, type, propertyInfo, depth, stringBuilder);
                return false;
            }

            propertyInfo.SetValue(obj, enumerable);

            stringBuilder?.AppendLine($"{padding}[Success] Property '{value?.name.ValueOrNull()}' modified to '{enumerable}'.");
            return true;
        }

        public override bool SetField(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            FieldInfo fieldInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!TryDeserializeValue(reflector,
                serializedMember: value,
                type: out var parsedValue,
                result: out var type,
                fallbackType: fallbackType,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger))
            {
                return false;
            }
            // TODO: Print previous and new value in stringBuilder
            fieldInfo.SetValue(obj, parsedValue);
            return true;
        }

        public override bool SetProperty(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            PropertyInfo propertyInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!TryDeserializeValue(reflector,
                serializedMember: value,
                type: out var parsedValue,
                result: out var type,
                fallbackType: fallbackType,
                depth: depth,
                stringBuilder: stringBuilder,
                logger: logger))
            {
                return false;
            }
            // TODO: Print previous and new value in stringBuilder
            propertyInfo.SetValue(obj, parsedValue);
            return true;
        }
    }
}