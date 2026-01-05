/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    /// <summary>
    /// Converter for types that should be serialized/deserialized without their fields and properties.
    /// This is useful for types that should be treated as read-only during serialization.
    /// </summary>
    public class IgnoreFieldsAndPropertiesReflectionConverter<T> : GenericReflectionConverter<T>
    {
        readonly bool _ignoreFields;
        readonly bool _ignoreProperties;

        public IgnoreFieldsAndPropertiesReflectionConverter(bool ignoreFields, bool ignoreProperties)
        {
            _ignoreFields = ignoreFields;
            _ignoreProperties = ignoreProperties;
        }

        /// <summary>
        /// Disable cascade serialization to prevent infinite recursion when serializing
        /// objects with complex property graphs.
        /// </summary>
        public override bool AllowCascadeSerialization => false;

        protected override IEnumerable<FieldInfo>? GetSerializableFieldsInternal(
            Reflector reflector,
            Type objType,
            BindingFlags flags,
            ILogger? logger = null)
        {
            return _ignoreFields
                ? null
                : base.GetSerializableFieldsInternal(reflector, objType, flags, logger);
        }

        protected override IEnumerable<PropertyInfo>? GetSerializablePropertiesInternal(
            Reflector reflector,
            Type objType,
            BindingFlags flags,
            ILogger? logger = null)
        {
            return _ignoreProperties
                ? null
                : base.GetSerializablePropertiesInternal(reflector, objType, flags, logger);
        }
    }
}