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
        private readonly Dictionary<object, string> _visited;
        private readonly Stack<string> _pathStack;

        public SerializationContext()
        {
            _visited = new Dictionary<object, string>(new ReferenceEqualityComparer());
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
                // Cycle detected. Caller must still call Exit() to pop the segment from the stack.
                return false;
            }

            // Compute and store the path string directly (more memory efficient than storing List<string>)
            _visited[obj] = BuildCurrentPath();
            return true;
        }

        public string BuildCurrentPath()
        {
            if (_pathStack.Count == 1)
                return "#";

            // Stack enumerates LIFO, so reverse to get correct path order
            var segments = new string[_pathStack.Count];
            _pathStack.CopyTo(segments, 0);
            System.Array.Reverse(segments);
            return string.Join("/", segments);
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
            if (!_visited.TryGetValue(obj, out var path))
            {
                throw new System.InvalidOperationException(
                    $"Object of type '{obj.GetType().GetTypeShortName()}' was not found in the serialization context. " +
                    "GetPath should only be called for objects that have been visited.");
            }

            return path;
        }

        private class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);
            int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
