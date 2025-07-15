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
        public override int SerializationPriority(Type type, ILogger? logger = null)
        {
            if (type.IsArray)
                return MAX_DEPTH + 1;

            var isGenericList = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
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

            foreach (var element in enumerable)
                serializedList.Add(reflector.Serialize(element, type: element?.GetType(), name: $"[{index++}]", recursive: recursive, flags: flags, logger: logger));

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

        protected override bool SetValue(Reflector reflector, ref object? obj, Type type, JsonElement? value, ILogger? logger = null)
        {
            if (value == null || !value.HasValue)
            {
                obj = null;
                return true;
            }
            try
            {
                var success = true;
                var parsedList = JsonUtils.Deserialize<List<SerializedMember>>(value.Value);
                if (parsedList == null)
                {
                    obj = null;
                    return true;
                }
                var enumerable = parsedList
                    .Select((element, i) =>
                    {
                        if (element == null)
                            return null;

                        if (element.valueJsonElement == null)
                            return null;

                        if (element.valueJsonElement.HasValue == false)
                            return null;

                        var elementType = TypeUtils.GetType(element.typeName);
                        if (elementType == null)
                        {
                            if (logger != null)
                                logger.LogError($"[Error] Array element [{i}] Type '{element.typeName}' not found for deserialization.");
                            // throw new ArgumentException($"[Error] Array element [{i}] Type '{element.typeName}' not found for deserialization.");

                            success = false;
                            return null;
                        }

                        return JsonUtils.Deserialize(element.valueJsonElement.Value, elementType);
                    });

                if (!success)
                    return false;

                obj = type.IsArray
                    ? enumerable.ToArray() as IEnumerable<object>
                    : enumerable.ToList() as IEnumerable<object>;
                return true;
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"[Error] Failed to deserialize array: {ex.Message}");
                return false;
            }
        }

        public override bool SetAsField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedList = value?.valueJsonElement != null && value.valueJsonElement.HasValue
                ? JsonUtils.Deserialize<List<SerializedMember>>(value.valueJsonElement.Value)
                : TypeUtils.GetDefaultValue<List<SerializedMember>>();

            try
            {
                var success = true;
                var arrayStringBuilder = stringBuilder != null
                    ? new StringBuilder()
                    : null;
                var enumerable = parsedList
                    .Select((element, i) =>
                    {
                        var elementType = TypeUtils.GetType(element.typeName);
                        if (elementType == null)
                        {
                            if (logger != null)
                                logger.LogError($"[Error] Array element [{i}] Type '{element.typeName}' not found for deserialization.");

                            if (arrayStringBuilder != null)
                                arrayStringBuilder.AppendLine($"[Error] Array element [{i}] Type '{element.typeName}' not found for deserialization.");

                            success = false;
                            return null;
                        }

                        var elementValue = element.valueJsonElement != null && element.valueJsonElement.HasValue
                            ? JsonUtils.Deserialize(element.valueJsonElement.Value, elementType)
                            : TypeUtils.GetDefaultValue(type);
                        return elementValue;
                    });

                if (!success)
                {
                    if (stringBuilder != null)
                    {
                        stringBuilder.Append(arrayStringBuilder!.ToString());
                        stringBuilder.AppendLine($"[Error] Failed to set field '{value?.name}': Some elements could not be deserialized.");
                    }
                    return false;
                }

                fieldInfo.SetValue(obj, type.IsArray
                    ? enumerable.ToArray() as IEnumerable<object>
                    : enumerable.ToList() as IEnumerable<object>);

                stringBuilder?.AppendLine($"[Success] Field '{value?.name}' modified to '[{string.Join(", ", enumerable)}]'.");
                return true;
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"[Error] Failed to set field '{value?.name}': {ex.Message}");

                stringBuilder?.AppendLine($"[Error] Failed to set field '{value?.name}': {ex.Message}");
                return false;
            }
        }

        public override bool SetAsProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var hasValue = value?.valueJsonElement != null && value.valueJsonElement.HasValue;
            var parsedList = hasValue
                ? JsonUtils.Deserialize<List<SerializedMember>>(value!.valueJsonElement!.Value)
                : TypeUtils.GetDefaultValue<List<SerializedMember>>();

            try
            {
                var success = true;
                var arrayStringBuilder = stringBuilder != null
                    ? new StringBuilder()
                    : null;
                var enumerable = parsedList
                    .Select((element, i) =>
                    {
                        if (element == null)
                            return null;

                        if (element.valueJsonElement == null)
                            return null;

                        if (element.valueJsonElement.HasValue == false)
                            return null;

                        var elementType = TypeUtils.GetType(element.typeName);
                        if (elementType == null)
                        {
                            if (logger != null)
                                logger.LogError($"[Error] Array element [{i}] Type '{element.typeName}' not found for deserialization.");

                            if (arrayStringBuilder != null)
                                arrayStringBuilder.AppendLine($"[Error] Array element [{i}] Type '{element.typeName}' not found for deserialization.");

                            success = false;
                            return null;
                        }

                        return JsonUtils.Deserialize(element.valueJsonElement.Value, elementType);
                    });

                if (!success)
                {
                    if (stringBuilder != null)
                    {
                        stringBuilder.Append(arrayStringBuilder!.ToString());
                        stringBuilder.AppendLine($"[Error] Failed to set property '{value?.name}': Some elements could not be deserialized.");
                    }
                    return false;
                }

                propertyInfo.SetValue(obj, type.IsArray
                    ? enumerable.ToArray() as IEnumerable<object>
                    : enumerable.ToList() as IEnumerable<object>);

                stringBuilder?.AppendLine($"[Success] Property '{value?.name}' modified to '{enumerable}'.");
                return true;
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogError($"[Error] Failed to set property '{value?.name}': {ex.Message}");

                stringBuilder?.AppendLine($"[Error] Failed to set property '{value?.name}': {ex.Message}");
                return false;
            }
        }

        public override bool SetField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = value?.valueJsonElement != null && value.valueJsonElement.HasValue
                ? JsonUtils.Deserialize(value.valueJsonElement.Value, type)
                : TypeUtils.GetDefaultValue(type);
            fieldInfo.SetValue(obj, parsedValue);
            return true;
        }

        public override bool SetProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = value?.valueJsonElement != null && value.valueJsonElement.HasValue
                ? JsonUtils.Deserialize(value.valueJsonElement.Value, type)
                : TypeUtils.GetDefaultValue(type);
            propertyInfo.SetValue(obj, parsedValue);
            return true;
        }
    }
}