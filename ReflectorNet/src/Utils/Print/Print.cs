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
using com.IvanMurzak.ReflectorNet.Model;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static class Print
    {
        public static void FailedToSetNewValue(ref object? obj, Type type, int depth = 0, Logs? logs = null, ILogger? logger = null)
        {
            logs?.Error($"Failed to set new value for '{type.GetTypeId()}'.", depth);
        }
        public static void SetNewValue<T>(ref object? obj, ref T? newValue, Type type, int depth = 0, Logs? logs = null, ILogger? logger = null)
        {
            var originalType = obj?.GetType() ?? type;
            var newType = newValue?.GetType() ?? type;

            logs?.Success($@"Set value
  was: type='{originalType.GetTypeId().ValueOrNull()}', value='{obj}'
  new: type='{newType.GetTypeId().ValueOrNull()}', value='{newValue}'.", depth);
        }
        public static void SetNewValueEnumerable(ref object? obj, ref IEnumerable? newValue, Type type, int depth = 0, Logs? logs = null, ILogger? logger = null)
        {
            var originalType = obj?.GetType() ?? type;
            var newType = newValue?.GetType() ?? type;

            logs?.Success($@"Set array value
  was: type='{originalType.GetTypeId().ValueOrNull()}', value='{obj}'
  new: type='{newType.GetTypeId().ValueOrNull()}', value='{newValue}'.", depth);
        }
        public static void SetNewValueEnumerable<T>(ref object? obj, ref IEnumerable<T>? newValue, Type type, int depth = 0, Logs? logs = null, ILogger? logger = null)
        {
            var originalType = obj?.GetType() ?? type;
            var newType = newValue?.GetType() ?? type;

            logs?.Success($@"Set array value
  was: type='{originalType.GetTypeId().ValueOrNull()}', value='{obj}'
  new: type='{newType.GetTypeId().ValueOrNull()}', value='{newValue}'.", depth);
        }
        public static void FailedToSetField(ref object? obj, Type type, FieldInfo fieldInfo, int depth = 0, Logs? logs = null, ILogger? logger = null)
        {
            logs?.Error($"Failed to set field '{fieldInfo.Name}'", depth);
            logs?.Error($"Failed to set new value for '{type.GetTypeId()}'.", depth);
        }
        public static void FailedToSetProperty(ref object? obj, Type type, PropertyInfo propertyInfo, int depth = 0, Logs? logs = null, ILogger? logger = null)
        {
            logs?.Error($"Failed to set property '{propertyInfo.Name}'", depth);
            logs?.Error($"Failed to set new value for '{type.GetTypeId()}'.", depth);
        }
    }
}