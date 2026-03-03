/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections;
using System.Collections.Generic;
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
        /// Navigates to a specific field, array element, or dictionary entry by path and modifies only that target.
        /// This is a truly atomic modification — no other parts of the object graph are touched.
        ///
        /// Path format:
        ///   - Plain segment: field or property name (e.g. "admin" or "admin/name")
        ///   - [i] where obj is Array/IList: array index (e.g. "users/[2]/name")
        ///   - [key] where obj is IDictionary: dictionary key (e.g. "config/[timeout]")
        ///   - Leading "#/" stripped automatically (compatible with SerializationContext paths)
        ///
        /// Errors are accumulated in the optional Logs object; nothing is thrown.
        /// </summary>
        public bool TryModifyAt(
            ref object? obj,
            string path,
            SerializedMember value,
            Type? fallbackObjType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var segments = ParsePath(path);

            // No segments left — this is the terminal: apply the modification
            if (segments.Length == 0)
                return TryModify(ref obj, value, fallbackObjType, depth, logs, flags, logger);

            if (obj == null)
            {
                var msg = $"Cannot navigate path '{path}': object is null.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{StringUtils.GetPadding(depth)}{msg}");
                return false;
            }

            var segment = segments[0];
            var remainingPath = segments.Length > 1
                ? string.Join("/", segments, 1, segments.Length - 1)
                : string.Empty;

            var objType = obj.GetType();

            if (TryParseBracketSegment(segment, out var innerKey))
                return TryModifyAtBracketed(ref obj, segment, innerKey, remainingPath, value, objType, depth, logs, flags, logger);

            return TryModifyAtMember(ref obj, segment, remainingPath, value, objType, depth, logs, flags, logger);
        }

        /// <summary>
        /// Convenience generic overload — ideal for modifying leaf/primitive values by path.
        /// Internally creates a SerializedMember.FromValue&lt;T&gt; and calls the primary overload.
        /// </summary>
        public bool TryModifyAt<T>(
            ref object? obj,
            string path,
            T value,
            Type? fallbackObjType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var serializedValue = SerializedMember.FromValue<T>(this, value);
            return TryModifyAt(ref obj, path, serializedValue, fallbackObjType, depth, logs, flags, logger);
        }

        // ─── Path parsing ────────────────────────────────────────────────────────────

        private static string[] ParsePath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return Array.Empty<string>();

            // Strip leading "#/" or "#" (SerializationContext convention)
            if (path.StartsWith("#/"))
                path = path.Substring(2);
            else if (path.StartsWith("#"))
                path = path.Substring(1);

            return path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        internal static bool TryParseBracketSegment(string segment, out string innerKey)
        {
            if (segment.Length > 2 && segment[0] == '[' && segment[segment.Length - 1] == ']')
            {
                innerKey = segment.Substring(1, segment.Length - 2);
                return true;
            }
            innerKey = string.Empty;
            return false;
        }

        // ─── Type-replacement check ───────────────────────────────────────────────────

        /// <summary>
        /// At the terminal navigation step, if the SerializedMember requests a different (but compatible subtype)
        /// than the current value's type, resets currentValue to null so TryModify will create a fresh instance.
        /// </summary>
        private void ApplyTypeReplacementCheck(ref object? currentValue, SerializedMember value, Type declaredType, string remainingPath)
        {
            if (!string.IsNullOrEmpty(remainingPath))
                return;

            var desiredType = TypeUtils.GetTypeWithNamePriority(value, declaredType, out _);
            if (desiredType != null
                && currentValue != null
                && desiredType != currentValue.GetType()
                && declaredType.IsAssignableFrom(desiredType))
            {
                currentValue = null; // force fresh instance creation in TryModify's null branch
            }
        }

        // ─── Bracket navigation (array index / dict key) ─────────────────────────────

        private bool TryModifyAtBracketed(
            ref object? obj,
            string segment,
            string innerKey,
            string remainingPath,
            SerializedMember value,
            Type objType,
            int depth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            var padding = StringUtils.GetPadding(depth);

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
                return TryModifyAtArrayIndex(ref obj, segment, idx, remainingPath, value, objType, depth, logs, flags, logger);
            }

            if (TypeUtils.IsDictionary(objType))
            {
                var args = TypeUtils.GetDictionaryGenericArguments(objType);
                var keyType = args?[0] ?? typeof(object);
                var valType = args?[1] ?? typeof(object);

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
                return TryModifyAtDictKey(ref obj, segment, dictKey, remainingPath, value, valType, depth, logs, flags, logger);
            }

            {
                var msg = $"Bracket segment '{segment}' cannot be used on type '{objType.GetTypeShortName()}'. Type is not an array, list, or dictionary.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{msg}");
                return false;
            }
        }

        private bool TryModifyAtArrayIndex(
            ref object? obj,
            string segment,
            int idx,
            string remainingPath,
            SerializedMember value,
            Type objType,
            int depth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            var elementType = TypeUtils.GetEnumerableItemType(objType);
            var padding = StringUtils.GetPadding(depth);

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

                var currentElement = array.GetValue(idx);
                ApplyTypeReplacementCheck(ref currentElement, value, elementType ?? typeof(object), remainingPath);

                var success = TryModifyAt(ref currentElement, remainingPath, value, elementType, depth + 1, logs, flags, logger);
                if (success)
                    array.SetValue(currentElement, idx);
                return success;
            }

            if (obj is IList list)
            {
                if (idx < 0 || idx >= list.Count)
                {
                    var msg = $"Bracket segment '{segment}' index out of range on type '{objType.GetTypeShortName()}'. List count is {list.Count}.";
                    logs?.Error(msg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{msg}");
                    return false;
                }

                var currentElement = list[idx];
                ApplyTypeReplacementCheck(ref currentElement, value, elementType ?? typeof(object), remainingPath);

                var success = TryModifyAt(ref currentElement, remainingPath, value, elementType, depth + 1, logs, flags, logger);
                if (success)
                    list[idx] = currentElement;
                return success;
            }

            {
                var msg = $"Bracket segment '{segment}' cannot be applied: type '{objType.GetTypeShortName()}' is not an array or list.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{msg}");
                return false;
            }
        }

        private bool TryModifyAtDictKey(
            ref object? obj,
            string segment,
            object dictKey,
            string remainingPath,
            SerializedMember value,
            Type valueType,
            int depth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            var dict = (IDictionary)obj!;
            var currentElement = dict.Contains(dictKey) ? dict[dictKey] : null;

            ApplyTypeReplacementCheck(ref currentElement, value, valueType, remainingPath);

            var success = TryModifyAt(ref currentElement, remainingPath, value, valueType, depth + 1, logs, flags, logger);
            if (success)
                dict[dictKey] = currentElement;
            return success;
        }

        // ─── Member navigation (field / property) ────────────────────────────────────

        private bool TryModifyAtMember(
            ref object? obj,
            string memberName,
            string remainingPath,
            SerializedMember value,
            Type objType,
            int depth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            var padding = StringUtils.GetPadding(depth);

            // Try field
            var fieldInfo = TypeMemberUtils.GetField(objType, flags, memberName);
            if (fieldInfo != null)
            {
                var currentValue = fieldInfo.GetValue(obj);
                ApplyTypeReplacementCheck(ref currentValue, value, fieldInfo.FieldType, remainingPath);

                var success = TryModifyAt(ref currentValue, remainingPath, value, fieldInfo.FieldType, depth + 1, logs, flags, logger);
                if (success)
                    fieldInfo.SetValue(obj, currentValue); // struct-safe write-back
                return success;
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
                    return false;
                }

                var currentValue = propInfo.GetValue(obj);
                ApplyTypeReplacementCheck(ref currentValue, value, propInfo.PropertyType, remainingPath);

                var success = TryModifyAt(ref currentValue, remainingPath, value, propInfo.PropertyType, depth + 1, logs, flags, logger);
                if (success)
                    propInfo.SetValue(obj, currentValue);
                return success;
            }

            // Neither field nor property found — detailed error with available members
            var fieldNames = objType.GetFields(flags).Select(f => f.Name).ToList();
            var propNames  = objType.GetProperties(flags).Select(p => p.Name).ToList();
            var fieldsStr  = fieldNames.Count > 0 ? string.Join(", ", fieldNames) : "none";
            var propsStr   = propNames.Count  > 0 ? string.Join(", ", propNames)  : "none";

            var msg = $"Segment '{memberName}' not found on type '{objType.GetTypeShortName()}'."
                    + $"\nAvailable fields: {fieldsStr}"
                    + $"\nAvailable properties: {propsStr}";

            logs?.Error(msg, depth);
            if (logger?.IsEnabled(LogLevel.Error) == true)
                logger.LogError($"{padding}{msg}");

            return false;
        }
    }
}
