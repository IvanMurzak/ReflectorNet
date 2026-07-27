/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * SPDX-License-Identifier: Apache-2.0
 * Copyright (c) 2024-2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace com.IvanMurzak.ReflectorNet.Model
{
    /// <summary>
    /// How a raw <c>value</c> JSON payload relates to the <see cref="SerializedMember"/> schema.
    /// </summary>
    public enum SerializedMemberShapeKind
    {
        /// <summary>No payload at all, or an explicit JSON <c>null</c>. Means "no value", not a failure.</summary>
        Absent,

        /// <summary>
        /// A JSON object whose every property is a known <see cref="SerializedMember"/> key. An empty
        /// object <c>{}</c> qualifies - that is the shape ReflectorNet itself emits for a cascade
        /// member whose data travels in <c>fields</c>/<c>props</c>.
        /// </summary>
        SerializedMember,

        /// <summary>
        /// A JSON object with at least one property and NOT ONE known <see cref="SerializedMember"/>
        /// key - for example a consumer object reference <c>{"instanceID":"12345"}</c>. The payload
        /// is not trying to be a <see cref="SerializedMember"/> at all, so a converter that only
        /// understands <see cref="SerializedMember"/> must DECLINE it
        /// (<c>DeserializationOutcome.NotApplicable</c>) rather than call it broken.
        /// </summary>
        Foreign,

        /// <summary>
        /// A JSON object that mixes known <see cref="SerializedMember"/> keys with unknown ones. It IS
        /// trying to be a <see cref="SerializedMember"/> and getting it wrong, so it is a hard failure
        /// and must be reported loudly.
        /// </summary>
        Malformed,

        /// <summary>The payload is present but is not a JSON object (a number, string, array, ...).</summary>
        NotAnObject
    }

    /// <summary>
    /// Classification of a <c>value</c> JSON payload against the <see cref="SerializedMember"/> schema.
    /// </summary>
    /// <remarks>
    /// This classification is the crux of the converter tri-state: it is what lets a converter tell
    /// "not my shape, carry on" apart from "my shape, but broken". See
    /// <see cref="com.IvanMurzak.ReflectorNet.Converter.DeserializationOutcome"/>.
    /// </remarks>
    public readonly struct SerializedMemberShape
    {
        /// <summary>Every property name a <see cref="SerializedMember"/> JSON object may carry.</summary>
        public static readonly IReadOnlyList<string> KnownKeys = new[]
        {
            nameof(SerializedMember.name),
            nameof(SerializedMember.typeName),
            SerializedMember.ValueName,
            nameof(SerializedMember.fields),
            nameof(SerializedMember.props)
        };

        static readonly string[] EmptyKeys = Array.Empty<string>();

        public SerializedMemberShapeKind Kind { get; }

        /// <summary>Property names present in the payload that are NOT <see cref="SerializedMember"/> keys.</summary>
        public IReadOnlyList<string> UnknownKeys { get; }

        /// <summary>How many <see cref="SerializedMember"/> keys the payload carries.</summary>
        public int RecognisedKeyCount { get; }

        SerializedMemberShape(SerializedMemberShapeKind kind, IReadOnlyList<string> unknownKeys, int recognisedKeyCount)
        {
            Kind = kind;
            UnknownKeys = unknownKeys;
            RecognisedKeyCount = recognisedKeyCount;
        }

        /// <summary>
        /// <c>true</c> when the payload carries no <see cref="SerializedMember"/> key at all, so a
        /// converter that only speaks <see cref="SerializedMember"/> has nothing to say about it.
        /// </summary>
        public bool IsForeign => Kind == SerializedMemberShapeKind.Foreign;

        /// <summary>The first unrecognised property name, or <c>null</c> when there is none.</summary>
        public string? FirstUnknownKey => UnknownKeys.Count > 0 ? UnknownKeys[0] : null;

        /// <summary>
        /// Human-readable description of the offending keys, phrased the same way
        /// <c>SerializedMemberConverter.Read</c> phrases it so the two paths read alike.
        /// </summary>
        public string DescribeUnknownKeys()
        {
            if (UnknownKeys.Count == 0)
                return string.Empty;

            return $"Unexpected property name: '{string.Join("', '", UnknownKeys)}'. "
                + $"Did you want to use '{string.Join("', '", KnownKeys)}'?";
        }

        static bool IsKnownKey(string name)
            => name == nameof(SerializedMember.name)
            || name == nameof(SerializedMember.typeName)
            || name == SerializedMember.ValueName
            || name == nameof(SerializedMember.fields)
            || name == nameof(SerializedMember.props);

        /// <summary>
        /// Classifies a raw <c>value</c> payload against the <see cref="SerializedMember"/> schema.
        /// Pure: reads the payload's property names and nothing else.
        /// </summary>
        /// <remarks>
        /// Runs on the per-member deserialization path, so the common case (a well-formed payload)
        /// is allocation-free: the unknown-key list is only created once an unknown key is seen.
        /// </remarks>
        public static SerializedMemberShape Classify(JsonElement? value)
        {
            if (value == null || value.Value.ValueKind == JsonValueKind.Null || value.Value.ValueKind == JsonValueKind.Undefined)
                return new SerializedMemberShape(SerializedMemberShapeKind.Absent, EmptyKeys, 0);

            if (value.Value.ValueKind != JsonValueKind.Object)
                return new SerializedMemberShape(SerializedMemberShapeKind.NotAnObject, EmptyKeys, 0);

            var recognisedKeyCount = 0;
            var unknown = default(List<string>);

            foreach (var property in value.Value.EnumerateObject())
            {
                if (IsKnownKey(property.Name))
                    recognisedKeyCount++;
                else
                    (unknown ??= new List<string>()).Add(property.Name);
            }

            // An empty JSON object carries no unknown key, so it is a (vacuously valid) SerializedMember.
            // That is deliberate: `{}` is the exact shape ReflectorNet emits for a cascade member whose
            // data travels in 'fields'/'props'.
            if (unknown == null)
                return new SerializedMemberShape(SerializedMemberShapeKind.SerializedMember, EmptyKeys, recognisedKeyCount);

            return recognisedKeyCount == 0
                ? new SerializedMemberShape(SerializedMemberShapeKind.Foreign, unknown, recognisedKeyCount)
                : new SerializedMemberShape(SerializedMemberShapeKind.Malformed, unknown, recognisedKeyCount);
        }
    }
}
