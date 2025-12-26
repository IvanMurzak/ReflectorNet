/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON strings and System.Uri.
    /// </summary>
    public class UriJsonConverter : JsonConverter<Uri>
    {
        public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected string token for Uri, but got {reader.TokenType}");

            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
                return null;

            if (Uri.TryCreate(stringValue, UriKind.RelativeOrAbsolute, out var uri))
                return uri;

            throw new JsonException($"Unable to parse '{stringValue}' as a Uri.");
        }

        public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.OriginalString);
        }
    }
}
