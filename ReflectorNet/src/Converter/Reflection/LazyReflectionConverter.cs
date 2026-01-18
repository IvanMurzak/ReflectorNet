/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet.Utils;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    /// <summary>
    /// A reflection converter that resolves its target type lazily by name.
    /// This is useful for optional dependencies where the target type might not be present at runtime.
    /// If the type is not found, this converter will remain inactive (priority 0).
    /// </summary>
    public class LazyReflectionConverter : GenericReflectionConverter<object>
    {
        private readonly string _targetTypeName;
        private readonly HashSet<string> _ignoredProperties;
        private readonly HashSet<string> _ignoredFields;
        private Type? _targetType;
        private bool _typeResolved;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyReflectionConverter"/> class.
        /// </summary>
        /// <param name="targetTypeName">The full name of the type to handle.</param>
        /// <param name="ignoredProperties">Optional list of property names to ignore during serialization.</param>
        /// <param name="ignoredFields">Optional list of field names to ignore during serialization.</param>
        public LazyReflectionConverter(string targetTypeName, IEnumerable<string>? ignoredProperties = null, IEnumerable<string>? ignoredFields = null)
        {
            if (string.IsNullOrWhiteSpace(targetTypeName))
                throw new ArgumentException("Target type name cannot be null or empty.", nameof(targetTypeName));

            _targetTypeName = targetTypeName;
            _ignoredProperties = ignoredProperties != null
                ? new HashSet<string>(ignoredProperties)
                : new HashSet<string>();
            _ignoredFields = ignoredFields != null
                ? new HashSet<string>(ignoredFields)
                : new HashSet<string>();
        }

        private Type? GetTargetType()
        {
            if (!_typeResolved)
            {
                _targetType = TypeUtils.GetType(_targetTypeName);
                _typeResolved = true;
            }
            return _targetType;
        }

        public override int SerializationPriority(Type type, ILogger? logger = null)
        {
            var targetType = GetTargetType();

            // If the target type cannot be resolved, this converter is inactive.
            if (targetType == null)
                return 0;

            // Exact match
            if (type == targetType)
                return MAX_DEPTH + 1;

            // Check inheritance
            var distance = TypeUtils.GetInheritanceDistance(baseType: targetType, targetType: type);

            return distance >= 0
                ? MAX_DEPTH - distance
                : 0;
        }

        protected override IEnumerable<string> GetIgnoredProperties()
        {
            return _ignoredProperties;
        }

        protected override IEnumerable<string> GetIgnoredFields()
        {
            return _ignoredFields;
        }
    }
}
