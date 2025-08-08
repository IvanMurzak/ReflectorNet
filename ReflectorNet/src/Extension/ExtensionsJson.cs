/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsJson
    {
        public static JsonElement ToJsonElement(this object data, Reflector? reflector, JsonSerializerOptions? options = null)
            => JsonSerializer.SerializeToElement(data, options ?? reflector?.JsonSerializerOptions);

        public static JsonElement? ToJsonElement(this JsonNode? node)
        {
            if (node == null)
                return null;

            // Convert JsonNode to JsonElement
            var jsonString = node.ToJsonString();

            // Parse the JSON string into a JsonElement
            using var document = JsonDocument.Parse(jsonString);
            return document.RootElement.Clone();
        }

        public static string ToJson(this object? value, Reflector? reflector, JsonSerializerOptions? options = null)
            => ToJson(
                value: value,
                defaultValue: Utils.JsonSerializer.EmptyJsonObject, // Use empty JSON object as default value
                reflector: reflector,
                options: options);

        public static string ToJsonOrEmptyJsonObject(this object? value, Reflector? reflector, JsonSerializerOptions? options = null)
            => ToJson(
                value: value,
                defaultValue: Utils.JsonSerializer.EmptyJsonObject,
                reflector: reflector,
                options: options);

        public static string ToJson(this object? value, string defaultValue, Reflector? reflector, JsonSerializerOptions? options = null)
        {
            if (value == null)
                return defaultValue;

            if (value is Utils.JsonSerializer)
                throw new ArgumentException("Cannot serialize JsonSerializer instance.", nameof(value));

            return JsonSerializer.Serialize(
                value: value,
                options: options ?? reflector?.JsonSerializerOptions ?? new JsonSerializerOptions());
        }
    }
}