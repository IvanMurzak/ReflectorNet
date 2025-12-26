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
    /// JsonConverter that handles conversion between JSON objects and System.Numerics.Complex.
    /// </summary>
    public class ComplexJsonConverter : JsonSchemaConverter<Complex>, IJsonSchemaConverter
    {
        private const string RealProperty = "real";
        private const string ImaginaryProperty = "imaginary";

        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                [RealProperty] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number },
                [ImaginaryProperty] = new JsonObject { [JsonSchema.Type] = JsonSchema.Number }
            },
            [JsonSchema.Required] = new JsonArray { RealProperty, ImaginaryProperty }
        };

        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + StaticId
        };

        public override JsonNode GetSchemaRef() => SchemaRef;
        public override JsonNode GetSchema() => Schema;

        public override Complex Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                throw new JsonException("Cannot convert null to Complex.");
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject for Complex.");

            var real = 0d;
            var imaginary = 0d;
            var realSet = false;
            var imaginarySet = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    if (propertyName == null)
                    {
                        reader.Skip();
                        continue;
                    }

                    if (string.Equals(propertyName, RealProperty, StringComparison.OrdinalIgnoreCase))
                    {
                        real = reader.GetDouble();
                        realSet = true;
                    }
                    else if (string.Equals(propertyName, ImaginaryProperty, StringComparison.OrdinalIgnoreCase))
                    {
                        imaginary = reader.GetDouble();
                        imaginarySet = true;
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }

            if (!realSet || !imaginarySet)
                throw new JsonException("Complex number requires both 'real' and 'imaginary' properties.");

            return new Complex(real, imaginary);
        }

        public override void Write(Utf8JsonWriter writer, Complex value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(RealProperty, value.Real);
            writer.WriteNumber(ImaginaryProperty, value.Imaginary);
            writer.WriteEndObject();
        }
    }
}
