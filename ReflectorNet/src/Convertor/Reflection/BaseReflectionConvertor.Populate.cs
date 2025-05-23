#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Reflection;
using System.Text;
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Data.Unity;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;
using static com.IvanMurzak.ReflectorNet.Reflection.Reflector;

namespace com.IvanMurzak.ReflectorNet.Reflection.Convertor
{
    public abstract partial class BaseReflectionConvertor<T> : IReflectionConvertor
    {
        public virtual StringBuilder? Populate(Reflector reflector, ref object obj, SerializedMember data, int depth = 0, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(data.typeName))
                return stringBuilder?.AppendLine("[Error] Type is null or empty.");

            var type = TypeUtils.GetType(data.typeName);
            if (type == null)
                return stringBuilder?.AppendLine($"[Error] Type not found: {data.typeName}");

            TypeUtils.CastTo(obj, data.typeName, out var error);
            if (error != null)
                return stringBuilder?.AppendLine(error);

            if (!type.IsAssignableFrom(obj.GetType()))
                return stringBuilder?.AppendLine($"[Error] Type mismatch: {data.typeName} vs {obj.GetType().FullName}");

            if (data.valueJsonElement != null)
            {
                try
                {
                    SetValue(reflector, ref obj, type, data.valueJsonElement, logger);
                    stringBuilder?.AppendLine(new string(' ', depth) + $"[Success] Object '{obj}' modified to '{data.valueJsonElement}'.");
                }
                catch (Exception ex)
                {
                    stringBuilder?.AppendLine(new string(' ', depth) + $"[Error] Object '{obj}' modification failed: {ex.Message}");
                }
            }

            var nextDepth = depth + 1;

            if (data.fields != null)
                foreach (var field in data.fields)
                    ModifyField(reflector, ref obj, field, stringBuilder, depth: nextDepth, flags: flags, logger: logger);

            if (data.props != null)
                foreach (var property in data.props)
                    ModifyProperty(reflector, ref obj, property, stringBuilder, depth: nextDepth, flags: flags, logger: logger);

            return stringBuilder;
        }
        protected abstract bool SetValue(Reflector reflector, ref object obj, Type type, JsonElement? value, ILogger? logger = null);

        protected virtual StringBuilder? ModifyField(Reflector reflector, ref object obj, SerializedMember fieldValue, StringBuilder? stringBuilder = null, int depth = 0,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(fieldValue.name))
                return stringBuilder?.AppendLine(new string(' ', depth) + Error.ComponentFieldNameIsEmpty());

            if (string.IsNullOrEmpty(fieldValue.typeName))
                return stringBuilder?.AppendLine(new string(' ', depth) + Error.ComponentFieldTypeIsEmpty());

            var fieldInfo = obj.GetType().GetField(fieldValue.name, flags);
            if (fieldInfo == null)
                return stringBuilder?.AppendLine(new string(' ', depth) + $"[Error] Field '{fieldValue.name}'. Make sure the name is right, it is case sensitive. Make sure this is a field, maybe is it a property?.");

            var targetType = TypeUtils.GetType(fieldValue.typeName);
            if (targetType == null)
                return stringBuilder?.AppendLine(new string(' ', depth) + Error.InvalidComponentFieldType(fieldValue, fieldInfo));

            try
            {
                foreach (var convertor in reflector.Convertors.BuildPopulatorsChain(targetType))
                    convertor.SetAsField(reflector, ref obj, targetType, fieldInfo, value: fieldValue, stringBuilder: stringBuilder, flags: flags, logger: logger);

                return stringBuilder?.AppendLine(new string(' ', depth) + $"[Success] Field '{fieldValue.name}' modified to '{fieldValue.valueJsonElement}'.");
            }
            catch (Exception ex)
            {
                var message = $"[Error] Field '{fieldValue.name}' modification failed: {ex.Message}";
                return stringBuilder?.AppendLine(new string(' ', depth) + message);
            }
        }

        protected virtual StringBuilder? ModifyProperty(Reflector reflector, ref object obj, SerializedMember propertyValue, StringBuilder? stringBuilder = null, int depth = 0,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(propertyValue.name))
                return stringBuilder?.AppendLine(new string(' ', depth) + Error.ComponentPropertyNameIsEmpty());

            if (string.IsNullOrEmpty(propertyValue.typeName))
                return stringBuilder?.AppendLine(new string(' ', depth) + Error.ComponentPropertyTypeIsEmpty());

            var propInfo = obj.GetType().GetProperty(propertyValue.name, flags);
            if (propInfo == null)
            {
                var warningMessage = $"[Error] Property '{propertyValue.name}' not found. Make sure the name is right, it is case sensitive. Make sure this is a property, maybe is it a field?.";
                return stringBuilder?.AppendLine(new string(' ', depth) + warningMessage);
            }
            if (!propInfo.CanWrite)
            {
                var warningMessage = $"[Error] Property '{propertyValue.name}' is not writable. Can't modify property '{propertyValue.name}'.";
                return stringBuilder?.AppendLine(new string(' ', depth) + warningMessage);
            }

            var targetType = TypeUtils.GetType(propertyValue.typeName);
            if (targetType == null)
                return stringBuilder?.AppendLine(new string(' ', depth) + Error.InvalidComponentPropertyType(propertyValue, propInfo));

            try
            {
                foreach (var convertor in reflector.Convertors.BuildPopulatorsChain(targetType))
                    convertor.SetAsProperty(reflector, ref obj, targetType, propInfo, value: propertyValue, stringBuilder: stringBuilder, flags: flags, logger: logger);

                return stringBuilder;
            }
            catch (Exception ex)
            {
                var message = $"[Error] Property '{propertyValue.name}' modification failed: {ex.Message}";
                return stringBuilder?.AppendLine(new string(' ', depth) + message);
            }
        }

        public abstract bool SetAsField(Reflector reflector, ref object obj, Type type, FieldInfo fieldInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetAsProperty(Reflector reflector, ref object obj, Type type, PropertyInfo propertyInfo, SerializedMember? value, StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetField(Reflector reflector, ref object obj, Type type, FieldInfo fieldInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetProperty(Reflector reflector, ref object obj, Type type, PropertyInfo propertyInfo, SerializedMember? value,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);
    }
}