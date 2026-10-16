/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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

            var segments = new List<string>();
            var start = 0;
            var bracketed = false;
            for (int i = 0; i < path.Length; i++)
            {
                var c = path[i];
                if (bracketed && c == '\\' && i + 1 < path.Length
                    && (path[i + 1] == ']' || path[i + 1] == '\\'))
                {
                    i++; // Keep escapes intact until the key is decoded.
                    continue;
                }
                if (i == start && c == '[') bracketed = true;
                else if (bracketed && c == ']') bracketed = false;
                else if (!bracketed && c == '/')
                {
                    if (i > start) segments.Add(path.Substring(start, i - start));
                    start = i + 1;
                }
            }
            if (start < path.Length) segments.Add(path.Substring(start));
            return segments.ToArray();
        }

        internal static bool TryParseBracketSegment(string segment, out string innerKey, bool unescape = true)
        {
            if (segment.Length >= 2 && segment[0] == '[' && segment[segment.Length - 1] == ']')
            {
                if (!unescape)
                {
                    innerKey = segment.Substring(1, segment.Length - 2);
                    return true;
                }
                var key = new StringBuilder();
                for (int i = 1; i < segment.Length - 1; i++)
                {
                    if (segment[i] == '\\' && i + 1 < segment.Length - 1
                        && (segment[i + 1] == ']' || segment[i + 1] == '\\')) i++;
                    key.Append(segment[i]);
                }
                innerKey = key.ToString();
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

        // ─── Write-back ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Stores a modified value back into its container (member, array/list element or dictionary entry).
        /// A setter that throws is reported to <paramref name="logs"/> and fails the call, so a dropped write
        /// is never reported as a success. <paramref name="describeTarget"/> is only evaluated on failure.
        /// </summary>
        internal static bool TryWriteBack(
            Action<object?> writeBack,
            object? value,
            Func<string> describeTarget,
            int depth,
            Logs? logs,
            ILogger? logger)
        {
            try
            {
                writeBack(value);
                return true;
            }
            catch (Exception ex)
            {
                var reason = (ex as TargetInvocationException)?.InnerException?.Message ?? ex.Message;
                var msg = $"Cannot write {describeTarget()}: {reason}";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{StringUtils.GetPadding(depth)}{msg}");
                return false;
            }
        }

        /// <summary>
        /// True when a nested modification mutated a reference-type value in place: it is already stored in its
        /// container, so writing it back is redundant (and a throwing container setter must not turn an applied
        /// change into a reported failure). Boxed value types are copies and always need the write-back.
        /// </summary>
        private static bool IsSameInstance(object? original, object? modified)
            => modified != null && !modified.GetType().IsValueType && ReferenceEquals(original, modified);

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
            var original = currentElement;
            return TryModifyAt(ref currentElement, remainingPath, value, elementType, depth + 1, logs, flags, logger)
                && (IsSameInstance(original, currentElement) || TryWriteBack(writeBack!, currentElement, () => $"element '{segment}' of type '{objType.GetTypeShortName()}'", depth, logs, logger));
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
            var original = currentValue;
            return TryModifyAt(ref currentValue, remainingPath, value, memberType, depth + 1, logs, flags, logger)
                && (IsSameInstance(original, currentValue) || TryWriteBack(writeBack!, currentValue,
                    () => $"member '{memberName}' of type '{objType.GetTypeShortName()}'", depth, logs, logger));
        }
    }
}
