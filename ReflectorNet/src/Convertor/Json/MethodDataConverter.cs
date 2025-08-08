/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    using JsonSerializer = System.Text.Json.JsonSerializer;

    public class MethodDataConverter : JsonConverter<MethodData>, IJsonSchemaConverter
    {
        public static string StaticId => TypeUtils.GetTypeId<MethodData>();

        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                [nameof(MethodData.IsPublic)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Boolean,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(MethodData)
                        .GetMember(nameof(MethodData.IsPublic))
                        .First())
                },
                [nameof(MethodData.IsStatic)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Boolean,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(MethodData)
                        .GetMember(nameof(MethodData.IsStatic))
                        .First())
                },
                [nameof(MethodData.ReturnType)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.String,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(MethodData)
                        .GetMember(nameof(MethodData.ReturnType))
                        .First())
                },
                [nameof(MethodData.ReturnSchema)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Object,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(MethodData)
                        .GetMember(nameof(MethodData.ReturnSchema))
                        .First())
                },
                [nameof(MethodData.InputParametersSchema)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Array,
                    [JsonSchema.Items] = new JsonObject
                    {
                        [JsonSchema.Type] = JsonSchema.Object,
                        [JsonSchema.AdditionalProperties] = true
                    },
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(MethodData)
                        .GetMember(nameof(MethodData.InputParametersSchema))
                        .First())
                },
                [nameof(MethodData.Namespace)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.String,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(MethodData)
                        .GetMember(nameof(MethodData.Namespace))
                        .First())
                },
                [nameof(MethodData.TypeName)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.String,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(MethodData)
                        .GetMember(nameof(MethodData.TypeName))
                        .First())
                },
                [nameof(MethodData.MethodName)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.String,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(MethodData)
                        .GetMember(nameof(MethodData.MethodName))
                        .First())
                },
                [nameof(MethodData.InputParameters)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Array,
                    [JsonSchema.Items] = new JsonObject
                    {
                        [JsonSchema.Ref] = JsonSchema.RefValue + TypeUtils.GetTypeId<MethodData.Parameter>()
                    },
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(MethodData)
                        .GetMember(nameof(MethodData.InputParameters))
                        .First())
                }
            },
            [JsonSchema.AdditionalProperties] = false
        };
        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + StaticId
        };

        public string Id => StaticId;

        public JsonNode GetSchemeRef() => SchemaRef;
        public JsonNode GetScheme() => Schema;

        public override MethodData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected start of object, but got {reader.TokenType}");

            var member = new MethodData();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return member;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read(); // Move to the value token

                    switch (propertyName)
                    {
                        case nameof(MethodData.IsPublic):
                            member.IsPublic = reader.GetBoolean();
                            break;
                        case nameof(MethodData.IsStatic):
                            member.IsStatic = reader.GetBoolean();
                            break;
                        case nameof(MethodData.ReturnType):
                            member.ReturnType = reader.GetString();
                            break;
                        case nameof(MethodData.ReturnSchema):
                            member.ReturnSchema = reader.TokenType != JsonTokenType.Null
                                ? JsonNode.Parse(ref reader)
                                : null;
                            break;
                        case nameof(MethodData.InputParametersSchema):
                            member.InputParametersSchema = JsonSerializer.Deserialize<List<JsonNode>>(ref reader, options);
                            break;
                        case nameof(MethodData.Namespace):
                            member.Namespace = reader.GetString();
                            break;
                        case nameof(MethodData.TypeName):
                            member.TypeName = reader.GetString() ?? throw new JsonException($"'{nameof(MethodData.TypeName)}' cannot be null.");
                            break;
                        case nameof(MethodData.MethodName):
                            member.MethodName = reader.GetString() ?? throw new JsonException($"'{nameof(MethodData.MethodName)}' cannot be null.");
                            break;
                        case nameof(MethodData.InputParameters):
                            member.InputParameters = JsonSerializer.Deserialize<List<MethodRef.Parameter>>(ref reader, options);
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: '{propertyName}'. "
                                + $"Did you want to use '{nameof(MethodData.IsPublic)}', '{nameof(MethodData.InputParameters)}', '{nameof(MethodData.ReturnType)}', "
                                + $"'{nameof(MethodData.ReturnSchema)}', '{nameof(MethodData.InputParametersSchema)}', '{nameof(MethodData.Namespace)}, "
                                + $"'{nameof(MethodData.TypeName)} or '{nameof(MethodData.InputParameters)}'?");
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON while reading MethodData.");
        }

        public override void Write(Utf8JsonWriter writer, MethodData? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteBoolean(nameof(MethodData.IsPublic), value.IsPublic);
            writer.WriteBoolean(nameof(MethodData.IsStatic), value.IsStatic);
            writer.WriteString(nameof(MethodData.ReturnType), value.ReturnType);
            if (value.ReturnSchema != null)
            {
                writer.WritePropertyName(nameof(MethodData.ReturnSchema));
                value.ReturnSchema.WriteTo(writer, options);
            }
            if (value.InputParametersSchema != null)
            {
                writer.WritePropertyName(nameof(MethodData.InputParametersSchema));
                JsonSerializer.Serialize(writer, value.InputParametersSchema, options);
            }
            writer.WriteString(nameof(MethodData.Namespace), value.Namespace);
            writer.WriteString(nameof(MethodData.TypeName), value.TypeName);
            writer.WriteString(nameof(MethodData.MethodName), value.MethodName);
            if (value.InputParameters != null)
            {
                writer.WritePropertyName(nameof(MethodData.InputParameters));
                JsonSerializer.Serialize(writer, value.InputParameters, options);
            }

            writer.WriteEndObject();
        }
    }
}