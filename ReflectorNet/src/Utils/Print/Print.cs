/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class Print
    {
        public static void FailedToSetNewValue(ref object? obj, Type type, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (stringBuilder == null)
                return;

            var padding = StringUtils.GetPadding(depth);
            stringBuilder.AppendLine($"{padding}[Error] Failed to set new value for '{type.GetTypeName(pretty: false)}'.");
        }
        public static void SetNewValue<T>(ref object? obj, ref T? newValue, Type type, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (stringBuilder == null)
                return;

            var padding = StringUtils.GetPadding(depth);
            var paddingNext = StringUtils.GetPadding(depth + 1);
            var originalType = obj?.GetType() ?? type;
            var newType = newValue?.GetType() ?? type;

            stringBuilder.AppendLine($@"{padding}[Success] Set value
{paddingNext}was: type='{originalType.GetTypeName(pretty: false).ValueOrNull()}', value='{obj}'
{paddingNext}new: type='{newType.GetTypeName(pretty: false).ValueOrNull()}', value='{newValue}'.");
        }
        public static void SetNewValueEnumerable(ref object? obj, ref IEnumerable? newValue, Type type, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (stringBuilder == null)
                return;

            var padding = StringUtils.GetPadding(depth);
            var paddingNext = StringUtils.GetPadding(depth + 1);
            var originalType = obj?.GetType() ?? type;
            var newType = newValue?.GetType() ?? type;

            stringBuilder.AppendLine($@"{padding}[Success] Set array value
{paddingNext}was: type='{originalType.GetTypeName(pretty: false).ValueOrNull()}', value='{obj}'
{paddingNext}new: type='{newType.GetTypeName(pretty: false).ValueOrNull()}', value='{newValue}'.");
        }
        public static void SetNewValueEnumerable<T>(ref object? obj, ref IEnumerable<T>? newValue, Type type, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (stringBuilder == null)
                return;

            var padding = StringUtils.GetPadding(depth);
            var paddingNext = StringUtils.GetPadding(depth + 1);
            var originalType = obj?.GetType() ?? type;
            var newType = newValue?.GetType() ?? type;

            stringBuilder.AppendLine($@"{padding}[Success] Set array value
{paddingNext}was: type='{originalType.GetTypeName(pretty: false).ValueOrNull()}', value='{obj}'
{paddingNext}new: type='{newType.GetTypeName(pretty: false).ValueOrNull()}', value='{newValue}'.");
        }
        public static void FailedToSetField(ref object? obj, Type type, FieldInfo fieldInfo, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (stringBuilder == null)
                return;

            var padding = StringUtils.GetPadding(depth);
            stringBuilder.AppendLine($"{padding}[Error] Failed to set field '{fieldInfo.Name}'");
            stringBuilder.AppendLine($"{padding}[Error] Failed to set new value for '{type.GetTypeName(pretty: false)}'.");
        }
        public static void FailedToSetProperty(ref object? obj, Type type, PropertyInfo propertyInfo, int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
        {
            if (stringBuilder == null)
                return;

            var padding = StringUtils.GetPadding(depth);
            stringBuilder.AppendLine($"{padding}[Error] Failed to set property '{propertyInfo.Name}'");
            stringBuilder.AppendLine($"{padding}[Error] Failed to set new value for '{type.GetTypeName(pretty: false)}'.");
        }
    }
}