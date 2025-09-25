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
    /// JsonConverter that handles conversion from JSON string values to numeric primitive types.
    /// Supports all integer and floating point types (excludes bool).
    ///
    /// Supported types:
    /// - All integer types (byte, sbyte, short, ushort, int, uint, long, ulong)
    /// - Floating point types (float, double)
    /// - Nullable versions of all above types
    /// </summary>
    public class PrimitiveJsonConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

            // Check for numeric primitives but exclude bool
            return underlyingType.IsPrimitive && underlyingType != typeof(bool);
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

            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

            // Handle direct number tokens
            if (reader.TokenType == JsonTokenType.Number)
            {
                return underlyingType switch
                {
                    Type t when t == typeof(byte) => reader.GetByte(),
                    Type t when t == typeof(sbyte) => reader.GetSByte(),
                    Type t when t == typeof(short) => reader.GetInt16(),
                    Type t when t == typeof(ushort) => reader.GetUInt16(),
                    Type t when t == typeof(int) => reader.GetInt32(),
                    Type t when t == typeof(uint) => reader.GetUInt32(),
                    Type t when t == typeof(long) => reader.GetInt64(),
                    Type t when t == typeof(ulong) => reader.GetUInt64(),
                    Type t when t == typeof(float) => reader.GetSingle(),
                    Type t when t == typeof(double) => reader.GetDouble(),
                    _ => throw new JsonException($"Unsupported numeric primitive type: {typeToConvert.GetTypeShortName()}")
                };
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

                return ConvertStringToPrimitive(stringValue, underlyingType);
            }

            throw new JsonException($"Expected string or number token but got {reader.TokenType} for type {typeToConvert.GetTypeShortName()}");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            var type = value.GetType();
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            switch (underlyingType)
            {
                case Type t when t == typeof(byte):
                    writer.WriteNumberValue((byte)value);
                    break;
                case Type t when t == typeof(sbyte):
                    writer.WriteNumberValue((sbyte)value);
                    break;
                case Type t when t == typeof(short):
                    writer.WriteNumberValue((short)value);
                    break;
                case Type t when t == typeof(ushort):
                    writer.WriteNumberValue((ushort)value);
                    break;
                case Type t when t == typeof(int):
                    writer.WriteNumberValue((int)value);
                    break;
                case Type t when t == typeof(uint):
                    writer.WriteNumberValue((uint)value);
                    break;
                case Type t when t == typeof(long):
                    writer.WriteNumberValue((long)value);
                    break;
                case Type t when t == typeof(ulong):
                    writer.WriteNumberValue((ulong)value);
                    break;
                case Type t when t == typeof(float):
                    writer.WriteNumberValue((float)value);
                    break;
                case Type t when t == typeof(double):
                    writer.WriteNumberValue((double)value);
                    break;
                default:
                    throw new JsonException($"Not supported primitive type for writing: {type.GetTypeShortName()}");
            }
        }

        private static object ConvertStringToPrimitive(string stringValue, Type underlyingType)
        {
            try
            {
                return underlyingType switch
                {
                    Type t when t == typeof(byte) => ParseByte(stringValue),
                    Type t when t == typeof(sbyte) => ParseSByte(stringValue),
                    Type t when t == typeof(short) => ParseInt16(stringValue),
                    Type t when t == typeof(ushort) => ParseUInt16(stringValue),
                    Type t when t == typeof(int) => ParseInt32(stringValue),
                    Type t when t == typeof(uint) => ParseUInt32(stringValue),
                    Type t when t == typeof(long) => ParseInt64(stringValue),
                    Type t when t == typeof(ulong) => ParseUInt64(stringValue),
                    Type t when t == typeof(float) => ParseSingle(stringValue),
                    Type t when t == typeof(double) => ParseDouble(stringValue),
                    _ => throw new JsonException($"Not supported primitive type: {underlyingType.GetTypeShortName()}")
                };
            }
            catch (Exception ex) when (ex is not JsonException)
            {
                throw new JsonException($"Failed to convert '{stringValue}' to {underlyingType.GetTypeShortName()}: {ex.Message}", ex);
            }
        }

        private static byte ParseByte(string stringValue)
        {
            if (byte.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(byte).GetTypeShortName()}.");
        }

        private static sbyte ParseSByte(string stringValue)
        {
            if (sbyte.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(sbyte).GetTypeShortName()}.");
        }

        private static short ParseInt16(string stringValue)
        {
            if (short.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(short).GetTypeShortName()}.");
        }

        private static ushort ParseUInt16(string stringValue)
        {
            if (ushort.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(ushort).GetTypeShortName()}.");
        }

        private static int ParseInt32(string stringValue)
        {
            if (int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(int).GetTypeShortName()}.");
        }

        private static uint ParseUInt32(string stringValue)
        {
            if (uint.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(uint).GetTypeShortName()}.");
        }

        private static long ParseInt64(string stringValue)
        {
            if (long.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(long).GetTypeShortName()}.");
        }

        private static ulong ParseUInt64(string stringValue)
        {
            if (ulong.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(ulong).GetTypeShortName()}.");
        }

        private static float ParseSingle(string stringValue)
        {
            if (float.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(float).GetTypeShortName()}.");
        }

        private static double ParseDouble(string stringValue)
        {
            if (double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                return result;
            throw new JsonException($"Unable to convert '{stringValue}' to {typeof(double).GetTypeShortName()}.");
        }
    }
}