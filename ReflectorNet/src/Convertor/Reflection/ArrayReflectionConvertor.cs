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

        public override bool SetAsField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!value.TryDeserializeEnumerable(type, out var enumerable, depth: depth + 1, stringBuilder: stringBuilder))
            {
                stringBuilder?.AppendLine($"[Error] Failed to set field '{value?.name}'");
                return false;
            }

            fieldInfo.SetValue(obj, enumerable);

            stringBuilder?.AppendLine($"[Success] Field '{value?.name}' modified to '[{string.Join(", ", enumerable)}]'.");
            return true;
        }

        public override bool SetAsProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!value.TryDeserializeEnumerable(type, out var parsedValue, depth: depth + 1, stringBuilder: stringBuilder))
            {
                stringBuilder?.AppendLine($"[Error] Failed to set property '{value?.name}'");
                return false;
            }

            propertyInfo.SetValue(obj, parsedValue);

            stringBuilder?.AppendLine($"[Success] Property '{value?.name}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!value.TryDeserialize(type, out var parsedValue))
                return false;

            // TODO: Print previous and new value in stringBuilder
            fieldInfo.SetValue(obj, parsedValue);
            return true;
        }

        public override bool SetProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            if (!value.TryDeserialize(type, out var parsedValue))
                return false;

            // TODO: Print previous and new value in stringBuilder
            propertyInfo.SetValue(obj, parsedValue);
            return true;
        }
    }
}