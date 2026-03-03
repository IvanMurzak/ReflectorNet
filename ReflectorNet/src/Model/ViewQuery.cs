/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;

namespace com.IvanMurzak.ReflectorNet.Model
{
    /// <summary>
    /// Options for <see cref="Reflector.View"/> — controls which parts of the serialized data to return.
    /// When no options are set, <c>View</c> is equivalent to <c>Serialize</c> (full tree returned).
    /// </summary>
    public class ViewQuery
    {
        /// <summary>
        /// Navigate to this path first, then serialize only that subtree.
        /// Uses the same format as TryReadAt / TryModifyAt:
        ///   - Plain segment navigates a field or property (e.g. "admin/name")
        ///   - [i] navigates an Array or IList element (e.g. "users/[2]/name")
        ///   - [key] navigates a dictionary entry (e.g. "config/[timeout]")
        ///   - Leading "#/" is stripped automatically (compatible with SerializationContext paths)
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// .NET regex pattern applied to field/property names (case-insensitive).
        /// Only branches that contain at least one matching name are kept in the result tree.
        /// A plain string like "orbitRadius" matches that name exactly; "orbit.*" matches all
        /// field/property names that start with "orbit".
        /// When no match is found anywhere, the root envelope is returned with empty fields/props
        /// so that the caller can still inspect the root type.
        /// </summary>
        public string? NamePattern { get; set; }

        /// <summary>
        /// Maximum depth of the returned tree.
        /// 0 = root typeName/value only — no nested fields or properties.
        /// 1 = one level of fields/props visible, their children stripped.
        /// null = unlimited depth (default).
        /// </summary>
        public int? MaxDepth { get; set; }

        /// <summary>
        /// When set, only members whose resolved typeName is assignable to this type are kept.
        /// Non-matching branches are pruned; the root envelope is preserved even with no matches.
        /// </summary>
        public Type? TypeFilter { get; set; }
    }

    /// <summary>
    /// A single result entry returned by <see cref="Reflector.Grep"/>.
    /// Holds the full slash-delimited path to the matched field/property and its serialized value.
    /// </summary>
    public class ViewMatch
    {
        /// <summary>
        /// Full path to the matched location, e.g. "celestialBodies/[0]/orbitRadius".
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Serialized value at the matched location.
        /// </summary>
        public SerializedMember Value { get; }

        public ViewMatch(string path, SerializedMember value)
        {
            Path  = path;
            Value = value;
        }
    }
}
