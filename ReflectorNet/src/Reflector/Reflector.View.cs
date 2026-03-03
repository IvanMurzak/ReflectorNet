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
using System.Text.RegularExpressions;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public partial class Reflector
    {
        /// <summary>
        /// Serializes the object (or a navigated subtree) with optional filtering.
        /// Without a query (or with an empty query) this is equivalent to Serialize().
        ///
        /// ViewQuery options (applied in this order):
        ///   Path        — navigate to a path first, then serialize only that subtree
        ///   MaxDepth    — prune the returned tree to N levels (0 = root typeName/value only)
        ///   NamePattern — keep only branches containing a field/property name matching a .NET regex
        ///   TypeFilter  — keep only branches whose typeName resolves to a type assignable to this
        ///
        /// Filter order matters: MaxDepth prunes the tree before NamePattern/TypeFilter are applied,
        /// so members deeper than MaxDepth are excluded even if their name or type would match.
        ///
        /// Errors are accumulated in the optional Logs object; nothing is thrown.
        /// </summary>
        public SerializedMember? View(
            object? obj,
            ViewQuery? query = null,
            Type? fallbackObjType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            // Step 1: Navigate to path if specified
            var target     = obj;
            var targetType = obj?.GetType() ?? fallbackObjType;

            if (!string.IsNullOrEmpty(query?.Path))
            {
                var segments = ParsePath(query!.Path);
                for (int i = 0; i < segments.Length; i++)
                {
                    if (!TryNavigateOneSegment(ref target, ref targetType, segments[i], depth + i, logs, flags, logger))
                        return null;
                }
                depth += segments.Length;
            }

            // Step 2: Serialize
            var serialized = Serialize(target, targetType ?? fallbackObjType, depth: depth, logs: logs, flags: flags, logger: logger);

            // Step 3: Apply filters
            if (query != null)
            {
                if (query.MaxDepth.HasValue)
                    serialized = PruneToDepth(serialized, query.MaxDepth.Value);

                if (!string.IsNullOrEmpty(query.NamePattern))
                {
                    var filtered = FilterByNamePattern(serialized, query.NamePattern);
                    // If nothing matched, return root envelope with empty fields/props
                    serialized = filtered ?? new SerializedMember { name = serialized.name, typeName = serialized.typeName };
                }

                if (query.TypeFilter != null)
                {
                    var filtered = FilterByType(serialized, query.TypeFilter);
                    serialized = filtered ?? new SerializedMember { name = serialized.name, typeName = serialized.typeName };
                }
            }

            return serialized;
        }

        /// <summary>
        /// Searches the object graph for all fields and properties whose name matches the given
        /// .NET regex pattern (case-insensitive) and returns them as a flat list of path+value pairs.
        ///
        /// Similar to the grep command: a plain string matches exactly, "orbit.*" matches all names
        /// beginning with "orbit". maxDepth limits how many levels deep the search recurses
        /// (null = unlimited).
        ///
        /// Errors are accumulated in the optional Logs object; nothing is thrown.
        /// </summary>
        public IReadOnlyList<ViewMatch> Grep(
            object? obj,
            string namePattern,
            int? maxDepth = null,
            Type? fallbackObjType = null,
            int depth = 0,
            Logs? logs = null,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            var matches = new List<ViewMatch>();
            var visited = new HashSet<object>(ObjectReferenceEqualityComparer.Instance);
            GrepWalk(obj, obj?.GetType() ?? fallbackObjType, namePattern, maxDepth, 0,
                     string.Empty, matches, visited, depth, logs, flags, logger);
            return matches.AsReadOnly();
        }

        // ─── Grep walker ──────────────────────────────────────────────────────────────

        private void GrepWalk(
            object? obj,
            Type? objType,
            string namePattern,
            int? maxDepth,
            int currentDepth,
            string currentPath,
            List<ViewMatch> matches,
            HashSet<object> visited,
            int serializeDepth,
            Logs? logs,
            BindingFlags flags,
            ILogger? logger)
        {
            if (obj == null) return;
            if (maxDepth.HasValue && currentDepth > maxDepth.Value) return;

            var resolvedType = obj.GetType() ?? objType;
            if (resolvedType == null) return;

            // Circular-reference guard for reference types
            if (!resolvedType.IsValueType)
            {
                if (!visited.Add(obj)) return;
            }

            // Arrays / IList — recurse into elements (name-match does not apply to index paths)
            if (obj is IList list)
            {
                var elementType = TypeUtils.GetEnumerableItemType(resolvedType);
                for (int i = 0; i < list.Count; i++)
                {
                    var elemPath = currentPath.Length == 0 ? $"[{i}]" : $"{currentPath}/[{i}]";
                    GrepWalk(list[i], elementType, namePattern, maxDepth, currentDepth + 1,
                             elemPath, matches, visited, serializeDepth, logs, flags, logger);
                }
                return; // arrays also implement IEnumerable — skip field/dict scanning
            }

            // Fields
            var fields = GetSerializableFields(resolvedType, flags, logger);
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    var fieldPath = currentPath.Length == 0 ? field.Name : $"{currentPath}/{field.Name}";

                    object? fieldValue;
                    try { fieldValue = field.GetValue(obj); }
                    catch { continue; }

                    if (Regex.IsMatch(field.Name, namePattern, RegexOptions.IgnoreCase))
                    {
                        var serialized = Serialize(fieldValue, field.FieldType, name: field.Name,
                                                   depth: serializeDepth, logs: logs, flags: flags, logger: logger);
                        matches.Add(new ViewMatch(fieldPath, serialized));
                    }

                    // Recurse into non-primitive types (IList is handled by GrepWalk's own guard at entry)
                    if (!TypeUtils.IsPrimitive(field.FieldType))
                        GrepWalk(fieldValue, field.FieldType, namePattern, maxDepth, currentDepth + 1,
                                 fieldPath, matches, visited, serializeDepth, logs, flags, logger);
                }
            }

            // Properties
            var props = GetSerializableProperties(resolvedType, flags, logger);
            if (props != null)
            {
                foreach (var prop in props)
                {
                    if (!prop.CanRead) continue;

                    var propPath = currentPath.Length == 0 ? prop.Name : $"{currentPath}/{prop.Name}";

                    object? propValue;
                    try { propValue = prop.GetValue(obj); }
                    catch { continue; }

                    if (Regex.IsMatch(prop.Name, namePattern, RegexOptions.IgnoreCase))
                    {
                        var serialized = Serialize(propValue, prop.PropertyType, name: prop.Name,
                                                   depth: serializeDepth, logs: logs, flags: flags, logger: logger);
                        matches.Add(new ViewMatch(propPath, serialized));
                    }

                    // Recurse into non-primitive types
                    if (!TypeUtils.IsPrimitive(prop.PropertyType))
                        GrepWalk(propValue, prop.PropertyType, namePattern, maxDepth, currentDepth + 1,
                                 propPath, matches, visited, serializeDepth, logs, flags, logger);
                }
            }

            // Dictionaries — recurse into values (name-match does not apply to key paths)
            if (TypeUtils.IsDictionary(resolvedType))
            {
                var args    = TypeUtils.GetDictionaryGenericArguments(resolvedType);
                var valType = args?[1] ?? typeof(object);
                var dict    = (IDictionary)obj;
                foreach (var key in dict.Keys)
                {
                    var valuePath = currentPath.Length == 0 ? $"[{key}]" : $"{currentPath}/[{key}]";
                    GrepWalk(dict[key], valType, namePattern, maxDepth, currentDepth + 1,
                             valuePath, matches, visited, serializeDepth, logs, flags, logger);
                }
            }
        }

        // ─── Filter helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a shallow copy of <paramref name="m"/> with all fields/props beyond
        /// <paramref name="maxRelativeDepth"/> levels stripped.
        /// maxRelativeDepth = 0 keeps the node itself (typeName + value) but removes all children.
        /// </summary>
        private static SerializedMember PruneToDepth(SerializedMember m, int maxRelativeDepth)
        {
            if (maxRelativeDepth <= 0)
                return new SerializedMember { name = m.name, typeName = m.typeName, valueJsonElement = m.valueJsonElement };

            var result = new SerializedMember { name = m.name, typeName = m.typeName, valueJsonElement = m.valueJsonElement };

            if (m.fields != null)
            {
                result.fields = new SerializedMemberList(m.fields.Count);
                foreach (var f in m.fields)
                    result.fields.Add(PruneToDepth(f, maxRelativeDepth - 1));
            }

            if (m.props != null)
            {
                result.props = new SerializedMemberList(m.props.Count);
                foreach (var p in m.props)
                    result.props.Add(PruneToDepth(p, maxRelativeDepth - 1));
            }

            return result;
        }

        /// <summary>
        /// Keeps only branches containing at least one field/property whose name matches
        /// <paramref name="pattern"/> (regex, case-insensitive).
        /// Returns null when nothing in this subtree matches.
        /// </summary>
        private static SerializedMember? FilterByNamePattern(SerializedMember m, string pattern)
        {
            var selfMatch      = m.name != null && Regex.IsMatch(m.name, pattern, RegexOptions.IgnoreCase);
            var filteredFields = FilterListByNamePattern(m.fields, pattern);
            var filteredProps  = FilterListByNamePattern(m.props,  pattern);

            if (!selfMatch && filteredFields == null && filteredProps == null)
                return null;

            return new SerializedMember
            {
                name             = m.name,
                typeName         = m.typeName,
                valueJsonElement = m.valueJsonElement,
                fields           = selfMatch ? m.fields : filteredFields,
                props            = selfMatch ? m.props  : filteredProps,
            };
        }

        private static SerializedMemberList? FilterListByNamePattern(SerializedMemberList? list, string pattern)
        {
            if (list == null || list.Count == 0) return null;

            SerializedMemberList? result = null;
            foreach (var m in list)
            {
                var filtered = FilterByNamePattern(m, pattern);
                if (filtered != null)
                {
                    result ??= new SerializedMemberList();
                    result.Add(filtered);
                }
            }
            return result;
        }

        /// <summary>
        /// Keeps only branches where the resolved typeName is assignable to <paramref name="typeFilter"/>.
        /// Returns null when nothing in this subtree matches.
        /// </summary>
        private static SerializedMember? FilterByType(SerializedMember m, Type typeFilter)
        {
            var memberType = TypeUtils.GetType(m.typeName);
            var typeMatch  = memberType != null && typeFilter.IsAssignableFrom(memberType);

            var filteredFields = FilterListByType(m.fields, typeFilter);
            var filteredProps  = FilterListByType(m.props,  typeFilter);

            if (!typeMatch && filteredFields == null && filteredProps == null)
                return null;

            return new SerializedMember
            {
                name             = m.name,
                typeName         = m.typeName,
                valueJsonElement = m.valueJsonElement,
                fields           = typeMatch ? m.fields : filteredFields,
                props            = typeMatch ? m.props  : filteredProps,
            };
        }

        private static SerializedMemberList? FilterListByType(SerializedMemberList? list, Type typeFilter)
        {
            if (list == null || list.Count == 0) return null;

            SerializedMemberList? result = null;
            foreach (var m in list)
            {
                var filtered = FilterByType(m, typeFilter);
                if (filtered != null)
                {
                    result ??= new SerializedMemberList();
                    result.Add(filtered);
                }
            }
            return result;
        }

        // Comparer that uses object identity (ReferenceEquals) rather than value equality.
        // Avoids false-positive cycle-detection when two distinct objects share the same GetHashCode().
        // netstandard2.1 does not provide System.Collections.Generic.ReferenceEqualityComparer,
        // so we supply our own.
        private sealed class ObjectReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ObjectReferenceEqualityComparer Instance = new ObjectReferenceEqualityComparer();
            private ObjectReferenceEqualityComparer() { }
            public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
