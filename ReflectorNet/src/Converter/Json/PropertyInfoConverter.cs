/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON objects and System.Reflection.PropertyInfo.
    /// </summary>
    public class PropertyInfoConverter : JsonConverter<PropertyInfo>
    {
        static class Json
        {
            public const string Name = "name";
            public const string DeclaringType = "declaringType";
        }

        public override PropertyInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty(Json.DeclaringType, out var declaringTypeElement) ||
                !root.TryGetProperty(Json.Name, out var nameElement))
            {
                throw new JsonException("PropertyInfo JSON must contain 'declaringType' and 'name'.");
            }

            var typeName = declaringTypeElement.GetString();
            var propertyName = nameElement.GetString();

            var declaringType = TypeUtils.GetType(typeName);
            if (declaringType == null)
                throw new JsonException($"Could not find type: {typeName}");

            var property = declaringType.GetProperty(
                name: propertyName!,
                bindingAttr: BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static
            );
            if (property == null)
                throw new JsonException($"Could not find property: {propertyName} on type: {typeName}");

            return property;
        }

        public override void Write(Utf8JsonWriter writer, PropertyInfo? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStartObject();
            writer.WriteString(Json.Name, value.Name);
            writer.WriteString(Json.DeclaringType, value.DeclaringType?.GetTypeId());
            writer.WriteEndObject();
        }
    }
}
