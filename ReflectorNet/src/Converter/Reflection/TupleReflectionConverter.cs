/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet.Converter
{
    /// <summary>
    /// Specialized converter for ITuple types (ValueTuple, Tuple).
    /// Handles the ITuple interface's indexer property that cannot be serialized directly.
    /// </summary>
    public class TupleReflectionConverter : GenericReflectionConverter<ITuple>
    {
        public override int SerializationPriority(Type type, ILogger? logger = null)
        {
            // Check if type implements ITuple
            if (typeof(ITuple).IsAssignableFrom(type))
                return MAX_DEPTH + 2; // Higher priority than GenericReflectionConverter

            return 0;
        }

        protected override IEnumerable<PropertyInfo>? GetSerializablePropertiesInternal(
            Reflector reflector,
            Type objType,
            BindingFlags flags,
            ILogger? logger = null)
        {
            // Delegate to base implementation; it already filters out indexer properties.
            return base.GetSerializablePropertiesInternal(reflector, objType, flags, logger);
        }
    }
}
