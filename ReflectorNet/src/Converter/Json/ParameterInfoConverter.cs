/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON objects and System.Reflection.ParameterInfo.
    /// </summary>
    public class ParameterInfoConverter : JsonConverter<ParameterInfo>
    {
        static class Json
        {
            public const string Name = "name";
            public const string Member = "member";
            public const string MemberType = "memberType";
        }

        public override ParameterInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty(Json.Name, out var nameElement) ||
                !root.TryGetProperty(Json.Member, out var memberElement))
            {
                throw new JsonException("ParameterInfo JSON must contain 'name' and 'member'.");
            }

            var paramName = nameElement.GetString();
            var memberJson = memberElement.GetRawText();

            MethodBase? methodBase = null;
            if (root.TryGetProperty(Json.MemberType, out var memberTypeElement))
            {
                var memberType = memberTypeElement.GetString();
                if (memberType == nameof(MethodInfo))
                    methodBase = System.Text.Json.JsonSerializer.Deserialize<MethodInfo>(memberJson, options);
                else if (memberType == nameof(ConstructorInfo))
                    methodBase = System.Text.Json.JsonSerializer.Deserialize<ConstructorInfo>(memberJson, options);
            }

            if (methodBase == null)
                throw new JsonException("Could not resolve member for ParameterInfo.");

            var parameter = methodBase.GetParameters().FirstOrDefault(p => p.Name == paramName);
            if (parameter == null)
                throw new JsonException($"Could not find parameter: {paramName} on member: {methodBase.Name}");

            return parameter;
        }

        public override void Write(Utf8JsonWriter writer, ParameterInfo value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStartObject();
            writer.WriteString(Json.Name, value.Name);
            writer.WriteString(Json.MemberType, value.Member.GetType().Name);
            writer.WritePropertyName(Json.Member);

            if (value.Member is MethodInfo methodInfo)
                System.Text.Json.JsonSerializer.Serialize(writer, methodInfo, options);
            else if (value.Member is ConstructorInfo constructorInfo)
                System.Text.Json.JsonSerializer.Serialize(writer, constructorInfo, options);
            else
                writer.WriteNullValue();

            writer.WriteEndObject();
        }
    }
}
