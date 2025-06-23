using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public partial class PrimitiveReflectionConvertor : NotArrayReflectionConvertor<object>
    {
        public override int SerializationPriority(Type type, ILogger? logger = null)
        {
            var isPrimitive = TypeUtils.IsPrimitive(type);

            return isPrimitive
                ? MAX_DEPTH + 1
                : 0;
        }
        protected override SerializedMember InternalSerialize(Reflector reflector, object? obj, Type? type, string? name = null, bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
            => SerializedMember.FromValue(type, obj, name: name);

        public override IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => null;

        public override IEnumerable<PropertyInfo>? GetSerializableProperties(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => null;

        protected override bool SetValue(Reflector reflector, ref object obj, Type type, JsonElement? value, ILogger? logger = null)
        {
            var parsedValue = JsonUtils.Deserialize(value.Value, type);
            obj = parsedValue;
            return true;
        }

        public override bool SetAsField(Reflector reflector, ref object obj, Type type, FieldInfo fieldInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = JsonUtils.Deserialize(value.valueJsonElement.Value, type);
            fieldInfo.SetValue(obj, parsedValue);
            stringBuilder?.AppendLine($"[Success] Field '{value.name}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetAsProperty(Reflector reflector, ref object obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = JsonUtils.Deserialize(value.valueJsonElement.Value, type);
            propertyInfo.SetValue(obj, parsedValue);
            stringBuilder?.AppendLine($"[Success] Property '{value.name}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetField(Reflector reflector, ref object obj, Type type, FieldInfo fieldInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = JsonUtils.Deserialize(value.valueJsonElement.Value, type);
            fieldInfo.SetValue(obj, parsedValue);
            return true;
        }

        public override bool SetProperty(Reflector reflector, ref object obj, Type type, PropertyInfo propertyInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var parsedValue = JsonUtils.Deserialize(value.valueJsonElement.Value, type);
            propertyInfo.SetValue(obj, parsedValue);
            return true;
        }
    }
}