using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    public class SerializedMemberConverter : JsonConverter<SerializedMember>, IJsonSchemaConverter
    {
        public static string StaticId => TypeUtils.GetTypeId<SerializedMember>();
        public static JsonNode Schema => new JsonObject
        {
            [JsonUtils.Schema.Type] = JsonUtils.Schema.Object,
            [JsonUtils.Schema.Properties] = new JsonObject
            {
                [nameof(SerializedMember.typeName)] = new JsonObject
                {
                    [JsonUtils.Schema.Type] = JsonUtils.Schema.String,
                    [JsonUtils.Schema.Description] = "Full type name. Eg: 'System.String', 'System.Int32', 'UnityEngine.Vector3', etc."
                },
                [nameof(SerializedMember.name)] = new JsonObject
                {
                    [JsonUtils.Schema.Type] = JsonUtils.Schema.String,
                    [JsonUtils.Schema.Description] = "Name of the member. Can be null or empty."
                },
                [SerializedMember.ValueName] = new JsonObject
                {
                    [JsonUtils.Schema.Type] = JsonUtils.Schema.Object,
                    [JsonUtils.Schema.Description] = "Member's value. Can be null or empty.",
                },
                [nameof(SerializedMember.fields)] = new JsonObject
                {
                    [JsonUtils.Schema.Type] = JsonUtils.Schema.Array,
                    [JsonUtils.Schema.Items] = new JsonObject
                    {
                        [JsonUtils.Schema.Ref] = JsonUtils.Schema.RefValue + StaticId,
                        [JsonUtils.Schema.Description] = "Field's value, nested fields and properties."
                    },
                    [JsonUtils.Schema.Description] = "List of fields of the member. Can be null or empty.",
                },
                [nameof(SerializedMember.props)] = new JsonObject
                {
                    [JsonUtils.Schema.Type] = JsonUtils.Schema.Array,
                    [JsonUtils.Schema.Items] = new JsonObject
                    {
                        [JsonUtils.Schema.Ref] = JsonUtils.Schema.RefValue + StaticId,
                        [JsonUtils.Schema.Description] = "Property's value, nested fields and properties."
                    },
                    [JsonUtils.Schema.Description] = "List of properties of the member. Can be null or empty.",
                }
            },
            [JsonUtils.Schema.Required] = new JsonArray { nameof(SerializedMember.typeName), SerializedMember.ValueName }
        };
        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonUtils.Schema.Ref] = JsonUtils.Schema.RefValue + StaticId
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
                            throw new JsonException($"Unexpected property name: '{propertyName}'. "
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