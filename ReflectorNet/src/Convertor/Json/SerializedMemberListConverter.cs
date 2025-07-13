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
        public static string StaticId => typeof(SerializedMemberList).FullName;
        public static JsonNode Schema => new JsonObject
        {
            [JsonUtils.Schema.Id] = StaticId,
            [JsonUtils.Schema.Type] = JsonUtils.Schema.Array,
            [JsonUtils.Schema.Items] = new JsonObject
            {
                [JsonUtils.Schema.Ref] = SerializedMemberConverter.StaticId
            }
        };
        public string Id => StaticId;
        public JsonNode GetScheme() => Schema;
        public JsonNode GetSchemeRef() => new JsonObject
        {
            [JsonUtils.Schema.Ref] = Id
        };

        public override SerializedMemberList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var member = new SerializedMemberList();

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException($"Expected start of array, but got {reader.TokenType}");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                var item = JsonSerializer.Deserialize<SerializedMember>(ref reader, options);
                if (item != null)
                    member.Add(item);
            }

            return member;
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
                JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }
}