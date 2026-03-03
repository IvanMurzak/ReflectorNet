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
using System.Text.Json;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        /// <summary>
        /// Applies a JSON Merge Patch document to an object, modifying multiple fields at different depths in
        /// a single call. Follows RFC 7396 JSON Merge Patch semantics, extended with bracket-notation keys
        /// for array/dictionary access.
        ///
        /// Patch document rules:
        ///   - JSON object key   → navigate into that field/property (plain) or array/dict element (bracket)
        ///   - JSON non-object   → set as the value at current node
        ///   - JSON null         → set the field to null
        ///   - "$type" key       → optional type hint: replace current instance with new type (must be a subtype
        ///                         of the declared type). Other keys in the same object are then applied to the
        ///                         fresh instance.
        ///
        /// Errors are accumulated in the optional Logs object; nothing is thrown.
        /// </summary>
        public bool TryPatch(
            ref object? obj,
            string json,
            Type? fallbackObjType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to parse JSON patch: {ex.Message}";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{StringUtils.GetPadding(depth)}{msg}");
                return false;
            }

            using (doc)
            {
                return TryPatch(ref obj, doc.RootElement, fallbackObjType, depth, logs, flags, logger);
            }
        }

        /// <summary>
        /// Applies a JSON Merge Patch document to an object. See the string overload for full documentation.
        /// </summary>
        public bool TryPatch(
            ref object? obj,
            JsonElement patch,
            Type? fallbackObjType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var objType = obj?.GetType() ?? fallbackObjType;
            return TryPatchInternal(ref obj, patch, objType, depth, logs, flags, logger);
        }

        // ─── Internal recursive patch engine ─────────────────────────────────────────

        private bool TryPatchInternal(
            ref object? obj,
            JsonElement patch,
            Type? objType,
            int depth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            var padding = StringUtils.GetPadding(depth);

            // null patch → set the current node to null
            if (patch.ValueKind == JsonValueKind.Null)
            {
                obj = null;
                return true;
            }

            // Leaf value (non-object JSON) → set directly via existing TryModify
            if (patch.ValueKind != JsonValueKind.Object)
            {
                var member = SerializedMember.FromJson(objType ?? obj?.GetType() ?? typeof(object), patch);
                return TryModify(ref obj, member, objType, depth, logs, flags, logger);
            }

            // JSON object → process optional "$type" hint first, then navigate keys

            // Extract "$type" if present
            string? typeHint = null;
            if (patch.TryGetProperty("$type", out var typeElement))
                typeHint = typeElement.GetString();

            // Apply type replacement if $type specifies a compatible subtype
            if (typeHint != null)
            {
                var desiredType = TypeUtils.GetType(typeHint);
                var declaredType = objType ?? obj?.GetType();
                if (desiredType != null
                    && obj != null
                    && desiredType != obj.GetType()
                    && (declaredType == null || declaredType.IsAssignableFrom(desiredType)))
                {
                    obj = null;
                    objType = desiredType;
                }
                else if (desiredType != null && objType == null)
                {
                    objType = desiredType;
                }
            }

            // If obj is null but we have a type, create a default instance so we can navigate into it
            if (obj == null && objType != null)
            {
                obj = CreateInstance(objType);
                if (obj == null)
                {
                    var msg = $"Cannot create instance of type '{objType.GetTypeShortName()}' for patch application.";
                    logs?.Error(msg, depth);
                    if (logger?.IsEnabled(LogLevel.Error) == true)
                        logger.LogError($"{padding}{msg}");
                    return false;
                }
            }

            if (obj == null)
            {
                var msg = $"Cannot apply JSON patch: target object is null and no type is known.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{msg}");
                return false;
            }

            objType = obj.GetType();
            var overallSuccess = true;

            foreach (var property in patch.EnumerateObject())
            {
                if (property.Name == "$type")
                    continue; // already consumed above

                bool success;
                if (TryParseBracketSegment(property.Name, out var innerKey))
                    success = TryPatchBracketed(ref obj, property.Name, innerKey, property.Value, objType, depth, logs, flags, logger);
                else
                    success = TryPatchMember(ref obj, property.Name, property.Value, objType, depth, logs, flags, logger);

                overallSuccess &= success;
            }

            return overallSuccess;
        }

        // ─── Patch member navigation ──────────────────────────────────────────────────

        private bool TryPatchMember(
            ref object? obj,
            string memberName,
            JsonElement patchValue,
            Type? objType,
            int depth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            if (objType == null || obj == null)
            {
                var msg = $"Cannot navigate to member '{memberName}': object is null.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{StringUtils.GetPadding(depth)}{msg}");
                return false;
            }

            var padding = StringUtils.GetPadding(depth);

            // Try field
            var fieldInfo = TypeMemberUtils.GetField(objType, flags, memberName);
            if (fieldInfo != null)
            {
                var currentValue = fieldInfo.GetValue(obj);
                ApplyPatchTypeReplacement(ref currentValue, patchValue, fieldInfo.FieldType);

                var childType = currentValue?.GetType() ?? fieldInfo.FieldType;
                var success = TryPatchInternal(ref currentValue, patchValue, childType, depth + 1, logs, flags, logger);
                if (success)
                    fieldInfo.SetValue(obj, currentValue);
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
                ApplyPatchTypeReplacement(ref currentValue, patchValue, propInfo.PropertyType);

                var childType = currentValue?.GetType() ?? propInfo.PropertyType;
                var success = TryPatchInternal(ref currentValue, patchValue, childType, depth + 1, logs, flags, logger);
                if (success)
                    propInfo.SetValue(obj, currentValue);
                return success;
            }

            // Neither found — detailed error
            var fieldNames = objType.GetFields(flags).Select(f => f.Name).ToList();
            var propNames  = objType.GetProperties(flags).Select(p => p.Name).ToList();
            var fieldsStr  = fieldNames.Count > 0 ? string.Join(", ", fieldNames) : "none";
            var propsStr   = propNames.Count  > 0 ? string.Join(", ", propNames)  : "none";

            var notFoundMsg = $"Segment '{memberName}' not found on type '{objType.GetTypeShortName()}'."
                            + $"\nAvailable fields: {fieldsStr}"
                            + $"\nAvailable properties: {propsStr}";

            logs?.Error(notFoundMsg, depth);
            if (logger?.IsEnabled(LogLevel.Error) == true)
                logger.LogError($"{padding}{notFoundMsg}");

            return false;
        }

        private bool TryPatchBracketed(
            ref object? obj,
            string segment,
            string innerKey,
            JsonElement patchValue,
            Type? objType,
            int depth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            if (obj == null || objType == null)
            {
                var msg = $"Cannot navigate bracket segment '{segment}': object is null.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{StringUtils.GetPadding(depth)}{msg}");
                return false;
            }

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
                return TryPatchArrayIndex(ref obj, segment, idx, patchValue, objType, depth, logs, flags, logger);
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
                return TryPatchDictKey(ref obj, segment, dictKey, patchValue, valType, depth, logs, flags, logger);
            }

            {
                var msg = $"Bracket segment '{segment}' cannot be used on type '{objType.GetTypeShortName()}'. Type is not an array, list, or dictionary.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{padding}{msg}");
                return false;
            }
        }

        private bool TryPatchArrayIndex(
            ref object? obj,
            string segment,
            int idx,
            JsonElement patchValue,
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
                ApplyPatchTypeReplacement(ref currentElement, patchValue, elementType ?? typeof(object));

                var childType = currentElement?.GetType() ?? elementType;
                var success = TryPatchInternal(ref currentElement, patchValue, childType, depth + 1, logs, flags, logger);
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
                ApplyPatchTypeReplacement(ref currentElement, patchValue, elementType ?? typeof(object));

                var childType = currentElement?.GetType() ?? elementType;
                var success = TryPatchInternal(ref currentElement, patchValue, childType, depth + 1, logs, flags, logger);
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

        private bool TryPatchDictKey(
            ref object? obj,
            string segment,
            object dictKey,
            JsonElement patchValue,
            Type valueType,
            int depth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            var dict = (IDictionary)obj!;
            var currentElement = dict.Contains(dictKey) ? dict[dictKey] : null;

            ApplyPatchTypeReplacement(ref currentElement, patchValue, valueType);

            var childType = currentElement?.GetType() ?? valueType;
            var success = TryPatchInternal(ref currentElement, patchValue, childType, depth + 1, logs, flags, logger);
            if (success)
                dict[dictKey] = currentElement;
            return success;
        }

        // ─── Type replacement for patch (uses "$type" from JsonElement) ──────────────

        /// <summary>
        /// Checks if the JSON patch value contains a "$type" key specifying a compatible subtype.
        /// If so, resets currentValue to null so TryPatchInternal will create a fresh instance.
        /// </summary>
        private static void ApplyPatchTypeReplacement(ref object? currentValue, JsonElement patchValue, Type declaredType)
        {
            if (patchValue.ValueKind != JsonValueKind.Object)
                return;

            if (!patchValue.TryGetProperty("$type", out var typeElement))
                return;

            var typeName = typeElement.GetString();
            if (string.IsNullOrEmpty(typeName))
                return;

            var desiredType = TypeUtils.GetType(typeName);
            if (desiredType != null
                && currentValue != null
                && desiredType != currentValue.GetType()
                && declaredType.IsAssignableFrom(desiredType))
            {
                currentValue = null; // force fresh instance creation
            }
        }
    }
}
