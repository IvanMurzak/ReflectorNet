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
    /// JsonConverter that handles conversion between JSON numbers/strings and System.IntPtr.
    /// </summary>
    public class IntPtrJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType == typeof(IntPtr);
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (Nullable.GetUnderlyingType(typeToConvert) != null)
                    return null;

                return IntPtr.Zero;
            }

            long value;
            if (reader.TokenType == JsonTokenType.Number)
            {
                value = reader.GetInt64();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (!long.TryParse(stringValue, out value))
                    throw new JsonException($"Unable to parse '{stringValue}' as IntPtr.");
            }
            else
            {
                throw new JsonException($"Expected number or string token for IntPtr, but got {reader.TokenType}");
            }

            // Validate value fits in platform's IntPtr size to avoid overflow
            if (IntPtr.Size == 4)
            {
                if (value < int.MinValue || value > int.MaxValue)
                    throw new JsonException($"Value {value} is outside the range of IntPtr on this 32-bit platform.");

                return new IntPtr((int)value);
            }

            return new IntPtr(value);
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteNumberValue(((IntPtr)value).ToInt64());
        }
    }
}
