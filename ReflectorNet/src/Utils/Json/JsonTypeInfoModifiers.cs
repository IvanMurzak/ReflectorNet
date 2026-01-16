/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
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

            // Iterate backwards to allow in-place removal without allocation
            for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
            {
                if (IsObsolete(typeInfo.Properties[i]))
                    typeInfo.Properties.RemoveAt(i);
            }
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
                return (p.GetMethod?.IsDefined(typeof(ObsoleteAttribute), inherit: true) ?? false)
                    || (p.SetMethod?.IsDefined(typeof(ObsoleteAttribute), inherit: true) ?? false);

            return false;
        }
    }
}
