/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles serialization and deserialization of JsonArray objects.
    /// Supports schema generation for JSON arrays and conversion between JsonArray and JsonElement.
    /// </summary>
    public class JsonArrayJsonConverter : JsonNodeJsonConverter<JsonArray>, IJsonSchemaConverter
    {
        // Schema for any JSON value type
        public static readonly JsonNode JsonAnySchema = new JsonObject
        {
            [JsonSchema.AnyOf] = new JsonArray
            {
                new JsonObject { [JsonSchema.Type] = JsonSchema.Object, [JsonSchema.AdditionalProperties] = true },
                new JsonObject { [JsonSchema.Type] = JsonSchema.Array, [JsonSchema.Items] = new JsonObject() },
                new JsonObject { [JsonSchema.Type] = JsonSchema.String },
                new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                new JsonObject { [JsonSchema.Type] = JsonSchema.Boolean },
                new JsonObject { [JsonSchema.Type] = JsonSchema.Null }
            }
        };
        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Array,
            [JsonSchema.Items] = JsonAnySchema
        };
        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + StaticId
        };

        public override JsonNode GetSchema() => Schema;
        public override JsonNode GetSchemaRef() => SchemaRef;

        protected override JsonArray CreateJsonNode(JsonElement element)
        {
            return JsonArray.Create(element)!;
        }
    }
}