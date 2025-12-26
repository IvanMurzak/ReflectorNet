/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON string values and System.Net.IPAddress.
    /// </summary>
    public class IPAddressJsonConverter : JsonSchemaConverter<IPAddress>, IJsonSchemaConverter
    {
        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.String,
            ["format"] = "ipv4-or-ipv6"
        };

        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + StaticId
        };

        public override JsonNode GetSchemaRef() => SchemaRef;
        public override JsonNode GetSchema() => Schema;

        public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Expected string for IPAddress.");

            var stringValue = reader.GetString();
            if (IPAddress.TryParse(stringValue, out var address))
            {
                return address;
            }

            throw new JsonException($"Invalid IPAddress format: {stringValue}");
        }

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.ToString());
        }
    }
}
