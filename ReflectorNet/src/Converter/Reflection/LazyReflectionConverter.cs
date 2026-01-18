/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
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
        /// <param name="ignoredProperties">Optional list of property names to ignore during serialization.</param>
        /// <param name="ignoredFields">Optional list of field names to ignore during serialization.</param>
        /// <param name="backingConverter">Optional converter to delegate serialization to.</param>
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

            _backingConverter = backingConverter;
            _targetType = new Lazy<Type?>(() => TypeUtils.GetType(_targetTypeName));
        }

        public override int SerializationPriority(Type type, ILogger? logger = null)
        {
            var targetType = _targetType.Value;

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

        protected override IEnumerable<PropertyInfo>? GetSerializablePropertiesInternal(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            if (_backingConverter != null)
            {
                return _backingConverter.GetSerializableProperties(reflector, objType, flags, logger);
            }

            return base.GetSerializablePropertiesInternal(reflector, objType, flags, logger);
        }

        protected override IEnumerable<FieldInfo>? GetSerializableFieldsInternal(
            Reflector reflector,
            Type objType,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            ILogger? logger = null)
        {
            if (_backingConverter != null)
            {
                return _backingConverter.GetSerializableFields(reflector, objType, flags, logger);
            }

            return base.GetSerializableFieldsInternal(reflector, objType, flags, logger);
        }

        protected override IEnumerable<string> GetIgnoredProperties()
        {
            foreach (var prop in base.GetIgnoredProperties())
                yield return prop;

            foreach (var prop in _ignoredProperties)
                yield return prop;
        }

        protected override IEnumerable<string> GetIgnoredFields()
        {
            foreach (var field in base.GetIgnoredFields())
                yield return field;

            foreach (var field in _ignoredFields)
                yield return field;
        }
    }
}
