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
    /// JsonConverter that handles conversion between JSON objects and System.Exception.
    /// Captures message, type, and stack trace for serialization.
    /// </summary>
    public class ExceptionJsonConverter : JsonConverter<Exception>
    {
        static class Json
        {
            public const string Message = "message";
            public const string Type = "type";
            public const string StackTrace = "stackTrace";
            public const string InnerException = "innerException";
        }

        public override Exception? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var message = root.TryGetProperty(Json.Message, out var msgEl) ? msgEl.GetString() : string.Empty;
            var typeName = root.TryGetProperty(Json.Type, out var typeEl) ? typeEl.GetString() : typeof(Exception).FullName;

            Exception? innerException = null;
            if (root.TryGetProperty(Json.InnerException, out var innerEl) && innerEl.ValueKind != JsonValueKind.Null)
            {
                innerException = System.Text.Json.JsonSerializer.Deserialize<Exception>(innerEl.GetRawText(), options);
            }

            var type = TypeUtils.GetType(typeName);
            if (type != null && typeof(Exception).IsAssignableFrom(type))
            {
                try
                {
                    return (Exception?)Activator.CreateInstance(type, message, innerException)
                           ?? new Exception(message, innerException);
                }
                catch
                {
                    return new Exception(message, innerException);
                }
            }

            return new Exception(message, innerException);
        }

        public override void Write(Utf8JsonWriter writer, Exception? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStartObject();
            writer.WriteString(Json.Type, value.GetType().GetTypeId());
            writer.WriteString(Json.Message, value.Message);
            writer.WriteString(Json.StackTrace, value.StackTrace);

            if (value.InnerException != null)
            {
                writer.WritePropertyName(Json.InnerException);
                System.Text.Json.JsonSerializer.Serialize(writer, value.InnerException, options);
            }

            writer.WriteEndObject();
        }
    }
}
