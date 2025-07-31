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
            var type = TypeUtils.GetTypeWithValuePriority(dataType, fallbackMember: data, out var typeError);
            if (type == null)
                return stringBuilder?.AppendLine($"{padding}[Error] {typeError}");

            if (obj == null)
                return stringBuilder?.AppendLine($"{padding}[Error] Object is null. Cannot populate {nameof(SerializedMember)}.{nameof(SerializedMember.typeName)} with value '{data.typeName}'.");

            TypeUtils.CastTo(obj, type, out var castError);
            if (castError != null)
                return stringBuilder?.AppendLine($"{padding}{castError}");

            if (!type.IsAssignableFrom(obj.GetType()))
                return stringBuilder?.AppendLine($"{padding}[Error] Type mismatch: '{data.typeName}' vs '{obj.GetType().GetTypeName(pretty: false).ValueOrNull()}'.");

            if (AllowSetValue)
            {
                try
                {
                    var success = SetValue(reflector, ref obj, type, data.valueJsonElement, depth: depth, stringBuilder: stringBuilder, logger: logger);
                    stringBuilder?.AppendLine(success
                        ? $"{padding}[Success] Object '{obj}' modified to\n{padding}```json\n{data.valueJsonElement}\n{padding}```"
                        : $"{padding}[Warning] Object '{obj}' was not modified to value \n{padding}```json\n{data.valueJsonElement}\n{padding}```");
                }
                catch (Exception ex)
                {
                    stringBuilder?.AppendLine($"{padding}[Error] Object '{obj}' modification failed: {ex.Message}");
                }
            }

            var nextDepth = depth + 1;

            if (data.fields != null)
                foreach (var field in data.fields)
                    ModifyField(reflector, ref obj, field, depth: nextDepth, stringBuilder: stringBuilder, flags: flags, logger: logger);

            if ((data.fields?.Count ?? 0) == 0)
            {
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Info] No fields modified.");
            }

            if (data.props != null)
                foreach (var property in data.props)
                    ModifyProperty(reflector, ref obj, property, depth: nextDepth, stringBuilder: stringBuilder, flags: flags, logger: logger);

            if ((data.props?.Count ?? 0) == 0)
            {
                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Info] No properties modified.");
            }

            return stringBuilder;
        }
        protected abstract bool SetValue(Reflector reflector, ref object? obj, Type type, JsonElement? value, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null);

        protected virtual StringBuilder? ModifyField(Reflector reflector, ref object? obj, SerializedMember fieldValue, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);
            if (string.IsNullOrEmpty(fieldValue.name))
                return stringBuilder?.AppendLine($"{padding}{Error.FieldNameIsEmpty()}");

            if (obj == null)
                return stringBuilder?.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}' modification failed: Object is null.");

            var fieldInfo = obj.GetType().GetField(fieldValue.name, flags);
            if (fieldInfo == null)
                return stringBuilder?.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}'. Make sure the name is right, it is case sensitive. Make sure this is a field, maybe is it a property?.");

            var targetType = TypeUtils.GetTypeWithNamePriority(fieldValue, fieldInfo.FieldType, out var error);
            if (targetType == null)
                return stringBuilder?.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}'. {error}");

            try
            {
                var success = false;
                foreach (var convertor in reflector.Convertors.BuildPopulatorsChain(targetType))
                    success |= convertor.SetAsField(reflector, ref obj, targetType, fieldInfo, value: fieldValue,
                        depth: depth, stringBuilder: stringBuilder,
                        flags: flags, logger: logger);

                return stringBuilder?.AppendLine(success
                    ? $"{padding}[Success] Field '{fieldValue.name.ValueOrNull()}' modified to value '{fieldValue.valueJsonElement}'."
                    : $"{padding}[Error] Failed to modify field '{fieldValue.name.ValueOrNull()}' to value '{fieldValue.valueJsonElement}'. Read error above for more details.");
            }
            catch (Exception ex)
            {
                return stringBuilder?.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}' modification failed: {ex.Message}");
            }
        }

        protected virtual StringBuilder? ModifyProperty(Reflector reflector, ref object? obj, SerializedMember propertyValue, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (string.IsNullOrEmpty(propertyValue.name))
                return stringBuilder?.AppendLine($"{padding}{Error.PropertyNameIsEmpty()}");

            if (obj == null)
                return stringBuilder?.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}' modification failed: Object is null.");

            var propInfo = obj.GetType().GetProperty(propertyValue.name, flags);
            if (propInfo == null)
            {
                return stringBuilder?.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}' not found. Make sure the name is right, it is case sensitive. Make sure this is a property, maybe is it a field?.");
            }
            if (!propInfo.CanWrite)
            {
                return stringBuilder?.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}' is not writable. Can't modify property '{propertyValue.name}'.");
            }

            var targetType = TypeUtils.GetTypeWithNamePriority(propertyValue, propInfo.PropertyType, out var error);
            if (targetType == null)
                return stringBuilder?.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}'. {error}");

            try
            {
                var success = false;
                foreach (var convertor in reflector.Convertors.BuildPopulatorsChain(targetType))
                    success |= convertor.SetAsProperty(reflector, ref obj, targetType, propInfo, value: propertyValue,
                        depth: depth, stringBuilder: stringBuilder,
                        flags: flags, logger: logger);

                return stringBuilder?.AppendLine(success
                    ? $"{padding}[Success] Property '{propertyValue.name.ValueOrNull()}' modified to value '{propertyValue.valueJsonElement}'."
                    : $"{padding}[Error] Failed to modify property '{propertyValue.name.ValueOrNull()}' to value '{propertyValue.valueJsonElement}'. Read error above for more details.");
            }
            catch (Exception ex)
            {
                return stringBuilder?.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}' modification failed: {ex.Message}");
            }
        }

        public abstract bool SetAsField(Reflector reflector, ref object? obj, Type fallbackType, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetAsProperty(Reflector reflector, ref object? obj, Type fallbackType, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetField(Reflector reflector, ref object? obj, Type type, FieldInfo fieldInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetProperty(Reflector reflector, ref object? obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);
    }
}