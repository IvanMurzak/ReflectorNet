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
        /// from JSON serialization. This modifier sets <see cref="JsonPropertyInfo.ShouldSerialize"/> to return
        /// false for any property whose underlying member has the <see cref="ObsoleteAttribute"/>.
        /// </summary>
        /// <param name="typeInfo">The JsonTypeInfo to modify.</param>
        public static void ExcludeObsoleteMembers(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object)
                return;

            foreach (var propertyInfo in typeInfo.Properties)
            {
                if (IsObsolete(propertyInfo))
                {
                    propertyInfo.ShouldSerialize = static (_, _) => false;
                }
            }
        }

        /// <summary>
        /// Determines whether the property info represents an obsolete member.
        /// Checks both PropertyInfo and FieldInfo for the <see cref="ObsoleteAttribute"/>.
        /// </summary>
        private static bool IsObsolete(JsonPropertyInfo propertyInfo)
        {
            var attributeProvider = propertyInfo.AttributeProvider;

            if (attributeProvider is PropertyInfo propInfo)
                return propInfo.GetCustomAttribute<ObsoleteAttribute>() != null;

            if (attributeProvider is FieldInfo fieldInfo)
                return fieldInfo.GetCustomAttribute<ObsoleteAttribute>() != null;

            return false;
        }
    }
}
