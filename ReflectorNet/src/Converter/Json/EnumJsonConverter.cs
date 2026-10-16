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
    /// JsonConverter that handles conversion from JSON string values to enum types.
    /// Supports case-insensitive string matching and nullable enum types.
    /// </summary>
    public class EnumJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
            return underlyingType.IsEnum;
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

            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

            // Handle number tokens (enum values as numbers)
            if (reader.TokenType == JsonTokenType.Number)
            {
                var numericValue = reader.GetInt64();
                var underlyingEnumType = Enum.GetUnderlyingType(underlyingType);
                var convertedValue = Convert.ChangeType(numericValue, underlyingEnumType);
                if (Enum.IsDefined(underlyingType, convertedValue) || IsFlagsCombination(underlyingType, numericValue))
                    return Enum.ToObject(underlyingType, convertedValue);

                throw new JsonException($"Value '{numericValue}' is not defined for enum {underlyingType.Name}. Valid values are: {string.Join(", ", Enum.GetNames(underlyingType))}");
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

                if (!Enum.TryParse(underlyingType, stringValue, ignoreCase: true, out var enumValue))
                    throw new JsonException($"Unable to convert '{stringValue}' to enum {underlyingType.Name}. Valid values are: {string.Join(", ", Enum.GetNames(underlyingType))}");

                if (Enum.IsDefined(underlyingType, enumValue!))
                    return enumValue!;

                throw new JsonException($"Unable to convert '{stringValue}' to enum {underlyingType.Name}. Valid values are: {string.Join(", ", Enum.GetNames(underlyingType))}");
            }

            throw new JsonException($"Expected string or number token but got {reader.TokenType} for enum type '{typeToConvert.GetTypeId()}'");
        }

        /// <summary>
        /// A [Flags] enum value such as 3 (A | B) is not itself defined but is valid when every set bit
        /// belongs to a defined member.
        /// </summary>
        static bool IsFlagsCombination(Type enumType, long value)
        {
            if (!enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
                return false;

            var isUnsigned = Enum.GetUnderlyingType(enumType) == typeof(ulong);
            ulong definedBits = 0;
            foreach (var member in Enum.GetValues(enumType))
                definedBits |= isUnsigned ? Convert.ToUInt64(member) : unchecked((ulong)Convert.ToInt64(member));

            var bits = unchecked((ulong)value);
            return (bits & ~definedBits) == 0;
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.ToString());
        }
    }
}