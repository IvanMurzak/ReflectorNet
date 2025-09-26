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
    /// JsonConverter that handles conversion from JSON string values to boolean types.
    /// Supports case-insensitive "true"/"false" string matching and nullable bool types.
    /// </summary>
    public class BoolJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType == typeof(bool);
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Handle null values for nullable types
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (Nullable.GetUnderlyingType(typeToConvert) != null)
                    return null;

                throw new JsonException($"Cannot convert null to non-nullable type {typeToConvert.GetTypeShortName()}.");
            }

            // Handle direct boolean tokens
            if (reader.TokenType == JsonTokenType.True)
                return true;

            if (reader.TokenType == JsonTokenType.False)
                return false;

            // Handle number tokens
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetBoolean();
            }

            // Handle string tokens
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (stringValue == null)
                {
                    if (Nullable.GetUnderlyingType(typeToConvert) != null)
                        return null;

                    throw new JsonException($"Cannot convert null string to non-nullable type {typeToConvert.GetTypeShortName()}.\nInput value: null");
                }

                if (bool.TryParse(stringValue, out var boolResult))
                    return boolResult;

                throw new JsonException($"Unable to convert '{stringValue}' to {typeof(bool).GetTypeShortName()}.\nInput value: {stringValue}");
            }

            throw new JsonException($"Expected string, boolean, or number token but got {reader.TokenType} for type {typeToConvert.GetTypeShortName()}.");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value != null
                ? (bool)value
                : false);
        }
    }
}