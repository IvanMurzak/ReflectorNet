/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON values and System.Numerics.BigInteger.
    /// Supports reading from both string and number tokens.
    /// </summary>
    public class BigIntegerJsonConverter : JsonSchemaConverter<BigInteger>, IJsonSchemaConverter
    {
        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.String,
            ["description"] = "A large integer represented as a string"
        };

        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + StaticId
        };

        public override JsonNode GetSchemaRef() => SchemaRef;
        public override JsonNode GetSchema() => Schema;

        public override BigInteger Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (BigInteger.TryParse(stringValue, out var result))
                {
                    return result;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                if (BigInteger.TryParse(doc.RootElement.GetRawText(), out var result))
                {
                    return result;
                }
            }

            throw new JsonException($"Expected string or number representing BigInteger.");
        }

        public override void Write(Utf8JsonWriter writer, BigInteger value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
