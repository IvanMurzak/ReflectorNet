using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public partial class GenericReflectionConvertor<T> : NotArrayReflectionConvertor<T>
    {
        protected override SerializedMember InternalSerialize(Reflector reflector, object obj, Type type, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var isStruct = type.IsValueType && !type.IsPrimitive && !type.IsEnum;
            if (type.IsClass || isStruct)
            {
                return recursive
                    ? new SerializedMember()
                    {
                        name = name,
                        typeName = type.FullName ?? string.Empty,
                        fields = SerializeFields(reflector, obj, flags),
                        props = SerializeProperties(reflector, obj, flags),
                        valueJsonElement = new JsonObject().ToJsonElement()
                    }
                    : SerializedMember.FromJson(type, JsonUtils.Serialize(obj), name: name);
            }
            throw new ArgumentException($"Unsupported type: {type.FullName}");
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
            var parsedValue = value == null
                ? TypeUtils.GetDefaultValue(type)
                : JsonUtils.Deserialize(value.Value, type);
            obj = parsedValue;
            return true;
        }

        public override bool SetAsField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = value?.valueJsonElement == null
                ? TypeUtils.GetDefaultValue(type)
                : JsonUtils.Deserialize(value.valueJsonElement.Value, type);
            fieldInfo.SetValue(obj, parsedValue);
            stringBuilder?.AppendLine($"[Success] Field '{fieldInfo.Name}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetAsProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = value?.valueJsonElement == null
                ? TypeUtils.GetDefaultValue(type)
                : JsonUtils.Deserialize(value.valueJsonElement.Value, type);
            propertyInfo.SetValue(obj, parsedValue);
            stringBuilder?.AppendLine($"[Success] Property '{propertyInfo.Name}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = value?.valueJsonElement == null
                ? TypeUtils.GetDefaultValue(type)
                : JsonUtils.Deserialize(value.valueJsonElement.Value, type);
            fieldInfo.SetValue(obj, parsedValue);
            return true;
        }

        public override bool SetProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = value?.valueJsonElement == null
                ? TypeUtils.GetDefaultValue(type)
                : JsonUtils.Deserialize(value.valueJsonElement.Value, type);
            propertyInfo.SetValue(obj, parsedValue);
            return true;
        }
    }
}