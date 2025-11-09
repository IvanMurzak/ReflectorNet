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

namespace com.IvanMurzak.ReflectorNet.Convertor
{
    public partial class IgnoreFieldsAndPropertiesReflectionConvertor<T> : GenericReflectionConvertor<T>
    {
        readonly bool _ignoreFields;
        readonly bool _ignoreProperties;

        public IgnoreFieldsAndPropertiesReflectionConvertor(bool ignoreFields, bool ignoreProperties)
        {
            _ignoreFields = ignoreFields;
            _ignoreProperties = ignoreProperties;
        }
        public override IEnumerable<FieldInfo>? GetSerializableFields(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => _ignoreFields
                ? null
                : base.GetSerializableFields(reflector, objType, flags, logger);

        public override IEnumerable<PropertyInfo>? GetSerializableProperties(Reflector reflector, Type objType, BindingFlags flags, ILogger? logger = null)
            => _ignoreProperties
                ? null
                : base.GetSerializableProperties(reflector, objType, flags, logger);
    }
}