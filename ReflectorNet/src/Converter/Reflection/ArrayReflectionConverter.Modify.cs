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
using Microsoft.Extensions.Logging;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    public partial class ArrayReflectionConverter
    {
        /// <summary>
        /// Overrides TryModify to support partial in-place modification of individual array/list elements
        /// using [i]-indexed field names in data.fields (e.g. name="[2]"). When data.valueJsonElement is
        /// present, falls back to full replacement (existing behaviour). When data.fields contains only
        /// non-indexed names, also falls back to base (unchanged behaviour).
        /// </summary>
        public override bool TryModify(
            Reflector reflector,
            ref object? obj,
            SerializedMember data,
            Type type,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            // Full replacement requested — delegate to base (calls SetValue → TryDeserializeValueListInternal)
            if (data.valueJsonElement.HasValue)
                return base.TryModify(reflector, ref obj, data, type, depth, logs, flags, logger);

            // Partition data.fields into indexed ([i]-named) and non-indexed
            var indexedFields = data.fields?.Where(f => IsArrayIndexName(f?.name)).ToList();

            // No indexed fields — let base handle (unchanged behaviour)
            if (indexedFields == null || indexedFields.Count == 0)
                return base.TryModify(reflector, ref obj, data, type, depth, logs, flags, logger);

            if (obj == null)
            {
                logs?.Error($"Cannot modify array elements: array is null.", depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{StringUtils.GetPadding(depth)}Cannot modify array elements: array is null.");
                return false;
            }

            var elementType = TypeUtils.GetEnumerableItemType(type);
            var overallSuccess = true;

            if (obj is Array array)
            {
                foreach (var indexedField in indexedFields)
                {
                    var idx = ParseArrayIndex(indexedField.name!);
                    if (idx < 0 || idx >= array.Length)
                    {
                        var msg = $"Bracket segment '[{idx}]' index out of range on type '{type.GetTypeShortName()}'. Array length is {array.Length}.";
                        logs?.Error(msg, depth);
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{StringUtils.GetPadding(depth)}{msg}");
                        overallSuccess = false;
                        continue;
                    }

                    var currentElement = array.GetValue(idx);
                    var success = reflector.TryModify(
                        ref currentElement,
                        data: indexedField,
                        fallbackObjType: elementType,
                        depth: depth + 1,
                        logs: logs,
                        flags: flags,
                        logger: logger);

                    if (success)
                        array.SetValue(currentElement, idx);

                    overallSuccess &= success;
                }
            }
            else if (obj is IList list)
            {
                foreach (var indexedField in indexedFields)
                {
                    var idx = ParseArrayIndex(indexedField.name!);
                    if (idx < 0 || idx >= list.Count)
                    {
                        var msg = $"Bracket segment '[{idx}]' index out of range on type '{type.GetTypeShortName()}'. List count is {list.Count}.";
                        logs?.Error(msg, depth);
                        if (logger?.IsEnabled(LogLevel.Error) == true)
                            logger.LogError($"{StringUtils.GetPadding(depth)}{msg}");
                        overallSuccess = false;
                        continue;
                    }

                    var currentElement = list[idx];
                    var success = reflector.TryModify(
                        ref currentElement,
                        data: indexedField,
                        fallbackObjType: elementType,
                        depth: depth + 1,
                        logs: logs,
                        flags: flags,
                        logger: logger);

                    if (success)
                        list[idx] = currentElement;

                    overallSuccess &= success;
                }
            }
            else
            {
                var msg = $"Cannot modify array elements: type '{type.GetTypeShortName()}' is not an array or list.";
                logs?.Error(msg, depth);
                if (logger?.IsEnabled(LogLevel.Error) == true)
                    logger.LogError($"{StringUtils.GetPadding(depth)}{msg}");
                return false;
            }

            return overallSuccess;
        }

        private static bool IsArrayIndexName(string? name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 3)
                return false;
            if (name[0] != '[' || name[name.Length - 1] != ']')
                return false;
            return int.TryParse(name.Substring(1, name.Length - 2), out _);
        }

        private static int ParseArrayIndex(string name)
            => int.Parse(name.Substring(1, name.Length - 2));
    }
}
