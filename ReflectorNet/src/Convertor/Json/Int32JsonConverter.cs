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
    /// JsonConverter that handles conversion from JSON string and number values to int (Int32) types.
    /// Supports nullable int types and provides comprehensive range validation.
    /// </summary>
    public class Int32JsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType == typeof(int);
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Handle null values for nullable types
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (Nullable.GetUnderlyingType(typeToConvert) != null)
                    return null;

                throw new JsonException($"Cannot convert null to non-nullable type {typeToConvert.GetTypeName(pretty: true)}.");
            }

            // Handle direct number tokens
            if (reader.TokenType == JsonTokenType.Number)
            {
                var doubleValue = reader.GetDouble();
                return ConvertToInt32(doubleValue);
            }

            // Handle string tokens
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (stringValue == null)
                {
                    if (Nullable.GetUnderlyingType(typeToConvert) != null)
                        return null;

                    throw new JsonException($"Cannot convert null string to non-nullable type {typeToConvert.GetTypeName(pretty: true)}.");
                }

                return ParseInt32(stringValue);
            }

            throw new JsonException($"Expected string or number token but got {reader.TokenType} for type {typeToConvert.GetTypeName(pretty: true)}");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }

        private static int ParseInt32(string stringValue)
        {
            if (int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(int).GetTypeName(pretty: true)}.");
        }

        private static int ConvertToInt32(double value)
        {
            if (value >= int.MinValue && value <= int.MaxValue && value == Math.Floor(value))
                return (int)value;
            throw new JsonException($"Value {value} is out of range for {typeof(int).GetTypeName(pretty: true)}.");
        }
    }
}