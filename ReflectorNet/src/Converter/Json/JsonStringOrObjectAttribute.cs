/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// Opt-in marker for a <see cref="string"/> parameter whose value carries a "stringified JSON blob"
    /// (e.g. a <c>jsonPatch</c> argument). By default a <see cref="string"/> parameter emits a flat
    /// <c>{"type":"string"}</c> input schema, so an LLM that sends the value as a raw JSON object/array
    /// (instead of a JSON string) is rejected up front by schema validation.
    ///
    /// When this attribute is applied to a <see cref="string"/> parameter, the generated schema for that
    /// parameter becomes an <c>anyOf</c> of string + object so both forms are accepted transparently:
    /// <code>{"anyOf":[{"type":"string"},{"type":"object","additionalProperties":true}]}</code>
    ///
    /// This is strictly opt-in: an un-annotated <see cref="string"/> parameter keeps emitting
    /// <c>{"type":"string"}</c>. The complementary binder coercion (object/array -&gt; raw JSON text) in
    /// <see cref="MethodWrapper"/> is unconditional and does not require this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class JsonStringOrObjectAttribute : Attribute
    {
        /// <summary>
        /// The schema emitted for a <see cref="string"/> parameter annotated with this attribute:
        /// an <c>anyOf</c> accepting either a JSON string or a raw JSON object. Mirrors the
        /// <c>JsonArrayJsonConverter.JsonAnySchema</c> pattern.
        /// </summary>
        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.AnyOf] = new JsonArray
            {
                new JsonObject { [JsonSchema.Type] = JsonSchema.String },
                new JsonObject { [JsonSchema.Type] = JsonSchema.Object, [JsonSchema.AdditionalProperties] = true }
            }
        };
    }
}
