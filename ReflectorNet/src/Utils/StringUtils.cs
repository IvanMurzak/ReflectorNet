using System;
using System.Collections.Concurrent;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /*
     * ReflectorNet
     * Author: Ivan Murzak (https://github.com/IvanMurzak)
     * Copyright (c) 2025 Ivan Murzak
     * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
     */

    public static class StringUtils
    {
        public const string Null = "null";
        public const string NA = "N/A";

        static readonly ConcurrentDictionary<int, string> _paddingCache = new ConcurrentDictionary<int, string>();

        public static string GetPadding(int depth)
        {
            if (depth < 0)
                return string.Empty;

            return _paddingCache.GetOrAdd(depth, static d => new string(' ', d * 2));
        }

        public static bool IsNullOrEmpty(string? value) => string.IsNullOrEmpty(value) || value == Null;
        public static bool IsNullOrWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) || value == Null;

        public static string? TrimPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var span = path.AsSpan();
            span = span.Trim('/');
            return span.IsEmpty ? string.Empty : span.ToString();
        }

        public static bool Path_ParseParent(string? path, out string? parentPath, out string? name)
        {
            if (string.IsNullOrEmpty(path))
            {
                parentPath = null;
                name = null;
                return false;
            }

            var span = path.AsSpan().Trim('/');
            if (span.IsEmpty)
            {
                parentPath = null;
                name = null;
                return false;
            }

            var lastSlashIndex = span.LastIndexOf('/');
            if (lastSlashIndex >= 0)
            {
                parentPath = span.Slice(0, lastSlashIndex).ToString();
                name = span.Slice(lastSlashIndex + 1).ToString();
                return true;
            }
            else
            {
                parentPath = null;
                name = span.ToString();
                return false;
            }
        }
        public static string? Path_GetParentFolderPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var span = path.AsSpan().TrimEnd('/');
            var lastSlashIndex = span.LastIndexOf('/');
            return lastSlashIndex >= 0
                ? span.Slice(0, lastSlashIndex).ToString()
                : span.ToString();
        }
        public static string? Path_GetLastName(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            var span = path.AsSpan().TrimEnd('/');
            var lastSlashIndex = span.LastIndexOf('/');
            return lastSlashIndex >= 0
                ? span.Slice(lastSlashIndex + 1).ToString()
                : span.ToString();
        }
        public static object? ConvertParameterStringToEnum(object? value, Type enumType, string parameterName)
        {
            if (value is string stringValue && enumType.IsEnum)
            {
                if (Enum.TryParse(enumType, stringValue, ignoreCase: true, out var result))
                {
                    if (Enum.IsDefined(enumType, result!))
                    {
                        return result;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Value '{stringValue}' for parameter '{parameterName}' was parsed but is not a defined member of '{enumType.GetTypeId()}'. Valid values are: {string.Join(", ", Enum.GetNames(enumType))}");
                    }
                }
                else
                {
                    throw new ArgumentException(
                        $"Value '{stringValue}' for parameter '{parameterName}' could not be parsed as '{enumType.GetTypeId()}'. Valid values are: {string.Join(", ", Enum.GetNames(enumType))}");
                }
            }
            throw new ArgumentException($"Parameter '{parameterName}' type mismatch. Expected '{enumType.GetTypeId()}', but got '{value?.GetType()}'.");
        }
    }
}