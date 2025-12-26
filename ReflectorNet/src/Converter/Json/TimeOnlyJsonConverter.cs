/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

#if NET6_0_OR_GREATER
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON strings and System.TimeOnly.
    /// </summary>
    public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
    {
        private const string Format = "HH:mm:ss.fffffff";

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                throw new JsonException("Cannot convert null value to TimeOnly.");
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"Expected string token for TimeOnly, but got {reader.TokenType}");

            var stringValue = reader.GetString();
            if (stringValue is null)
                throw new JsonException("Expected non-null string value for TimeOnly.");

            if (TimeOnly.TryParseExact(stringValue, Format, null, System.Globalization.DateTimeStyles.None, out var result))
                return result;

            throw new JsonException($"Invalid TimeOnly value '{stringValue}'. Expected format: '{Format}'.");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format));
        }
    }
}
#endif
