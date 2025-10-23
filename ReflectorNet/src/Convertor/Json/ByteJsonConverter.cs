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
    /// JsonConverter that handles conversion from JSON string and number values to byte types.
    /// Supports nullable byte types and provides comprehensive range validation.
    /// </summary>
    public class ByteJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType == typeof(byte);
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
                return ConvertToByte(doubleValue);
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

                return ParseByte(stringValue);
            }

            throw new JsonException($"Expected string or number token but got {reader.TokenType} for type {typeToConvert.GetTypeName(pretty: true)}");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((byte)value);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(((byte)value).ToString(CultureInfo.InvariantCulture));
        }

        public override object ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var propertyName = reader.GetString();
            if (propertyName == null)
                throw new JsonException($"Cannot convert null property name to {typeof(byte).GetTypeName(pretty: true)}.");
            return ParseByte(propertyName);
        }

        private static byte ParseByte(string stringValue)
        {
            if (byte.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(byte).GetTypeName(pretty: true)}.");
        }

        private static byte ConvertToByte(double value)
        {
            if (value >= byte.MinValue && value <= byte.MaxValue && value == Math.Floor(value))
                return (byte)value;
            throw new JsonException($"Value {value} is out of range for {typeof(byte).GetTypeName(pretty: true)}.");
        }
    }
}