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
    /// JsonConverter that handles conversion from JSON string and number values to long (Int64) types.
    /// Supports nullable long types and provides comprehensive range validation.
    /// </summary>
    public class Int64JsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType == typeof(long);
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

            // Handle direct number tokens
            if (reader.TokenType == JsonTokenType.Number)
            {
                var doubleValue = reader.GetDouble();
                return ConvertToInt64(doubleValue);
            }

            // Handle string tokens
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (stringValue == null)
                {
                    if (Nullable.GetUnderlyingType(typeToConvert) != null)
                        return null;

                    throw new JsonException($"Cannot convert null string to non-nullable type {typeToConvert.GetTypeShortName()}.");
                }

                return ParseInt64(stringValue);
            }

            throw new JsonException($"Expected string or number token but got {reader.TokenType} for type {typeToConvert.GetTypeShortName()}");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((long)value);
        }

        private static long ParseInt64(string stringValue)
        {
            if (long.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(long).GetTypeShortName()}.");
        }

        private static long ConvertToInt64(double value)
        {
            if (value >= long.MinValue && value <= long.MaxValue && value == Math.Floor(value))
                return (long)value;
            throw new JsonException($"Value {value} is out of range for {typeof(long).GetTypeShortName()}.");
        }
    }
}