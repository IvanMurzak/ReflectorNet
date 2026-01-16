/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /// <summary>
    /// Provides type info modifiers for System.Text.Json serialization.
    /// These modifiers customize how types are serialized by adjusting JsonTypeInfo metadata.
    /// </summary>
    public static class JsonTypeInfoModifiers
    {
        /// <summary>
        /// A type info modifier that excludes properties and fields marked with <see cref="ObsoleteAttribute"/>
        /// from JSON serialization. This modifier removes obsolete properties from the type metadata used
        /// during serialization, so the corresponding getters/setters are not invoked when writing JSON.
        /// It does not alter deserialization behavior.
        /// </summary>
        /// <param name="typeInfo">The JsonTypeInfo to modify.</param>
        public static void ExcludeObsoleteMembers(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object)
                return;

            // Collect obsolete properties first (can't modify collection while iterating)
            var obsoleteProperties = new List<JsonPropertyInfo>();
            foreach (var propertyInfo in typeInfo.Properties)
            {
                if (IsObsolete(propertyInfo))
                    obsoleteProperties.Add(propertyInfo);
            }

            // Remove obsolete properties from the collection
            foreach (var obsoleteProperty in obsoleteProperties)
                typeInfo.Properties.Remove(obsoleteProperty);
        }

        /// <summary>
        /// Determines whether the property info represents an obsolete member.
        /// Checks both PropertyInfo and FieldInfo for the <see cref="ObsoleteAttribute"/>.
        /// </summary>
        private static bool IsObsolete(JsonPropertyInfo propertyInfo)
        {
            var ap = propertyInfo.AttributeProvider;
            if (ap is null) return false;

            if (ap.IsDefined(typeof(ObsoleteAttribute), inherit: true))
                return true;

            if (ap is PropertyInfo p)
                return (p.GetMethod?.IsDefined(typeof(ObsoleteAttribute), true) ?? false)
                    || (p.SetMethod?.IsDefined(typeof(ObsoleteAttribute), true) ?? false);

            return false;
        }
    }
}
