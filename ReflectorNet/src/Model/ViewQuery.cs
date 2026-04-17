/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.ComponentModel;

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
        [Description(
            "Navigate to this path first, then serialize only that subtree. " +
            "Path segments are separated by '/'. " +
            "Use '[i]' for array/list index (e.g. 'users/[2]/name') and '[key]' for dictionary entry (e.g. 'config/[timeout]'). " +
            "A leading '#/' is stripped automatically. " +
            "Examples: 'admin/name', 'users/[0]/email', 'config/[timeout]'. " +
            "Leave null to start from the root object.")]
        public string? Path { get; set; }

        /// <summary>
        /// .NET regex pattern applied to field/property names (case-insensitive).
        /// Only branches that contain at least one matching name are kept in the result tree.
        /// A plain string like "orbitRadius" matches that name exactly; "orbit.*" matches all
        /// field/property names that start with "orbit".
        /// When no match is found anywhere, the root envelope is returned with empty fields/props
        /// so that the caller can still inspect the root type.
        /// </summary>
        [Description(
            "Case-insensitive .NET regex pattern matched against field and property names. " +
            "Only branches containing at least one match are kept in the result tree. " +
            "Examples: 'orbitRadius' (exact name), 'orbit.*' (prefix match), 'radius|speed' (either name). " +
            "When nothing matches, the root envelope is returned with empty fields/props. " +
            "Leave null to return all fields and properties without filtering.")]
        public string? NamePattern { get; set; }

        /// <summary>
        /// Maximum depth of the returned tree.
        /// 0 = root typeName/value only — no nested fields or properties.
        /// 1 = one level of fields/props visible, their children stripped.
        /// null = unlimited depth (default).
        /// </summary>
        [Description(
            "Maximum nesting depth of the returned serialized tree. " +
            "0 = root type name and value only — no nested fields or properties. " +
            "1 = one level of fields/props visible, their children stripped. " +
            "2 = two levels visible, and so on. " +
            "Leave null (default) for unlimited depth.")]
        public int? MaxDepth { get; set; }

        /// <summary>
        /// When set, only members whose resolved typeName is assignable to this type are kept.
        /// Non-matching branches are pruned; the root envelope is preserved even with no matches.
        /// </summary>
        [Description(
            "When set, prunes the result tree to members whose runtime type is assignable to this type. " +
            "Non-matching branches are removed; the root envelope is always preserved. " +
            "Examples: typeof(float) keeps only float fields, typeof(IEnumerable) keeps only collections. " +
            "Leave null to include members of any type.")]
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
        [Description(
            "Full slash-delimited path to the matched location within the object graph. " +
            "Array elements use bracket notation. " +
            "Examples: 'orbitRadius', 'celestialBodies/[0]/orbitRadius', 'config/[timeout]'.")]
        public string Path { get; }

        /// <summary>
        /// Serialized value at the matched location.
        /// </summary>
        [Description("Serialized representation of the value found at the matched path.")]
        public SerializedMember Value { get; }

        public ViewMatch(string path, SerializedMember value)
        {
            Path  = path;
            Value = value;
        }
    }
}
