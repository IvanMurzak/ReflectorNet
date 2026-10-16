/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// Stores the modified value back into its container (field, property, array/list element or
        /// dictionary entry). The value was produced by the converter named by the patch's <c>typeName</c>,
        /// so it may be a different primitive than the declared type (e.g. a <c>double</c> for a <c>float</c>
        /// member): such a value is converted when the conversion is lossless. Any value that still cannot be
        /// stored — or a setter that throws — is reported to <paramref name="logs"/> and fails the call
        /// instead of being reported as a success.
        /// </summary>
        private static bool TryWriteBack(
            Action<object?> writeBack,
            object? value,
            Type declaredType,
            string target,
            int depth,
            Logs? logs,
            ILogger? logger)
        {
            string? error;
            if (TryConvertForAssignment(value, declaredType, out var converted, out error))
            {
                try
                {
                    writeBack(converted);
                    return true;
                }
                catch (Exception ex)
                {
                    error = $"setter threw: {(ex as TargetInvocationException)?.InnerException?.Message ?? ex.Message}";
                }
            }

            var msg = $"Cannot write {target}: {error}";
            logs?.Error(msg, depth);
            if (logger?.IsEnabled(LogLevel.Error) == true)
                logger.LogError($"{StringUtils.GetPadding(depth)}{msg}");
            return false;
        }

        /// <summary>
        /// Returns <paramref name="value"/> as-is when <paramref name="declaredType"/> accepts it, or converts a
        /// numeric value to a numeric/enum declared type when no information is lost (no overflow, no dropped
        /// fraction). Anything else is rejected with a reason.
        /// </summary>
        private static bool TryConvertForAssignment(object? value, Type declaredType, out object? converted, out string? error)
        {
            converted = value;
            error = null;
            var target = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

            if (value == null)
            {
                if (!declaredType.IsValueType || target != declaredType)
                    return true;
                error = $"null is not a valid '{declaredType.GetTypeShortName()}'";
                return false;
            }
            if (declaredType.IsInstanceOfType(value))
                return true;

            if (IsNumeric(value.GetType()) && (IsNumeric(target) || target.IsEnum))
            {
                var numericTarget = target.IsEnum ? Enum.GetUnderlyingType(target) : target;
                try
                {
                    var number = Convert.ChangeType(value, numericTarget, CultureInfo.InvariantCulture)!;
                    // An integer must keep its exact value (3.7 never silently becomes 4); a float only must not
                    // overflow to infinity (0.33 as float is the intended rounding).
                    var lossless = IsIntegral(numericTarget)
                        ? Convert.ToDecimal(value, CultureInfo.InvariantCulture) == Convert.ToDecimal(number, CultureInfo.InvariantCulture)
                        : !double.IsInfinity(Convert.ToDouble(number, CultureInfo.InvariantCulture))
                            || double.IsInfinity(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    if (lossless)
                    {
                        converted = target.IsEnum ? Enum.ToObject(target, number) : number;
                        return true;
                    }
                }
                catch (Exception ex) when (ex is OverflowException || ex is InvalidCastException)
                {
                }
                error = $"the {value.GetType().GetTypeShortName()} value '{value}' does not fit '{declaredType.GetTypeShortName()}'";
                return false;
            }

            error = $"a '{value.GetType().GetTypeShortName()}' value is not assignable to '{declaredType.GetTypeShortName()}'";
            return false;
        }

        private static bool IsNumeric(Type type)
            => IsIntegral(type) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);

        private static bool IsIntegral(Type type)
            => type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort)
            || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong);

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
            return TryModifyAt(ref currentElement, remainingPath, value, elementType, depth + 1, logs, flags, logger)
                && TryWriteBack(writeBack!, currentElement, elementType, $"element '{segment}' of type '{objType.GetTypeShortName()}'", depth, logs, logger);
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
            return TryModifyAt(ref currentValue, remainingPath, value, memberType, depth + 1, logs, flags, logger)
                && TryWriteBack(writeBack!, currentValue, memberType, $"member '{memberName}' of type '{objType.GetTypeShortName()}'", depth, logs, logger);
        }
    }
}
