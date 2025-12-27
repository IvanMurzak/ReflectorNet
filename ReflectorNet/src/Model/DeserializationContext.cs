/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;

namespace com.IvanMurzak.ReflectorNet.Model
{
    /// <summary>
    /// Manages the context for deserialization, specifically for resolving $ref references.
    /// </summary>
    public class DeserializationContext
    {
        private readonly Dictionary<string, object> _resolvedObjects;
        private readonly Stack<string> _pathStack;

        public DeserializationContext()
        {
            _resolvedObjects = new Dictionary<string, object>();
            _pathStack = new Stack<string>();
            _pathStack.Push("#"); // Root
        }

        /// <summary>
        /// Enters a new path segment (property name or array index).
        /// Call this BEFORE deserializing a child element.
        /// </summary>
        /// <param name="segment">The path segment (property name or "[index]" for arrays).</param>
        public void Enter(string? segment)
        {
            if (!string.IsNullOrEmpty(segment))
                _pathStack.Push(segment!);
        }

        /// <summary>
        /// Exits the current path segment.
        /// Call this AFTER deserializing a child element.
        /// </summary>
        /// <param name="segment">The path segment that was used to enter.</param>
        public void Exit(string? segment)
        {
            if (!string.IsNullOrEmpty(segment))
                _pathStack.Pop();
        }

        /// <summary>
        /// Registers a deserialized object at the current path.
        /// </summary>
        /// <param name="obj">The deserialized object to register.</param>
        public void Register(object obj)
        {
            var path = BuildCurrentPath();
            _resolvedObjects[path] = obj;
        }

        /// <summary>
        /// Attempts to resolve a $ref path to a previously deserialized object.
        /// </summary>
        /// <param name="refPath">The JSON Pointer path from the $ref value.</param>
        /// <param name="result">The resolved object if found, null otherwise.</param>
        /// <returns>True if the reference was resolved, false otherwise.</returns>
        public bool TryResolve(string refPath, out object? result)
        {
            if (_resolvedObjects.TryGetValue(refPath, out var obj))
            {
                result = obj;
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Gets the current JSON Pointer path.
        /// </summary>
        /// <returns>The current path as a JSON Pointer string.</returns>
        public string GetCurrentPath() => BuildCurrentPath();

        public string BuildCurrentPath()
        {
            if (_pathStack.Count == 1)
                return "#";

            // Stack enumerates LIFO, so reverse to get correct path order
            var segments = new string[_pathStack.Count];
            _pathStack.CopyTo(segments, 0);
            Array.Reverse(segments);
            return string.Join("/", segments);
        }
    }
}
