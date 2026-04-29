/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        /// <summary>
        /// Resolves a named field or property on <paramref name="obj"/>, returning its current value,
        /// declared type, and a write-back action for struct-safe mutation. The read-only check for
        /// properties is enforced here. Returns false and logs a detailed error if the member is not
        /// found or is read-only.
        /// </summary>
        private bool TryLookupMember(
            object? obj,
            string memberName,
            Type objType,
            BindingFlags flags,
            int depth,
            Logs? logs,
            ILogger? logger,
            out object? currentValue,
            out Type memberType,
            out Action<object?>? writeBack)
        {
            var padding = StringUtils.GetPadding(depth);

            // Try field
            var fieldInfo = TypeMemberUtils.GetField(objType, flags, memberName);
            if (fieldInfo != null)
            {
                try
                {
                    currentValue = fieldInfo.GetValue(obj);
                }
                catch (Exception ex)
                {
                    var getMsg = $"Field '{memberName}' on type '{objType.GetTypeShortName()}' getter threw: {ex.Message}";
                    logs?.Error(getMsg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{getMsg}");
                    currentValue = null;
                    memberType   = fieldInfo.FieldType;
                    writeBack    = null;
                    return false;
                }
                memberType = fieldInfo.FieldType;
                writeBack  = v =>
                {
                    try { fieldInfo.SetValue(obj, v); }
                    catch (Exception ex)
                    {
                        logger?.LogError($"{padding}Field '{memberName}' setter threw: {ex.Message}");
                    }
                };
                return true;
            }

            // Try property
            var propInfo = TypeMemberUtils.GetProperty(objType, flags, memberName);
            if (propInfo != null)
            {
                if (!propInfo.CanWrite)
                {
                    var readOnlyMsg = $"Property '{memberName}' on type '{objType.GetTypeShortName()}' is read-only.";
                    logs?.Error(readOnlyMsg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{readOnlyMsg}");
                    currentValue = null;
                    memberType   = propInfo.PropertyType;
                    writeBack    = null;
                    return false;
                }

                if (!propInfo.CanRead)
                {
                    var writeOnlyMsg = $"Property '{memberName}' on type '{objType.GetTypeShortName()}' is write-only (no getter); cannot read current value.";
                    logs?.Error(writeOnlyMsg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{writeOnlyMsg}");
                    currentValue = null;
                    memberType   = propInfo.PropertyType;
                    writeBack    = null;
                    return false;
                }

                try
                {
                    currentValue = propInfo.GetValue(obj);
                }
                catch (Exception ex)
                {
                    var getMsg = $"Property '{memberName}' on type '{objType.GetTypeShortName()}' getter threw: {ex.Message}";
                    logs?.Error(getMsg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{getMsg}");
                    currentValue = null;
                    memberType   = propInfo.PropertyType;
                    writeBack    = null;
                    return false;
                }
                memberType = propInfo.PropertyType;
                writeBack  = v =>
                {
                    try { propInfo.SetValue(obj, v); }
                    catch (Exception ex)
                    {
                        logger?.LogError($"{padding}Property '{memberName}' setter threw: {ex.Message}");
                    }
                };
                return true;
            }

            // Neither found — detailed error with available members (matches the read-side
            // convention in TryNavigateOneSegment, which lists serializable members only).
            var serializableFields = GetSerializableFields(objType, flags, logger);
            var serializableProps  = GetSerializableProperties(objType, flags, logger);
            var fieldsStr = serializableFields != null && serializableFields.Any()
                ? string.Join(", ", serializableFields.Select(f => f.Name))
                : "none";
            var propsStr  = serializableProps  != null && serializableProps.Any()
                ? string.Join(", ", serializableProps .Select(p => p.Name))
                : "none";

            var msg = $"Segment '{memberName}' not found on type '{objType.GetTypeShortName()}'."
                    + $"\nAvailable fields: {fieldsStr}"
                    + $"\nAvailable properties: {propsStr}";

            logs?.Error(msg, depth);
            if (logger?.IsEnabled(LogLevel.Error) == true)
                logger.LogError($"{padding}{msg}");

            currentValue = null;
            memberType   = typeof(object);
            writeBack    = null;
            return false;
        }

        /// <summary>
        /// Resolves a bracket-notation segment (<c>[i]</c> for array/list, <c>[key]</c> for dictionary)
        /// on <paramref name="obj"/>, returning the current element, its declared type, and a write-back
        /// action. Bounds and key-type validation are enforced; detailed errors are logged on failure.
        /// </summary>
        private static bool TryLookupBracketedElement(
            object? obj,
            string segment,
            string innerKey,
            Type objType,
            int depth,
            Logs? logs,
            ILogger? logger,
            out object? currentElement,
            out Type elementType,
            out Action<object?>? writeBack)
        {
            var padding = StringUtils.GetPadding(depth);
            currentElement = null;
            elementType    = typeof(object);
            writeBack      = null;

            if (obj is IList)
            {
                if (!int.TryParse(innerKey, out int idx))
                {
                    var msg = $"Bracket segment '{segment}' cannot be used as index on type '{objType.GetTypeShortName()}'. Expected integer index.";
                    logs?.Error(msg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{msg}");
                    return false;
                }

                elementType = TypeUtils.GetEnumerableItemType(objType) ?? typeof(object);

                if (obj is Array array)
                {
                    if (idx < 0 || idx >= array.Length)
                    {
                        var msg = $"Bracket segment '{segment}' index out of range on type '{objType.GetTypeShortName()}'. Array length is {array.Length}.";
                        logs?.Error(msg, depth);
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding}{msg}");
                        return false;
                    }

                    currentElement = array.GetValue(idx);
                    writeBack      = v => array.SetValue(v, idx);
                    return true;
                }

                var list = (IList)obj;
                if (idx < 0 || idx >= list.Count)
                {
                    var msg = $"Bracket segment '{segment}' index out of range on type '{objType.GetTypeShortName()}'. List count is {list.Count}.";
                    logs?.Error(msg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{msg}");
                    return false;
                }

                currentElement = list[idx];
                writeBack      = v => list[idx] = v;
                return true;
            }

            if (TypeUtils.IsDictionary(objType))
            {
                var args    = TypeUtils.GetDictionaryGenericArguments(objType);
                var keyType = args?[0] ?? typeof(object);
                elementType = args?[1] ?? typeof(object);

                object dictKey;
                try
                {
                    dictKey = Convert.ChangeType(innerKey, keyType);
                }
                catch (Exception ex)
                {
                    var msg = $"Bracket segment '{segment}' cannot be converted to key type '{keyType.GetTypeShortName()}' on type '{objType.GetTypeShortName()}': {ex.Message}";
                    logs?.Error(msg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{msg}");
                    return false;
                }

                var dict   = (IDictionary)obj!;
                currentElement = dict.Contains(dictKey) ? dict[dictKey] : null;
                writeBack      = v => dict[dictKey] = v;
                return true;
            }

            {
                var msg = $"Bracket segment '{segment}' cannot be used on type '{objType.GetTypeShortName()}'. Type is not an array, list, or dictionary.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{msg}");
                return false;
            }
        }
    }
}
