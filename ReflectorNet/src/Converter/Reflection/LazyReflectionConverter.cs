/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using com.IvanMurzak.ReflectorNet.Model;
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
        private readonly IReflectionConverter? _backingConverter;
        private readonly Lazy<Type?> _targetType;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyReflectionConverter"/> class.
        /// </summary>
        /// <param name="targetTypeName">The full name of the type to handle.</param>
        /// <param name="ignoredProperties">Optional list of property names to ignore during serialization. Cannot be used with <paramref name="backingConverter"/>.</param>
        /// <param name="ignoredFields">Optional list of field names to ignore during serialization. Cannot be used with <paramref name="backingConverter"/>.</param>
        /// <param name="backingConverter">Optional converter to delegate serialization to. Cannot be used with <paramref name="ignoredProperties"/> or <paramref name="ignoredFields"/>.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="targetTypeName"/> is null or empty, or when <paramref name="backingConverter"/> is provided together with <paramref name="ignoredProperties"/> or <paramref name="ignoredFields"/>.</exception>
        public LazyReflectionConverter(
            string targetTypeName,
            IEnumerable<string>? ignoredProperties = null,
            IEnumerable<string>? ignoredFields = null,
            IReflectionConverter? backingConverter = null)
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

            var hasIgnoredMembers = _ignoredProperties.Count > 0 || _ignoredFields.Count > 0;
            if (backingConverter != null && hasIgnoredMembers)
                throw new ArgumentException("Cannot specify ignoredProperties or ignoredFields when using a backingConverter. The backing converter handles serialization entirely.", nameof(backingConverter));

            _backingConverter = backingConverter;
            _targetType = new Lazy<Type?>(() => TypeUtils.GetType(_targetTypeName));
        }

        private Type? GetTargetType() => _targetType.Value;

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

        protected override SerializedMember InternalSerialize(
            Reflector reflector,
            object? obj,
            Type type,
            string? name = null,
            bool recursive = true,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            int depth = 0,
            Logs? logs = null,
            ILogger? logger = null,
            SerializationContext? context = null)
        {
            if (_backingConverter != null)
            {
                return _backingConverter.Serialize(
                    reflector: reflector,
                    obj: obj,
                    fallbackType: type,
                    name: name,
                    recursive: recursive,
                    flags: flags,
                    depth: depth,
                    logs: logs,
                    logger: logger,
                    context: context);
            }

            return base.InternalSerialize(
                reflector: reflector,
                obj: obj,
                type: type,
                name: name,
                recursive: recursive,
                flags: flags,
                depth: depth,
                logs: logs,
                logger: logger,
                context: context);
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
