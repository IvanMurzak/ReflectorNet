using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    public class SerializedMemberListConverter : JsonConverter<SerializedMemberList>, IJsonSchemaConverter
    {
        public static string StaticId => TypeUtils.GetTypeId<SerializedMemberList>();
        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Array,
            [JsonSchema.Items] = new JsonObject
            {
                [JsonSchema.Ref] = JsonSchema.RefValue + SerializedMemberConverter.StaticId
            },
            [JsonSchema.AdditionalProperties] = false
        };

        public string Id => StaticId;

        public SerializedMemberListConverter(Reflector reflector)
        {
            if (reflector == null)
                throw new ArgumentNullException(nameof(reflector));
        }

        public JsonNode GetScheme() => Schema;
        public JsonNode GetSchemeRef() => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + Id
        };

        public override SerializedMemberList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"Expected start of array, but got {reader.TokenType}");

            var member = new SerializedMemberList();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return member;

                var item = System.Text.Json.JsonSerializer.Deserialize<SerializedMember>(ref reader, options);
                if (item != null)
                    member.Add(item);
            }

            throw new JsonException("Unexpected end of array.");
        }

        public override void Write(Utf8JsonWriter writer, SerializedMemberList value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();
            foreach (var item in value)
            {
                System.Text.Json.JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }
}