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
        /// Navigates to a specific field, array element, or dictionary entry by path and
        /// serializes only that target — the read-side counterpart of TryModifyAt.
        ///
        /// Path format:
        ///   - Plain segment: field or property name (e.g. "admin" or "admin/name")
        ///   - [i] where obj is Array/IList: array index (e.g. "users/[2]/name")
        ///   - [key] where obj is IDictionary: dictionary key (e.g. "config/[timeout]")
        ///   - Leading "#/" stripped automatically (compatible with SerializationContext paths)
        ///
        /// Errors are accumulated in the optional Logs object; nothing is thrown.
        /// </summary>
        public bool TryReadAt(
            object? obj,
            string path,
            out SerializedMember? result,
            Type? fallbackObjType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var segments   = ParsePath(path);
            var target     = obj;
            var targetType = obj?.GetType() ?? fallbackObjType;

            for (int i = 0; i < segments.Length; i++)
            {
                if (!TryNavigateOneSegment(ref target, ref targetType, segments[i], depth + i, logs, flags, logger))
                {
                    result = null;
                    return false;
                }
            }

            result = Serialize(target, targetType, depth: depth + segments.Length, logs: logs, flags: flags, logger: logger);
            return true;
        }

        // ─── Shared path-navigation helper (used by TryReadAt and View) ───────────────

        /// <summary>
        /// Navigates one path segment in-place, updating <paramref name="obj"/> and
        /// <paramref name="objType"/> to the value at that segment.
        /// Handles bracket notation ([i] for arrays/lists, [key] for dicts) and plain member names.
        /// </summary>
        internal bool TryNavigateOneSegment(
            ref object? obj,
            ref Type? objType,
            string segment,
            int depth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            var padding = StringUtils.GetPadding(depth);

            if (obj == null)
            {
                var msg = $"Cannot navigate segment '{segment}': object is null.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{msg}");
                return false;
            }

            var resolvedType = obj.GetType();

            if (TryParseBracketSegment(segment, out var innerKey))
            {
                // ── Array / IList ──────────────────────────────────────────────────────
                if (obj is IList list)
                {
                    if (!int.TryParse(innerKey, out int idx))
                    {
                        var msg = $"Bracket segment '{segment}' cannot be used as index on type '{resolvedType.GetTypeShortName()}'. Expected integer index.";
                        logs?.Error(msg, depth);
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding}{msg}");
                        return false;
                    }

                    if (idx < 0 || idx >= list.Count)
                    {
                        var msg = $"Bracket segment '{segment}' index out of range on type '{resolvedType.GetTypeShortName()}'. Array/list length is {list.Count}.";
                        logs?.Error(msg, depth);
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding}{msg}");
                        return false;
                    }

                    obj     = list[idx];
                    objType = TypeUtils.GetEnumerableItemType(resolvedType);
                    return true;
                }

                // ── Dictionary ────────────────────────────────────────────────────────
                if (TypeUtils.IsDictionary(resolvedType))
                {
                    var args    = TypeUtils.GetDictionaryGenericArguments(resolvedType);
                    var keyType = args?[0] ?? typeof(object);
                    var valType = args?[1] ?? typeof(object);

                    object dictKey;
                    try
                    {
                        dictKey = Convert.ChangeType(innerKey, keyType);
                    }
                    catch (Exception ex)
                    {
                        var msg = $"Bracket segment '{segment}' cannot be converted to key type '{keyType.GetTypeShortName()}' on type '{resolvedType.GetTypeShortName()}': {ex.Message}";
                        logs?.Error(msg, depth);
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding}{msg}");
                        return false;
                    }

                    var dict = (IDictionary)obj;
                    if (!dict.Contains(dictKey))
                    {
                        var msg = $"Bracket segment '{segment}' key not found in dictionary of type '{resolvedType.GetTypeShortName()}'.";
                        logs?.Error(msg, depth);
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{padding}{msg}");
                        return false;
                    }

                    obj     = dict[dictKey];
                    objType = valType;
                    return true;
                }

                // ── Neither array/list nor dictionary ─────────────────────────────────
                {
                    var msg = $"Bracket segment '{segment}' cannot be used on type '{resolvedType.GetTypeShortName()}'. Type is not an array, list, or dictionary.";
                    logs?.Error(msg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{msg}");
                    return false;
                }
            }

            // ── Plain segment — try field, then property ───────────────────────────────
            var fieldInfo = TypeMemberUtils.GetField(resolvedType, flags, segment);
            if (fieldInfo != null)
            {
                obj     = fieldInfo.GetValue(obj);
                objType = fieldInfo.FieldType;
                return true;
            }

            var propInfo = TypeMemberUtils.GetProperty(resolvedType, flags, segment);
            if (propInfo != null)
            {
                obj     = propInfo.GetValue(obj);
                objType = propInfo.PropertyType;
                return true;
            }

            // ── Not found — detailed error with available members ──────────────────────
            var fieldNames = resolvedType.GetFields(flags).Select(f => f.Name).ToList();
            var propNames  = resolvedType.GetProperties(flags).Select(p => p.Name).ToList();
            var fieldsStr  = fieldNames.Count > 0 ? string.Join(", ", fieldNames) : "none";
            var propsStr   = propNames.Count  > 0 ? string.Join(", ", propNames)  : "none";

            var errorMsg = $"Segment '{segment}' not found on type '{resolvedType.GetTypeShortName()}'."
                         + $"\nAvailable fields: {fieldsStr}"
                         + $"\nAvailable properties: {propsStr}";

            logs?.Error(errorMsg, depth);
            if (logger?.IsEnabled(LogLevel.Error) == true)
                logger.LogError($"{padding}{errorMsg}");

            return false;
        }
    }
}
