#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Data.Unity;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    public class SerializedMemberConverter : JsonConverter<SerializedMember>, IJsonSchemaConverter
    {
        public static string StaticId => typeof(SerializedMember).FullName;
        public static JsonNode Schema => new JsonObject
        {
            ["id"] = StaticId,
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [nameof(SerializedMember.typeName)] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "Full type name. Eg: 'System.String', 'System.Int32', 'UnityEngine.Vector3', etc."
                },
                [nameof(SerializedMember.name)] = new JsonObject { ["type"] = "string" },
                [SerializedMember.ValueName] = new JsonObject { ["type"] = "object" },
                [nameof(SerializedMember.fields)] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["$ref"] = StaticId
                    }
                },
                [nameof(SerializedMember.props)] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["$ref"] = StaticId
                    }
                }
            },
            ["required"] = new JsonArray { nameof(SerializedMember.typeName), SerializedMember.ValueName }
        };
        public static JsonNode SchemaRef => new JsonObject
        {
            ["$ref"] = StaticId
        };

        public string Id => StaticId;
        public JsonNode GetSchemeRef() => SchemaRef;
        public JsonNode GetScheme() => Schema;

        public override SerializedMember? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var member = new SerializedMember();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read(); // Move to the value token

                    switch (propertyName)
                    {
                        case nameof(SerializedMember.name):
                            member.name = reader.GetString() ?? "[FAILED TO READ]";
                            break;
                        case nameof(SerializedMember.typeName):
                            member.typeName = reader.GetString() ?? "[FAILED TO READ]";
                            break;
                        case SerializedMember.ValueName:
                            member.valueJsonElement = JsonElement.ParseValue(ref reader);
                            break;
                        case nameof(SerializedMember.fields):
                            member.fields = JsonUtils.Deserialize<List<SerializedMember>>(ref reader, options);
                            break;
                        case nameof(SerializedMember.props):
                            member.props = JsonUtils.Deserialize<List<SerializedMember>>(ref reader, options);
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + $"Did you want to use '{SerializedMember.ValueName}', '{nameof(SerializedMember.fields)}' or '{nameof(SerializedMember.props)}'?");
                    }
                }
            }

            return member;
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
                JsonSerializer.Serialize(writer, value.fields, options);
            }
            if (value.props != null && value.props.Count > 0)
            {
                writer.WritePropertyName(nameof(SerializedMember.props));
                JsonSerializer.Serialize(writer, value.props, options);
            }

            writer.WriteEndObject();
        }
    }
}