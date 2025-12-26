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
    /// JsonConverter that handles serialization and deserialization of JsonNode instances.
    /// </summary>
    public class JsonNodeConverter : JsonNodeJsonConverter<JsonNode>, IJsonSchemaConverter
    {
        public static JsonNode Schema => JsonArrayJsonConverter.JsonAnySchema;
        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + StaticId
        };

        public override JsonNode GetSchema() => Schema;
        public override JsonNode GetSchemaRef() => SchemaRef;

        protected override JsonNode? CreateJsonNode(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    return JsonObject.Create(element);
                case JsonValueKind.Array:
                    return JsonArray.Create(element);
                default:
                    return JsonValue.Create(element);
            }
        }
    }
}
