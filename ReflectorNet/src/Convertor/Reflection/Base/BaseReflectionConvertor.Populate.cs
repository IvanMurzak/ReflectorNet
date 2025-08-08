/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Reflection;
using System.Linq;
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
        public virtual bool TryPopulate(
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
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Failed to determine type for object '{data.name.ValueOrNull()}'. {typeError}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Failed to determine type for object '{data.name.ValueOrNull()}'. {typeError}");

                return false;
            }

            if (obj == null)
            {
                // obj = CreateInstance(reflector, objType);
                obj = reflector.Deserialize(
                    data: data,
                    fallbackType: objType,
                    depth: depth,
                    stringBuilder: stringBuilder,
                    logger: logger);

                if (obj == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Object '{data.name.ValueOrNull()}' population failed: Object is null. Instance creation failed for type '{objType.GetTypeName(pretty: false)}'.");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Object '{data.name.ValueOrNull()}' population failed: Object is null. Instance creation failed for type '{objType.GetTypeName(pretty: false)}'.");

                    return false;
                }

                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}Object '{data.name.ValueOrNull()}' populated with type '{objType.GetTypeName(pretty: false)}'.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Success] Object '{data.name.ValueOrNull()}' populated with type '{objType.GetTypeName(pretty: false)}'.");

                return true;
            }

            if (!TypeUtils.IsCastable(obj.GetType(), objType))
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Type mismatch: '{data.typeName}' vs '{obj.GetType().GetTypeName(pretty: false).ValueOrNull()}'.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Type mismatch: '{data.typeName}' vs '{obj.GetType().GetTypeName(pretty: false).ValueOrNull()}'.");

                return false;
            }

            var overallSuccess = true;
            if (AllowSetValue)
            {
                try
                {
                    var success = SetValue(
                        reflector: reflector,
                        obj: ref obj,
                        type: objType,
                        value: data.valueJsonElement,
                        depth: depth,
                        stringBuilder: stringBuilder,
                        logger: logger);

                    overallSuccess &= success;

                    if (success)
                    {
                        if (logger?.IsEnabled(LogLevel.Information) == true)
                            logger.LogInformation($"{padding}[Success] Value '{obj}' modified to\n{padding}```json\n{data.valueJsonElement}\n{padding}```");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Success] Value '{obj}' modified to\n{padding}```json\n{data.valueJsonElement}\n{padding}```");
                    }
                    else
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{padding}Value '{obj}' was not modified to value \n{padding}```json\n{data.valueJsonElement}\n{padding}```");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{padding}[Warning] Value '{obj}' was not modified to value \n{padding}```json\n{data.valueJsonElement}\n{padding}```");
                    }
                }
                catch (Exception ex)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError(ex, $"{padding}Value '{obj}' modification failed: {ex.Message}");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Value '{obj}' modification failed: {ex.Message}");
                }
            }

            var nextDepth = depth + 1;
            var nextPadding = StringUtils.GetPadding(nextDepth);

            if (data.fields != null)
            {
                foreach (var field in data.fields)
                {
                    var success = TryPopulateField(
                        reflector,
                        obj: ref obj,
                        objType: objType,
                        fieldValue: field,
                        depth: nextDepth,
                        stringBuilder: stringBuilder,
                        flags: flags,
                        logger: logger);

                    overallSuccess &= success;

                    if (success)
                    {
                        if (logger?.IsEnabled(LogLevel.Information) == true)
                            logger.LogInformation($"{nextPadding}[Success] Field '{field.name}' populated.");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{nextPadding}[Success] Field '{field.name}' populated.");
                    }
                    else
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{nextPadding}Field '{field.name}' was not populated.");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{nextPadding}[Warning] Field '{field.name}' was not populated.");
                    }
                }
            }

            if ((data.fields?.Count ?? 0) == 0)
            {
                if (logger?.IsEnabled(LogLevel.Information) == true)
                    logger.LogInformation($"{nextPadding}No fields populated.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{nextPadding}[Info] No fields populated.");
            }

            if (data.props != null)
            {
                foreach (var property in data.props)
                {
                    var success = TryPopulateProperty(
                        reflector,
                        obj: ref obj,
                        objType: objType,
                        propertyValue: property,
                        depth: nextDepth,
                        stringBuilder: stringBuilder,
                        flags: flags,
                        logger: logger);

                    overallSuccess &= success;

                    if (success)
                    {
                        if (logger?.IsEnabled(LogLevel.Information) == true)
                            logger.LogInformation($"{nextPadding}[Success] Property '{property.name}' populated.");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{nextPadding}[Success] Property '{property.name}' populated.");
                    }
                    else
                    {
                        if (logger?.IsEnabled(LogLevel.Warning) == true)
                            logger.LogWarning($"{nextPadding}Property '{property.name}' was not populated.");

                        if (stringBuilder != null)
                            stringBuilder.AppendLine($"{nextPadding}[Warning] Property '{property.name}' was not populated.");
                    }
                }
            }

            if ((data.props?.Count ?? 0) == 0)
            {
                if (logger?.IsEnabled(LogLevel.Information) == true)
                    logger.LogInformation($"{nextPadding}No properties populated.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{nextPadding}[Info] No properties populated.");
            }

            return overallSuccess;
        }

        protected abstract bool SetValue(
            Reflector reflector,
            ref object? obj,
            Type type,
            JsonElement? value,
            int depth = 0,
            StringBuilder? stringBuilder = null,
            ILogger? logger = null);

        protected virtual bool TryPopulateField(
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

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}{Error.FieldNameIsEmpty()}");

                return false;
            }

            if (obj == null)
            {
                // obj = CreateInstance(reflector, objType);
                obj = reflector.Deserialize(
                    data: fieldValue,
                    fallbackType: objType,
                    depth: depth,
                    stringBuilder: stringBuilder,
                    logger: logger);

                if (obj == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Field '{fieldValue.name.ValueOrNull()}' modification failed: Object is null.");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}' modification failed: Object is null.");

                    return false;
                }
            }
            var fieldInfo = obj.GetType().GetField(fieldValue.name, flags);
            if (fieldInfo == null)
            {
                var fieldNames = GetSerializableFields(
                        reflector: reflector,
                        objType: obj.GetType(),
                        flags: flags,
                        logger: logger)
                    ?.Select(f => f.Name)
                    ?.Concat(GetAdditionalSerializableFields(
                        reflector: reflector,
                        objType: obj.GetType(),
                        flags: flags,
                        logger: logger))
                    ?.ToList();

                var propNames = GetSerializableProperties(
                        reflector: reflector,
                        objType: obj.GetType(),
                        flags: flags,
                        logger: logger)
                    ?.Select(f => f.Name)
                    ?.Concat(GetAdditionalSerializableProperties(
                        reflector: reflector,
                        objType: obj.GetType(),
                        flags: flags,
                        logger: logger))
                    ?.ToList();

                var fieldsCount = fieldNames?.Count ?? 0;
                var propsCount = propNames?.Count ?? 0;

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Field '{fieldValue.name.ValueOrNull()}' not found. Make sure the name is right, it is case sensitive. Make sure this is a field, maybe is it a property?"
                        + $"\n{padding}"
                        + (fieldsCount > 0 ? $"Available fields: {string.Join(", ", fieldNames!)}" : "No available fields.")
                        + $"\n{padding}"
                        + (propsCount > 0 ? $"Available properties: {string.Join(", ", propNames!)}" : "No available properties.")
                    );

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}'. Make sure the name is right, it is case sensitive. Make sure this is a field, maybe is it a property?"
                        + $"\n{padding}"
                        + (fieldsCount > 0 ? $"Available fields: {string.Join(", ", fieldNames!)}" : "No available fields.")
                        + $"\n{padding}"
                        + (propsCount > 0 ? $"Available properties: {string.Join(", ", propNames!)}" : "No available properties.")
                    );

                return false;
            }

            var targetType = TypeUtils.GetTypeWithNamePriority(fieldValue, fieldInfo.FieldType, out var error);
            if (targetType == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Field '{fieldValue.name.ValueOrNull()}'. {error}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}'. {error}");

                return false;
            }

            try
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}Populate field type='{fieldInfo.FieldType.GetTypeShortName()}', name='{fieldInfo.Name.ValueOrNull()}'. Convertor='{GetType().GetTypeShortName()}'.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Info] Populate field type='{fieldInfo.FieldType.GetTypeName(pretty: false).ValueOrNull()}', name='{fieldInfo.Name.ValueOrNull()}'. Convertor='{GetType().GetTypeShortName()}'.");

                var currentValue = fieldInfo.GetValue(obj);

                var success = reflector.TryPopulate(
                    ref currentValue,
                    data: fieldValue,
                    fallbackObjType: targetType,
                    depth: depth + 1,
                    stringBuilder: stringBuilder,
                    flags: flags,
                    logger: logger);

                if (success)
                    fieldInfo.SetValue(obj, currentValue);

                return success;
            }
            catch (Exception ex)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError(ex, $"{padding}Field '{fieldValue.name.ValueOrNull()}' modification failed: {ex.Message}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Field '{fieldValue.name.ValueOrNull()}' modification failed: {ex.Message}");

                return false;
            }
        }

        protected virtual bool TryPopulateProperty(
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

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Property name is null or empty in serialized data: '{propertyValue.name.ValueOrNull()}'. Skipping.");

                return false;
            }

            if (obj == null)
            {
                // obj = CreateInstance(reflector, objType);
                obj = reflector.Deserialize(
                    data: propertyValue,
                    fallbackType: objType,
                    depth: depth,
                    stringBuilder: stringBuilder,
                    logger: logger);

                if (obj == null)
                {
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}Property '{propertyValue.name.ValueOrNull()}' modification failed: Object is null.");

                    if (stringBuilder != null)
                        stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}' modification failed: Object is null.");

                    return false;
                }
            }
            var propInfo = obj.GetType().GetProperty(propertyValue.name, flags);
            if (propInfo == null)
            {
                var fieldNames = GetSerializableFields(
                        reflector: reflector,
                        objType: obj.GetType(),
                        flags: flags,
                        logger: logger)
                    ?.Select(f => f.Name)
                    ?.Concat(GetAdditionalSerializableFields(
                        reflector: reflector,
                        objType: obj.GetType(),
                        flags: flags,
                        logger: logger))
                    ?.ToList();

                var propNames = GetSerializableProperties(
                        reflector: reflector,
                        objType: obj.GetType(),
                        flags: flags,
                        logger: logger)
                    ?.Select(f => f.Name)
                    ?.Concat(GetAdditionalSerializableProperties(
                        reflector: reflector,
                        objType: obj.GetType(),
                        flags: flags,
                        logger: logger))
                    ?.ToList();

                var fieldsCount = fieldNames?.Count ?? 0;
                var propsCount = propNames?.Count ?? 0;

                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Property '{propertyValue.name.ValueOrNull()}' not found. Make sure the name is right, it is case sensitive. Make sure this is a property, maybe is it a field?"
                        + $"\n{padding}"
                        + (propsCount > 0 ? $"Available properties: {string.Join(", ", propNames!)}" : "No available properties.")
                        + $"\n{padding}"
                        + (fieldsCount > 0 ? $"Available fields: {string.Join(", ", fieldNames!)}" : "No available fields.")
                    );

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}'. Make sure the name is right, it is case sensitive. Make sure this is a property, maybe is it a field?"
                        + $"\n{padding}"
                        + (propsCount > 0 ? $"Available properties: {string.Join(", ", propNames!)}" : "No available properties.")
                        + $"\n{padding}"
                        + (fieldsCount > 0 ? $"Available fields: {string.Join(", ", fieldNames!)}" : "No available fields.")
                    );
                return false;
            }

            var targetType = TypeUtils.GetTypeWithNamePriority(propertyValue, propInfo.PropertyType, out var error);
            if (targetType == null)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}Property '{propertyValue.name.ValueOrNull()}'. {error}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}'. {error}");

                return false;
            }

            try
            {
                if (logger?.IsEnabled(LogLevel.Trace) == true)
                    logger.LogTrace($"{padding}Populate property type='{propInfo.PropertyType.GetTypeName(pretty: false).ValueOrNull()}', name='{propInfo.Name.ValueOrNull()}'. Convertor='{GetType().GetTypeShortName()}'.");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Info] Populate property type='{propInfo.PropertyType.GetTypeName(pretty: false).ValueOrNull()}', name='{propInfo.Name.ValueOrNull()}'. Convertor='{GetType().GetTypeShortName()}'.");

                var currentValue = propInfo.GetValue(obj);

                var success = reflector.TryPopulate(
                    ref currentValue,
                    data: propertyValue,
                    fallbackObjType: targetType,
                    depth: depth + 1,
                    stringBuilder: stringBuilder,
                    flags: flags,
                    logger: logger);

                if (success)
                    propInfo.SetValue(obj, currentValue);

                return success;
            }
            catch (Exception ex)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError(ex, $"{padding}Property '{propertyValue.name.ValueOrNull()}' modification failed: {ex.Message}");

                if (stringBuilder != null)
                    stringBuilder.AppendLine($"{padding}[Error] Property '{propertyValue.name.ValueOrNull()}' modification failed: {ex.Message}");

                return false;
            }
        }

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