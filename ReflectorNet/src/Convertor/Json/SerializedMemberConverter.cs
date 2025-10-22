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
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    public class SerializedMemberConverter : JsonSchemaConverter<SerializedMember>, IJsonSchemaConverter
    {
        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                [nameof(SerializedMember.typeName)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.String,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(SerializedMember)
                        .GetMember(nameof(SerializedMember.typeName))
                        .First())
                },
                [nameof(SerializedMember.name)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.String,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(SerializedMember)
                        .GetMember(nameof(SerializedMember.name))
                        .First())
                },
                [SerializedMember.ValueName] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Object,
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(SerializedMember)
                        .GetMember(nameof(SerializedMember.valueJsonElement))
                        .First())
                },
                [nameof(SerializedMember.fields)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Array,
                    [JsonSchema.Items] = new JsonObject
                    {
                        [JsonSchema.Ref] = JsonSchema.RefValue + StaticId,
                        [JsonSchema.Description] = "Nested field value."
                    },
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(SerializedMember)
                        .GetMember(nameof(SerializedMember.fields))
                        .First())
                },
                [nameof(SerializedMember.props)] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Array,
                    [JsonSchema.Items] = new JsonObject
                    {
                        [JsonSchema.Ref] = JsonSchema.RefValue + StaticId,
                        [JsonSchema.Description] = "Nested property value."
                    },
                    [JsonSchema.Description] = TypeUtils.GetDescription(
                        typeof(SerializedMember)
                        .GetMember(nameof(SerializedMember.props))
                        .First())
                }
            },
            [JsonSchema.Required] = new JsonArray { nameof(SerializedMember.typeName), SerializedMember.ValueName },
            [JsonSchema.AdditionalProperties] = false
        };
        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + StaticId
        };

        readonly Reflector _reflector;

        public SerializedMemberConverter(Reflector reflector)
        {
            _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
        }

        public override JsonNode GetSchemaRef() => SchemaRef;
        public override JsonNode GetSchema() => Schema;
        public override IEnumerable<Type> GetDefinedTypes()
        {
            yield return typeof(SerializedMemberList);
        }

        public override SerializedMember? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected start of object, but got {reader.TokenType}");

            var member = new SerializedMember();

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
                        case nameof(SerializedMember.name):
                            member.name = reader.GetString();
                            break;
                        case nameof(SerializedMember.typeName):
                            member.typeName = reader.GetString() ?? "[FAILED TO READ]";
                            break;
                        case SerializedMember.ValueName:
                            if (!JsonElement.TryParseValue(ref reader, out member.valueJsonElement))
                                throw new JsonException($"Failed to parse value for property '{SerializedMember.ValueName}'.");
                            break;
                        case nameof(SerializedMember.fields):
                            member.fields = _reflector.JsonSerializer.Deserialize<SerializedMemberList>(ref reader, options);
                            break;
                        case nameof(SerializedMember.props):
                            member.props = _reflector.JsonSerializer.Deserialize<SerializedMemberList>(ref reader, options);
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: '{propertyName}'. "
                                + $"Did you want to use '{nameof(SerializedMember.name)}', '{nameof(SerializedMember.typeName)}', '{SerializedMember.ValueName}', '{nameof(SerializedMember.fields)}' or '{nameof(SerializedMember.props)}'?");
                    }
                }
            }

            throw new JsonException("Unexpected end of JSON while reading SerializedMember.");
        }

        public override void Write(Utf8JsonWriter writer, SerializedMember value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            writer.WriteString(nameof(SerializedMember.name), value.name);
            writer.WriteString(nameof(SerializedMember.typeName), value.typeName);

            if (value.valueJsonElement.HasValue)
            {
                writer.WritePropertyName(SerializedMember.ValueName);
                value.valueJsonElement.Value.WriteTo(writer);
            }
            if (value.fields != null && value.fields.Count > 0)
            {
                writer.WritePropertyName(nameof(SerializedMember.fields));
                System.Text.Json.JsonSerializer.Serialize(writer, value.fields, options);
            }
            if (value.props != null && value.props.Count > 0)
            {
                writer.WritePropertyName(nameof(SerializedMember.props));
                System.Text.Json.JsonSerializer.Serialize(writer, value.props, options);
            }

            writer.WriteEndObject();
        }
    }
}