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
        public virtual StringBuilder? Populate(
            Reflector reflector,
            ref object? obj,
            SerializedMember data,
            Type? fallbackType = null,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            var objType = TypeUtils.GetTypeWithNamePriority(data, fallbackType, out var typeError) ?? obj?.GetType();
            if (objType == null)
            {
                stringBuilder?.AppendLine($"{padding}[Error] {typeError}");
                logger?.LogError($"{padding}{typeError}");
                return stringBuilder;
            }

            if (obj == null)
            {
                obj = TypeUtils.CreateInstance(objType); // Requires empty constructor or value type
                if (obj == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Object '{data.name.ValueOrNull()}' population failed: Object is null. Instance creation failed for type '{objType.GetTypeName(pretty: false)}'.");

                    return stringBuilder?.AppendLine($"{padding}[Error] Object '{data.name.ValueOrNull()}' population failed: Object is null. Instance creation failed for type '{objType.GetTypeName(pretty: false)}'.");
                }
            }

            if (!TypeUtils.IsCastable(obj.GetType(), objType))
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Type mismatch: '{data.typeName}' vs '{obj.GetType().GetTypeName(pretty: false).ValueOrNull()}'.");
                return stringBuilder?.AppendLine($"{padding}[Error] Type mismatch: '{data.typeName}' vs '{obj.GetType().GetTypeName(pretty: false).ValueOrNull()}'.");
            }

            if (AllowSetValue)
            {
                try
                {
                    var success = SetValue(reflector, ref obj, objType, data.valueJsonElement, depth: depth, stringBuilder: stringBuilder, logger: logger);
                    stringBuilder?.AppendLine(success
                        ? $"{padding}[Success] Object '{obj}' modified to\n{padding}```json\n{data.valueJsonElement}\n{padding}```"
                        : $"{padding}[Warning] Object '{obj}' was not modified to value \n{padding}```json\n{data.valueJsonElement}\n{padding}```");
                }
                catch (Exception ex)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Object '{obj}' modification failed: {ex.Message}");

                    stringBuilder?.AppendLine($"{padding}[Error] Object '{obj}' modification failed: {ex.Message}");
                }
            }

            var nextDepth = depth + 1;

            if (data.fields != null)
            {
                foreach (var field in data.fields)
                {
                    PopulateField(
                        reflector,
                        obj: ref obj,
                        objType: objType,
                        fieldValue: field,
                        depth: nextDepth,
                        stringBuilder: stringBuilder,
                        flags: flags,
                        logger: logger);
                }
            }

            if ((data.fields?.Count ?? 0) == 0)
            {
                if (logger?.IsEnabled(LogLevel.Information) == true)
                    logger.LogInformation($"{padding}[Info] No fields modified.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Info] No fields modified.");
            }

            if (data.props != null)
            {
                foreach (var property in data.props)
                {
                    PopulateProperty(
                        reflector,
                        obj: ref obj,
                        objType: objType,
                        propertyValue: property,
                        depth: nextDepth,
                        stringBuilder: stringBuilder,
                        flags: flags,
                        logger: logger);
                }
            }

            if ((data.props?.Count ?? 0) == 0)
            {
                if (logger?.IsEnabled(LogLevel.Information) == true)
                    logger.LogInformation($"{padding}[Info] No properties modified.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Info] No properties modified.");
            }

            return stringBuilder;
        }

        protected abstract bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            JsonElement? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null);

        protected virtual StringBuilder? PopulateField(
            Reflector reflector,
            ref object? obj,
            Type objType,
            SerializedMember fieldValue,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (string.IsNullOrEmpty(fieldValue.name))
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Field name is null or empty in serialized data: '{fieldValue.name.ValueOrNull()}'. Skipping.");
                return stringBuilder?.AppendLine($"{padding}{Error.FieldNameIsEmpty()}");
            }

            if (obj == null)
            {
                obj = TypeUtils.CreateInstance(objType); // Requires empty constructor or value type
                if (obj == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Field '{fieldValue.name.ValueOrNull()}' modification failed: Object is null.");

                    return stringBuilder?.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}' modification failed: Object is null.");
                }
            }
            var fieldInfo = obj.GetType().GetField(fieldValue.name, flags);
            if (fieldInfo == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Field '{fieldValue.name.ValueOrNull()}' not found. Make sure the name is right, it is case sensitive. Make sure this is a field, maybe is it a property?");

                return stringBuilder?.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}'. Make sure the name is right, it is case sensitive. Make sure this is a field, maybe is it a property?.");
            }

            var targetType = TypeUtils.GetTypeWithNamePriority(fieldValue, fieldInfo.FieldType, out var error);
            if (targetType == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Field '{fieldValue.name.ValueOrNull()}'. {error}");

                return stringBuilder?.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}'. {error}");
            }

            try
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}Modify field type='{fieldInfo.FieldType.GetTypeShortName()}', name='{fieldInfo.Name.ValueOrNull()}'.");

                stringBuilder?.AppendLine($"{padding}[Info] Modify field type='{fieldInfo.FieldType.GetTypeName(pretty: false).ValueOrNull()}', name='{fieldInfo.Name.ValueOrNull()}'.");

                var currentValue = fieldInfo.GetValue(obj);

                var result = reflector.Populate(
                    ref currentValue,
                    data: fieldValue,
                    fallbackObjType: targetType,
                    depth: depth + 1,
                    stringBuilder: stringBuilder,
                    flags: flags,
                    logger: logger);

                fieldInfo.SetValue(obj, currentValue);

                return result;

                // return stringBuilder?.AppendLine(success
                //     ? $"{padding}[Success] Field '{fieldValue.name.ValueOrNull()}' modified to value '{fieldValue.valueJsonElement}'."
                //     : $"{padding}[Error] Failed to modify field '{fieldValue.name.ValueOrNull()}' to value '{fieldValue.valueJsonElement}'. Read error above for more details.");
            }
            catch (Exception ex)
            {
                return stringBuilder?.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}' modification failed: {ex.Message}");
            }
        }

        protected virtual StringBuilder? PopulateProperty(
            Reflector reflector,
            ref object? obj,
            Type objType,
            SerializedMember propertyValue,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var padding = StringUtils.GetPadding(depth);

            if (string.IsNullOrEmpty(propertyValue.name))
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Property name is null or empty in serialized data: '{propertyValue.name.ValueOrNull()}'. Skipping.");
                return stringBuilder?.AppendLine($"{padding}{Error.FieldNameIsEmpty()}");
            }

            if (obj == null)
            {
                obj = TypeUtils.CreateInstance(objType); // Requires empty constructor or value type
                if (obj == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Property '{propertyValue.name.ValueOrNull()}' modification failed: Object is null.");

                    return stringBuilder?.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}' modification failed: Object is null.");
                }
            }
            var propInfo = obj.GetType().GetProperty(propertyValue.name, flags);
            if (propInfo == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Property '{propertyValue.name.ValueOrNull()}' not found. Make sure the name is right, it is case sensitive. Make sure this is a property, maybe is it a field?");

                return stringBuilder?.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}'. Make sure the name is right, it is case sensitive. Make sure this is a property, maybe is it a field?");
            }

            var targetType = TypeUtils.GetTypeWithNamePriority(propertyValue, propInfo.PropertyType, out var error);
            if (targetType == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Property '{propertyValue.name.ValueOrNull()}'. {error}");

                return stringBuilder?.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}'. {error}");
            }

            try
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}Modify property type='{propInfo.PropertyType.GetTypeName(pretty: false).ValueOrNull()}', name='{propInfo.Name.ValueOrNull()}'.");

                stringBuilder?.AppendLine($"{padding}[Info] Modify property type='{propInfo.PropertyType.GetTypeName(pretty: false).ValueOrNull()}', name='{propInfo.Name.ValueOrNull()}'.");

                var currentValue = propInfo.GetValue(obj);

                var result = reflector.Populate(
                    ref currentValue,
                    data: propertyValue,
                    fallbackObjType: targetType,
                    depth: depth + 1,
                    stringBuilder: stringBuilder,
                    flags: flags,
                    logger: logger);

                propInfo.SetValue(obj, currentValue);

                return result;

                // return stringBuilder?.AppendLine(success
                //     ? $"{padding}[Success] Property '{propertyValue.name.ValueOrNull()}' modified to value '{propertyValue.valueJsonElement}'."
                //     : $"{padding}[Error] Failed to modify property '{propertyValue.name.ValueOrNull()}' to value '{propertyValue.valueJsonElement}'. Read error above for more details.");
            }
            catch (Exception ex)
            {
                return stringBuilder?.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}' modification failed: {ex.Message}");
            }
        }

        public abstract bool SetAsField(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            FieldInfo fieldInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetAsProperty(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            PropertyInfo propertyInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetField(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            FieldInfo fieldInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);

        public abstract bool SetProperty(
            Reflector reflector,
            ref object? obj,
            Type fallbackType,
            PropertyInfo propertyInfo,
            SerializedMember? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            ILogger? logger = null);
    }
}