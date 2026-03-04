/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
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
            if (!TryLookupBracketedElement(obj, segment, innerKey, objType, depth, logs, logger,
                    out var currentElement, out var elementType, out var writeBack))
                return false;

            ApplyTypeReplacementCheck(ref currentElement, value, elementType, remainingPath);
            var success = TryModifyAt(ref currentElement, remainingPath, value, elementType, depth + 1, logs, flags, logger);
            if (success)
                writeBack!(currentElement);
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
            if (!TryLookupMember(obj, memberName, objType, flags, depth, logs, logger,
                    out var currentValue, out var memberType, out var writeBack))
                return false;

            ApplyTypeReplacementCheck(ref currentValue, value, memberType, remainingPath);
            var success = TryModifyAt(ref currentValue, remainingPath, value, memberType, depth + 1, logs, flags, logger);
            if (success)
                writeBack!(currentValue);
            return success;
        }
    }
}
