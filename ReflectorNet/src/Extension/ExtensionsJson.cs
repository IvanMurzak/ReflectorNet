/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.ReflectorNet
{
    public static class ExtensionsJson
    {
        public static JsonElement ToJsonElement(this object data, Reflector? reflector, JsonSerializerOptions? options = null, ILogger? logger = null)
        {
            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace("Converting object of type {Type} to JsonElement.",
                    data?.GetType().GetTypeId().ValueOrNull());

            return JsonSerializer.SerializeToElement(data, options ?? reflector?.JsonSerializerOptions);
        }

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

        public static string ToJson(this object? value, Reflector? reflector, JsonSerializerOptions? options = null, ILogger? logger = null)
        {
            return ToJson(
                value: value,
                defaultValue: Utils.JsonSerializer.EmptyJsonObject, // Use empty JSON object as default value
                reflector: reflector,
                options: options,
                logger: logger);
        }

        public static string ToJson(this object? value, string defaultValue, Reflector? reflector, JsonSerializerOptions? options = null, ILogger? logger = null)
        {
            if (value == null)
                return defaultValue;

            if (value is Utils.JsonSerializer)
                throw new ArgumentException("Cannot serialize JsonSerializer instance.", nameof(value));

            if (logger?.IsEnabled(LogLevel.Trace) == true)
                logger.LogTrace("Serializing object of type {Type} to JSON string.",
                    value.GetType().GetTypeId().ValueOrNull());

            return JsonSerializer.Serialize(
                value: value,
                options: options ?? reflector?.JsonSerializerOptions);
        }
    }
}