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
    /// JsonConverter that handles conversion from JSON string and number values to double types.
    /// Supports nullable double types and provides comprehensive validation.
    /// </summary>
    public class DoubleJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType == typeof(double);
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
                return reader.GetDouble();
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

                return ParseDouble(stringValue);
            }

            throw new JsonException($"Expected string or number token but got {reader.TokenType} for type {typeToConvert.GetTypeShortName()}");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((double)value);
        }

        private static double ParseDouble(string stringValue)
        {
            if (double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(double).GetTypeShortName()}.");
        }
    }
}