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
        {
            type ??= obj?.GetType() ?? typeof(object);
            return SerializedMember.FromValue(type, obj, name: name);
        }

        public override IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => null;

        public override IEnumerable<PropertyInfo>? GetSerializableProperties(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => null;

        protected override bool SetValue(Reflector reflector, ref object? obj, Type type, JsonElement? value, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            var parsedValue = value.Deserialize(type);
            Print.SetNewValue(ref obj, ref parsedValue, type, depth, stringBuilder);
            obj = parsedValue;
            return true;
        }

        public override bool SetAsField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!value.TryDeserialize(type, out var parsedValue, logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to deserialize value for property '{value?.name.ValueOrNull()}'.");
                return false;
            }

            fieldInfo.SetValue(obj, parsedValue);
            stringBuilder?.AppendLine($"{padding}[Success] Field '{value?.name.ValueOrNull()}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetAsProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!value.TryDeserialize(type, out var parsedValue, logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to deserialize value for property '{value?.name.ValueOrNull()}'.");
                return false;
            }

            propertyInfo.SetValue(obj, parsedValue);
            stringBuilder?.AppendLine($"{padding}[Success] Property '{value?.name.ValueOrNull()}' modified to '{parsedValue}'.");
            return true;
        }

        public override bool SetField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!value.TryDeserialize(type, out var parsedValue, logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to deserialize value for field '{value?.name.ValueOrNull()}'.");
                return false;
            }

            // TODO: Print previous and new value in stringBuilder
            fieldInfo.SetValue(obj, parsedValue);
            return true;
        }

        public override bool SetProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (!value.TryDeserialize(type, out var parsedValue, logger: logger))
            {
                stringBuilder?.AppendLine($"{padding}[Error] Failed to deserialize value for property '{value?.name.ValueOrNull()}'.");
                return false;
            }

            // TODO: Print previous and new value in stringBuilder
            propertyInfo.SetValue(obj, parsedValue);
            return true;
        }
    }
}