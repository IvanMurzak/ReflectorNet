/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON objects and System.Reflection.ConstructorInfo.
    /// </summary>
    public class ConstructorInfoConverter : JsonConverter<ConstructorInfo>
    {
        static class Json
        {
            public const string DeclaringType = "declaringType";
            public const string Parameters = "parameters";
            public const string Type = "type";
        }

        public override ConstructorInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty(Json.DeclaringType, out var declaringTypeElement))
                throw new JsonException("ConstructorInfo JSON must contain 'declaringType'.");

            var typeName = declaringTypeElement.GetString();
            var parameterTypes = new List<Type>();

            if (root.TryGetProperty(Json.Parameters, out var parametersElement))
            {
                foreach (var param in parametersElement.EnumerateArray())
                {
                    var paramTypeName = param.GetProperty(Json.Type).GetString();
                    var paramType = TypeUtils.GetType(paramTypeName);
                    if (paramType != null)
                        parameterTypes.Add(paramType);
                }
            }

            var declaringType = TypeUtils.GetType(typeName);
            if (declaringType == null)
                throw new JsonException($"Could not find type: {typeName}");

            var constructor = declaringType.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: parameterTypes.ToArray(),
                modifiers: null
            );
            if (constructor == null)
                throw new JsonException($"Could not find constructor on type: {typeName} with specified parameters.");

            return constructor;
        }

        public override void Write(Utf8JsonWriter writer, ConstructorInfo? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStartObject();
            writer.WriteString(Json.DeclaringType, value.DeclaringType?.GetTypeId());

            writer.WritePropertyName(Json.Parameters);
            writer.WriteStartArray();
            foreach (var param in value.GetParameters())
            {
                writer.WriteStartObject();
                writer.WriteString(Json.Type, param.ParameterType.GetTypeId());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}
