/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON numbers/strings and System.UIntPtr.
    /// </summary>
    public class UIntPtrJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType == typeof(UIntPtr);
        }

        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (Nullable.GetUnderlyingType(typeToConvert) != null)
                    return null;

                return UIntPtr.Zero;
            }

            ulong value;
            if (reader.TokenType == JsonTokenType.Number)
            {
                value = reader.GetUInt64();
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (!ulong.TryParse(stringValue, out value))
                    throw new JsonException($"Unable to parse '{stringValue}' as UIntPtr.");
            }
            else
            {
                throw new JsonException($"Expected number or string token for UIntPtr, but got {reader.TokenType}");
            }

            // Validate value fits in platform's UIntPtr size to avoid overflow
            if (UIntPtr.Size == 4)
            {
                if (value > uint.MaxValue)
                    throw new JsonException($"Value {value} is outside the range of UIntPtr on this 32-bit platform.");

                return new UIntPtr((uint)value);
            }

            return new UIntPtr(value);
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteNumberValue(((UIntPtr)value).ToUInt64());
        }
    }
}
