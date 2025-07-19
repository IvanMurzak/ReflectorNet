using System;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using static com.IvanMurzak.ReflectorNet.Reflector;

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        public virtual StringBuilder? Populate(Reflector reflector, ref object? obj, SerializedMember data, Type? dataType = null, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            var type = dataType;
            if (type == null)
            {
                if (string.IsNullOrEmpty(data.typeName))
                    return stringBuilder?.AppendLine(padding + $"[Error] {nameof(SerializedMember)}.{nameof(SerializedMember.typeName)} is null or empty.");

                type = TypeUtils.GetType(data.typeName);
                if (type == null)
                    return stringBuilder?.AppendLine(padding + $"[Error] {nameof(SerializedMember)}.{nameof(SerializedMember.typeName)} with name '{data.typeName}' not found.");
            }

            if (obj == null)
                return stringBuilder?.AppendLine(padding + $"[Error] Object is null. Cannot populate {nameof(SerializedMember)}.{nameof(SerializedMember.typeName)} with value '{data.typeName}'.");

            TypeUtils.CastTo(obj, type, out var error);
            if (error != null)
                return stringBuilder?.AppendLine(padding + error);

            if (!type.IsAssignableFrom(obj.GetType()))
                return stringBuilder?.AppendLine(padding + $"[Error] Type mismatch: '{data.typeName}' vs '{obj.GetType().FullName ?? "null"}'.");

            if (data.valueJsonElement != null)
            {
                try
                {
                    var success = SetValue(reflector, ref obj, type, data.valueJsonElement, stringBuilder, logger: logger);
                    stringBuilder?.AppendLine(padding + (success
                        ? $"[Success] Object '{obj}' modified to\n```\n{data.valueJsonElement}\n```"
                        : $"[Error] Object '{obj}' modification failed."));
                }
                catch (Exception ex)
                {
                    stringBuilder?.AppendLine(padding + $"[Error] Object '{obj}' modification failed: {ex.Message}");
                }
            }

            var nextDepth = depth + 1;

            if (data.fields != null)
                foreach (var field in data.fields)
                    ModifyField(reflector, ref obj, field, depth: nextDepth, stringBuilder: stringBuilder, flags: flags, logger: logger);

            if (data.props != null)
                foreach (var property in data.props)
                    ModifyProperty(reflector, ref obj, property, depth: nextDepth, stringBuilder: stringBuilder, flags: flags, logger: logger);

            return stringBuilder;
        }
        protected abstract bool SetValue(Reflector reflector, ref object? obj, Type type, JsonElement? value, StringBuilder? stringBuilder = null, ILogger? logger = null);

        protected virtual StringBuilder? ModifyField(Reflector reflector, ref object? obj, SerializedMember fieldValue, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            if (string.IsNullOrEmpty(fieldValue.name))
                return stringBuilder?.AppendLine(padding + Error.ComponentFieldNameIsEmpty());

            if (string.IsNullOrEmpty(fieldValue.typeName))
                return stringBuilder?.AppendLine(padding + Error.ComponentFieldTypeIsEmpty());

            if (obj == null)
                return stringBuilder?.AppendLine(padding + $"[Error] Field '{fieldValue.name}' modification failed: Object is null.");

            var fieldInfo = obj.GetType().GetField(fieldValue.name, flags);
            if (fieldInfo == null)
                return stringBuilder?.AppendLine(padding + $"[Error] Field '{fieldValue.name}'. Make sure the name is right, it is case sensitive. Make sure this is a field, maybe is it a property?.");

            var targetType = TypeUtils.GetType(fieldValue.typeName);
            if (targetType == null)
                return stringBuilder?.AppendLine(padding + Error.InvalidComponentFieldType(fieldValue, fieldInfo));

            try
            {
                foreach (var convertor in reflector.Convertors.BuildPopulatorsChain(targetType))
                    convertor.SetAsField(reflector, ref obj, targetType, fieldInfo, value: fieldValue, stringBuilder: stringBuilder, flags: flags, logger: logger);

                return stringBuilder?.AppendLine(padding + $"[Success] Field '{fieldValue.name}' modified to '{fieldValue.valueJsonElement}'.");
            }
            catch (Exception ex)
            {
                var message = $"[Error] Field '{fieldValue.name}' modification failed: {ex.Message}";
                return stringBuilder?.AppendLine(padding + message);
            }
        }

        protected virtual StringBuilder? ModifyProperty(Reflector reflector, ref object? obj, SerializedMember propertyValue, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            if (string.IsNullOrEmpty(propertyValue.name))
                return stringBuilder?.AppendLine(padding + Error.ComponentPropertyNameIsEmpty());

            if (string.IsNullOrEmpty(propertyValue.typeName))
                return stringBuilder?.AppendLine(padding + Error.ComponentPropertyTypeIsEmpty());

            if (obj == null)
                return stringBuilder?.AppendLine(padding + $"[Error] Property '{propertyValue.name}' modification failed: Object is null.");

            var propInfo = obj.GetType().GetProperty(propertyValue.name, flags);
            if (propInfo == null)
            {
                var warningMessage = $"[Error] Property '{propertyValue.name}' not found. Make sure the name is right, it is case sensitive. Make sure this is a property, maybe is it a field?.";
                return stringBuilder?.AppendLine(padding + warningMessage);
            }
            if (!propInfo.CanWrite)
            {
                var warningMessage = $"[Error] Property '{propertyValue.name}' is not writable. Can't modify property '{propertyValue.name}'.";
                return stringBuilder?.AppendLine(padding + warningMessage);
            }

            var targetType = TypeUtils.GetType(propertyValue.typeName);
            if (targetType == null)
                return stringBuilder?.AppendLine(padding + Error.InvalidComponentPropertyType(propertyValue, propInfo));

            try
            {
                foreach (var convertor in reflector.Convertors.BuildPopulatorsChain(targetType))
                    convertor.SetAsProperty(reflector, ref obj, targetType, propInfo, value: propertyValue, stringBuilder: stringBuilder, flags: flags, logger: logger);

                return stringBuilder;
            }
            catch (Exception ex)
            {
                var message = $"[Error] Property '{propertyValue.name}' modification failed: {ex.Message}";
                return stringBuilder?.AppendLine(padding + message);
            }
        }

        public abstract bool SetAsField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetAsProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);
    }
}