/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion from JSON string and number values to DateTime type.
    /// Supports nullable DateTime types and uses ISO 8601 format for writing.
    /// Number values are treated as Unix time in milliseconds (UTC).
    /// </summary>
    public class DateTimeJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType == typeof(DateTime);
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Handle null values for nullable types
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (Nullable.GetUnderlyingType(typeToConvert) != null)
                    return null;

                throw new JsonException($"Cannot convert null to non-nullable type '{typeToConvert.GetTypeId()}'.");
            }

            // Handle numeric timestamps (Unix time in milliseconds)
            if (reader.TokenType == JsonTokenType.Number)
            {
                var unixTimeMilliseconds = reader.GetInt64();
                return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMilliseconds).DateTime;
            }

            // Handle string tokens
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (stringValue == null)
                {
                    if (Nullable.GetUnderlyingType(typeToConvert) != null)
                        return null;

                    throw new JsonException($"Cannot convert null string to non-nullable type '{typeToConvert.GetTypeId()}'.");
                }

                if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeResult))
                    return dateTimeResult;

                throw new JsonException($"Unable to convert '{stringValue}' to {typeof(DateTime).GetTypeId()}.");
            }

            throw new JsonException($"Expected string or number token but got {reader.TokenType} for type '{typeToConvert.GetTypeId()}'");
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(((DateTime)value).ToString("o", CultureInfo.InvariantCulture));
        }
    }
}