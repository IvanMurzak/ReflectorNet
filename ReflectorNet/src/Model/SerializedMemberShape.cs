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
        /// A JSON object with at least one unknown property and NOT ONE
        /// <see cref="SerializedMemberShape.StructuralKeys">structural</see>
        /// <see cref="SerializedMember"/> key - for example a consumer object reference
        /// <c>{"instanceID":"12345"}</c> or <c>{"instanceID":"7","typeName":"UnityEngine.Rigidbody"}</c>.
        /// The payload is not trying to be a <see cref="SerializedMember"/> at all, so a converter
        /// that only understands <see cref="SerializedMember"/> must DECLINE it
        /// (<c>DeserializationOutcome.NotApplicable</c>) rather than call it broken.
        /// </summary>
        Foreign,

        /// <summary>
        /// A JSON object that mixes <see cref="SerializedMemberShape.StructuralKeys">structural</see>
        /// <see cref="SerializedMember"/> keys with unknown ones. Only a structural key
        /// (<c>value</c> / <c>fields</c> / <c>props</c>) is strong enough evidence of intent: it IS
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

        /// <summary>
        /// The <see cref="KnownKeys"/> that are STRUCTURAL - unique to the <see cref="SerializedMember"/>
        /// shape and therefore real evidence that a payload was TRYING to be one.
        /// </summary>
        /// <remarks>
        /// These are the only keys that may turn an unknown key into a
        /// <see cref="SerializedMemberShapeKind.Malformed"/> verdict. See <see cref="DescriptiveKeys"/>
        /// for why the others cannot.
        /// </remarks>
        public static readonly IReadOnlyList<string> StructuralKeys = new[]
        {
            SerializedMember.ValueName,
            nameof(SerializedMember.fields),
            nameof(SerializedMember.props)
        };

        /// <summary>
        /// The <see cref="KnownKeys"/> that are DESCRIPTIVE - accepted on a
        /// <see cref="SerializedMember"/>, but far too generic to prove that a payload meant to be one.
        /// </summary>
        /// <remarks>
        /// <c>name</c> and <c>typeName</c> are ordinary English words. Consumer object-reference
        /// converters emit them freely: Unity-MCP's <c>GameObjectRefConverter</c> writes <c>name</c>
        /// and its <c>ComponentRefConverter</c> writes <c>typeName</c> when it disambiguates by type,
        /// so <c>{"instanceID":"7","typeName":"UnityEngine.Rigidbody"}</c> is a perfectly legitimate
        /// reference. Counting a descriptive key as evidence of intent classified such references as
        /// <see cref="SerializedMemberShapeKind.Malformed"/> and threw before the consumer's own
        /// converter could resolve them - the defect shipped in 5.3.3.
        /// </remarks>
        public static readonly IReadOnlyList<string> DescriptiveKeys = new[]
        {
            nameof(SerializedMember.name),
            nameof(SerializedMember.typeName)
        };

        static readonly string[] EmptyKeys = Array.Empty<string>();

        readonly IReadOnlyList<string>? _unknownKeys;

        public SerializedMemberShapeKind Kind { get; }

        /// <summary>
        /// Property names present in the payload that are NOT <see cref="SerializedMember"/> keys.
        /// Never <c>null</c>, including on a <c>default(SerializedMemberShape)</c>.
        /// </summary>
        public IReadOnlyList<string> UnknownKeys => _unknownKeys ?? EmptyKeys;

        /// <summary>How many <see cref="SerializedMember"/> keys the payload carries.</summary>
        public int RecognisedKeyCount { get; }

        /// <summary>
        /// How many <see cref="StructuralKeys">structural</see> keys the payload carries. This - NOT
        /// <see cref="RecognisedKeyCount"/> - is what separates <see cref="SerializedMemberShapeKind.Foreign"/>
        /// from <see cref="SerializedMemberShapeKind.Malformed"/>.
        /// </summary>
        public int StructuralKeyCount { get; }

        SerializedMemberShape(SerializedMemberShapeKind kind, IReadOnlyList<string>? unknownKeys, int recognisedKeyCount, int structuralKeyCount)
        {
            Kind = kind;
            _unknownKeys = unknownKeys;
            RecognisedKeyCount = recognisedKeyCount;
            StructuralKeyCount = structuralKeyCount;
        }

        /// <summary>
        /// <c>true</c> when the payload carries no <see cref="StructuralKeys">structural</see>
        /// <see cref="SerializedMember"/> key, so a converter that only speaks
        /// <see cref="SerializedMember"/> has nothing to say about it. Note that a foreign payload MAY
        /// still carry a <see cref="DescriptiveKeys">descriptive</see> key - <c>name</c> / <c>typeName</c>
        /// prove nothing, which is exactly why they are excluded here.
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

        // The single decision point. KnownKeys / StructuralKeys / DescriptiveKeys above are used ONLY
        // to phrase the diagnostic message and to document the split; keep them in sync with these
        // predicates if the SerializedMember schema ever grows a key.
        // ⚠ A NEW key belongs in StructuralKeys only if it is unique to the SerializedMember shape.
        // If it is a word a foreign payload could plausibly use too, it is descriptive.
        static bool IsStructuralKey(string name)
            => name == SerializedMember.ValueName
            || name == nameof(SerializedMember.fields)
            || name == nameof(SerializedMember.props);

        static bool IsDescriptiveKey(string name)
            => name == nameof(SerializedMember.name)
            || name == nameof(SerializedMember.typeName);

        static bool IsKnownKey(string name)
            => IsStructuralKey(name) || IsDescriptiveKey(name);

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
                return new SerializedMemberShape(SerializedMemberShapeKind.Absent, EmptyKeys, 0, 0);

            if (value.Value.ValueKind != JsonValueKind.Object)
                return new SerializedMemberShape(SerializedMemberShapeKind.NotAnObject, EmptyKeys, 0, 0);

            var recognisedKeyCount = 0;
            var structuralKeyCount = 0;
            var unknown = default(List<string>);

            foreach (var property in value.Value.EnumerateObject())
            {
                if (IsStructuralKey(property.Name))
                {
                    recognisedKeyCount++;
                    structuralKeyCount++;
                }
                else if (IsDescriptiveKey(property.Name))
                {
                    recognisedKeyCount++;
                }
                else
                {
                    (unknown ??= new List<string>()).Add(property.Name);
                }
            }

            // An empty JSON object carries no unknown key, so it is a (vacuously valid) SerializedMember.
            // That is deliberate: `{}` is the exact shape ReflectorNet emits for a cascade member whose
            // data travels in 'fields'/'props'.
            if (unknown == null)
                return new SerializedMemberShape(SerializedMemberShapeKind.SerializedMember, EmptyKeys, recognisedKeyCount, structuralKeyCount);

            // ---- The boundary ----------------------------------------------------------------
            // Only a STRUCTURAL key proves the payload meant to be a SerializedMember. 'name' and
            // 'typeName' are ordinary English words that consumer object-reference shapes carry too
            // (Unity's ComponentRef writes 'typeName', its GameObjectRef writes 'name'), so a payload
            // holding only those plus unknown keys is declined QUIETLY as Foreign and the chain gets
            // to resolve it. Reading a descriptive key as evidence is the 5.3.3 defect.
            //
            // Nothing is swallowed by the narrower rule: a genuinely mistyped SerializedMember that
            // happens to carry no structural key still fails - just at the CHAIN END, where nobody
            // understood the payload, and with the same DescribeUnknownKeys() diagnostic.
            return structuralKeyCount == 0
                ? new SerializedMemberShape(SerializedMemberShapeKind.Foreign, unknown, recognisedKeyCount, structuralKeyCount)
                : new SerializedMemberShape(SerializedMemberShapeKind.Malformed, unknown, recognisedKeyCount, structuralKeyCount);
        }
    }
}
