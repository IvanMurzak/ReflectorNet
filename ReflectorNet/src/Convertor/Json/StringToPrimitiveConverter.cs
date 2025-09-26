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
    /// JsonConverter that allows conversion from JSON string values to primitive types.
    /// This converter enables deserialization of primitive values that are represented as strings in JSON,
    /// which is useful for scenarios where data comes from sources that stringify all values.
    ///
    /// Supported types:
    /// - Boolean (case-insensitive "true"/"false")
    /// - All integer types (byte, sbyte, short, ushort, int, uint, long, ulong)
    /// - Floating point types (float, double, decimal)
    /// - DateTime and DateTimeOffset
    /// - TimeSpan
    /// - Guid
    /// - Enums (case-insensitive string matching)
    /// - Nullable versions of all above types
    /// </summary>
    public class StringToPrimitiveConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

            if (underlyingType.IsEnum)
                return true;

            return underlyingType.IsPrimitive ||
                underlyingType == typeof(decimal) ||
                underlyingType == typeof(DateTime) ||
                underlyingType == typeof(DateTimeOffset) ||
                underlyingType == typeof(TimeSpan) ||
                underlyingType == typeof(Guid);
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

            if (reader.TokenType == JsonTokenType.True && (typeToConvert == typeof(bool) || typeToConvert == typeof(bool?)))
                return true;

            if (reader.TokenType == JsonTokenType.False && (typeToConvert == typeof(bool) || typeToConvert == typeof(bool?)))
                return false;

            if (reader.TokenType == JsonTokenType.Number)
            {
                var underlyingType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

                switch (underlyingType)
                {
                    case Type t when t == typeof(bool):
                        return reader.GetBoolean();
                    case Type t when t == typeof(byte):
                        return reader.GetByte();
                    case Type t when t == typeof(sbyte):
                        return reader.GetSByte();
                    case Type t when t == typeof(short):
                        return reader.GetInt16();
                    case Type t when t == typeof(ushort):
                        return reader.GetUInt16();
                    case Type t when t == typeof(int):
                        return reader.GetInt32();
                    case Type t when t == typeof(uint):
                        return reader.GetUInt32();
                    case Type t when t == typeof(long):
                        return reader.GetInt64();
                    case Type t when t == typeof(ulong):
                        return reader.GetUInt64();
                    case Type t when t == typeof(float):
                        return reader.GetSingle();
                    case Type t when t == typeof(double):
                        return reader.GetDouble();
                    case Type t when t == typeof(decimal):
                        return reader.GetDecimal();
                    case Type t when t == typeof(DateTime):
                        return reader.GetDateTime();
                    case Type t when t == typeof(DateTimeOffset):
                        return reader.GetDateTimeOffset();
                    case Type t when t == typeof(TimeSpan):
                        return new TimeSpan(reader.GetInt64());
                    case Type t when t == typeof(Guid):
                        return reader.GetGuid();
                    default:
                        throw new JsonException($"Unsupported number type: {typeToConvert.GetTypeShortName()}");
                }
            }

            // If it's not a string, throw since this converter only handles string-to-primitive conversion
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string token but got {reader.TokenType} for type {typeToConvert.GetTypeShortName()}");
            }

            var stringValue = reader.GetString();
            if (stringValue == null)
            {
                if (Nullable.GetUnderlyingType(typeToConvert) != null)
                    return null;

                throw new JsonException($"Cannot convert null string to non-nullable type {typeToConvert.GetTypeShortName()}.");
            }

            return ConvertStringToPrimitive(stringValue, typeToConvert);
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            // Write primitive values directly to avoid infinite recursion
            var type = value.GetType();
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            switch (underlyingType)
            {
                case Type t when t == typeof(bool):
                    writer.WriteBooleanValue((bool)value);
                    break;
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
                case Type t when t == typeof(decimal):
                    writer.WriteNumberValue((decimal)value);
                    break;
                case Type t when t == typeof(DateTime):
                    writer.WriteStringValue(((DateTime)value).ToString("O", CultureInfo.InvariantCulture));
                    break;
                case Type t when t == typeof(DateTimeOffset):
                    writer.WriteStringValue(((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture));
                    break;
                case Type t when t == typeof(TimeSpan):
                    writer.WriteStringValue(((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture));
                    break;
                case Type t when t == typeof(Guid):
                    writer.WriteStringValue(((Guid)value).ToString());
                    break;
                case Type t when t.IsEnum:
                    writer.WriteStringValue(value.ToString());
                    break;
                default:
                    throw new JsonException($"Not supported type for writing: {type.GetTypeShortName()}");
            }
        }

        private static object ConvertStringToPrimitive(string stringValue, Type targetType)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                switch (underlyingType)
                {
                    case Type t when t == typeof(bool):
                        if (bool.TryParse(stringValue, out var boolResult))
                            return boolResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(bool).GetTypeShortName()}.");

                    case Type t when t == typeof(byte):
                        if (byte.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var byteResult))
                            return byteResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(byte).GetTypeShortName()}.");

                    case Type t when t == typeof(sbyte):
                        if (sbyte.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sbyteResult))
                            return sbyteResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(sbyte).GetTypeShortName()}.");

                    case Type t when t == typeof(short):
                        if (short.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var shortResult))
                            return shortResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(short).GetTypeShortName()}.");

                    case Type t when t == typeof(ushort):
                        if (ushort.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ushortResult))
                            return ushortResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(ushort).GetTypeShortName()}.");

                    case Type t when t == typeof(int):
                        if (int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intResult))
                            return intResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(int).GetTypeShortName()}.");

                    case Type t when t == typeof(uint):
                        if (uint.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uintResult))
                            return uintResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(uint).GetTypeShortName()}.");

                    case Type t when t == typeof(long):
                        if (long.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longResult))
                            return longResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(long).GetTypeShortName()}.");

                    case Type t when t == typeof(ulong):
                        if (ulong.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongResult))
                            return ulongResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(ulong).GetTypeShortName()}.");

                    case Type t when t == typeof(float):
                        if (float.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatResult))
                            return floatResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(float).GetTypeShortName()}.");

                    case Type t when t == typeof(double):
                        if (double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleResult))
                            return doubleResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(double).GetTypeShortName()}.");

                    case Type t when t == typeof(decimal):
                        if (decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalResult))
                            return decimalResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(decimal).GetTypeShortName()}.");

                    case Type t when t == typeof(DateTime):
                        if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeResult))
                            return dateTimeResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(DateTime).GetTypeShortName()}.");

                    case Type t when t == typeof(DateTimeOffset):
                        if (DateTimeOffset.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeOffsetResult))
                            return dateTimeOffsetResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(DateTimeOffset).GetTypeShortName()}.");

                    case Type t when t == typeof(TimeSpan):
                        if (TimeSpan.TryParse(stringValue, CultureInfo.InvariantCulture, out var timeSpanResult))
                            return timeSpanResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(TimeSpan).GetTypeShortName()}.");

                    case Type t when t == typeof(Guid):
                        if (Guid.TryParse(stringValue, out var guidResult))
                            return guidResult;
                        throw new JsonException($"Unable to convert '{stringValue}' to {typeof(Guid).GetTypeShortName()}.");

                    case Type t when t.IsEnum:
                        if (!Enum.TryParse(underlyingType, stringValue, ignoreCase: true, out var enumValue))
                            throw new JsonException($"Unable to convert '{stringValue}' to enum {underlyingType.Name}. Valid values are: {string.Join(", ", Enum.GetNames(underlyingType))}");

                        if (Enum.IsDefined(underlyingType, enumValue))
                            return enumValue;

                        throw new JsonException($"Unable to convert '{stringValue}' to enum {underlyingType.Name}. Valid values are: {string.Join(", ", Enum.GetNames(underlyingType))}");

                    default:
                        throw new JsonException($"Not supported target type: {targetType.GetTypeShortName()}");
                }
            }
            catch (Exception ex) when (ex is not JsonException)
            {
                throw new JsonException($"Failed to convert '{stringValue}' to {targetType.GetTypeShortName()}: {ex.Message}", ex);
            }
        }
    }
}