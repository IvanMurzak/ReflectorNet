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
    /// JsonConverter that handles conversion from JSON string and number values to ulong (UInt64) types.
    /// Supports nullable ulong types and provides comprehensive range validation.
    /// </summary>
    public class UInt64JsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType == typeof(ulong);
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

            // Handle direct number tokens
            if (reader.TokenType == JsonTokenType.Number)
            {
                var doubleValue = reader.GetDouble();
                return ConvertToUInt64(doubleValue);
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

                return ParseUInt64(stringValue);
            }

            throw new JsonException($"Expected string or number token but got {reader.TokenType} for type '{typeToConvert.GetTypeId()}'");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteNumberValue((ulong)value);
        }

        private static ulong ParseUInt64(string stringValue)
        {
            if (ulong.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(ulong).GetTypeId()}.");
        }

        private static ulong ConvertToUInt64(double value)
        {
            if (value >= ulong.MinValue && value <= ulong.MaxValue && value == Math.Floor(value))
                return (ulong)value;
            throw new JsonException($"Value {value} is out of range for {typeof(ulong).GetTypeId()}.");
        }
    }
}