/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace com.IvanMurzak.ReflectorNet.Model
{
    /// <summary>
    /// Manages the context for serialization, specifically for detecting recursive cycles.
    /// </summary>
    public class SerializationContext
    {
        private readonly Dictionary<object, List<string>> _visited;
        private readonly Stack<string> _pathStack;

        public SerializationContext()
        {
            _visited = new Dictionary<object, List<string>>(new ReferenceEqualityComparer());
            _pathStack = new Stack<string>();
            _pathStack.Push("#"); // Root
        }

        /// <summary>
        /// Enters a new object scope.
        /// </summary>
        /// <param name="obj">The object being visited.</param>
        /// <param name="segment">The path segment (property name or index) leading to this object.</param>
        /// <returns>True if the object can be visited (no cycle); False if a cycle is detected.</returns>
        public bool Enter(object obj, string? segment)
        {
            if (!string.IsNullOrEmpty(segment))
            {
                _pathStack.Push(segment!);
            }

            if (_visited.ContainsKey(obj))
            {
                // Cycle detected, do not add to visited again, but we still pushed the segment if any.
                // Wait, if we return false, the caller should handle the cycle.
                // But we modified the stack. The caller must ensure Exit is called or we should not modify stack if cycle?
                // If cycle detected, we usually return immediately and don't traverse children.
                // But we need to pop the stack if we pushed it.
                // It's cleaner if the caller always calls Exit if Enter was called (or we handle it here).
                // Let's stick to: Caller calls Enter. If false, caller handles ref. Caller calls Exit eventually?
                // Actually, if Enter returns false, we usually return a reference and stop.
                // So we should probably pop immediately if we are not going to proceed?
                // Or better: The caller pattern is:
                // try { context.Enter(); ... } finally { context.Exit(); }
                // So Exit will be called.
                return false;
            }

            // Store the current path for this object
            // Stack enumerates LIFO (Reverse of path)
            _visited[obj] = new List<string>(_pathStack);
            return true;
        }

        /// <summary>
        /// Exits the object scope.
        /// </summary>
        /// <param name="obj">The object being left.</param>
        /// <param name="segment">The path segment that was used to enter.</param>
        public void Exit(object obj, string? segment)
        {
            _visited.Remove(obj);
            if (!string.IsNullOrEmpty(segment))
            {
                _pathStack.Pop();
            }
        }

        /// <summary>
        /// Gets the JSON Pointer path to the first occurrence of the object.
        /// </summary>
        /// <param name="obj">The object to find.</param>
        /// <returns>The JSON Pointer string.</returns>
        public string GetPath(object obj)
        {
            if (_visited.TryGetValue(obj, out var pathSegments))
            {
                var segments = new List<string>(pathSegments);
                segments.Reverse();
                // Join with /
                // If segments are ["#", "Prop"], result "#/Prop"
                // If segments are ["#"], result "#"
                if (segments.Count == 1 && segments[0] == "#")
                    return "#";

                return string.Join("/", segments).Replace("#/", "#/");
                // Actually string.Join("/", ["#", "A"]) -> "#/A". Correct.
            }
            return "#";
        }

        private class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);
            int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
