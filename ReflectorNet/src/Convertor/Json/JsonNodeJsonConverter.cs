/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// Abstract base class for converting between JSON and types derived from JsonNode.
    /// Provides extensibility for custom JsonNode handling via the CreateJsonNode method.
    /// </summary>
    public abstract class JsonNodeJsonConverter<T> : JsonSchemaConverter<T>, IJsonSchemaConverter
        where T : JsonNode
    {
        protected abstract T CreateJsonNode(JsonElement element);
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Handle null values for nullable types
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (Nullable.GetUnderlyingType(typeToConvert) != null)
                    return null;

                throw new JsonException($"Cannot convert null to non-nullable type {typeToConvert.GetTypeName(pretty: true)}.");
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                using JsonDocument document = JsonDocument.ParseValue(ref reader);
                // Clone the element to break the dependency on the JsonDocument
                var clonedElement = document.RootElement.Clone();
                return CreateJsonNode(clonedElement);
            }

            throw new JsonException($"Expected Null, StartObject or StartArray token but got {reader.TokenType} for type {typeToConvert.GetTypeName(pretty: true)}");
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            value.WriteTo(writer, options);
        }
    }
}