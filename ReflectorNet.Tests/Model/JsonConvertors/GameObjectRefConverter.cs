using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Tests.Model
{
    public class GameObjectRefConverter : JsonConverter<GameObjectRef>
    {
        public override GameObjectRef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object token.");

            var result = new GameObjectRef();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return result;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read(); // Move to the value token

                    switch (propertyName)
                    {
                        case nameof(GameObjectRef.instanceID):
                            result.instanceID = reader.GetInt32();
                            break;
                        case nameof(GameObjectRef.path):
                            result.path = reader.GetString();
                            break;
                        case nameof(GameObjectRef.name):
                            result.name = reader.GetString();
                            break;
                        default:
                            throw new JsonException($"Unexpected property name: {propertyName}. "
                                + $"Expected '{nameof(GameObjectRef.instanceID)}', '{nameof(GameObjectRef.path)}', or '{nameof(GameObjectRef.name)}'.");
                    }
                }
            }

            throw new JsonException("Expected end of object token.");
        }

        public override void Write(Utf8JsonWriter writer, GameObjectRef value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteStartObject();

                // Write the "instanceID" property
                writer.WritePropertyName(nameof(GameObjectRef.instanceID));
                writer.WriteNumberValue(0);

                writer.WriteEndObject();
                return;
            }

            writer.WriteStartObject();

            writer.WriteNumber(nameof(GameObjectRef.instanceID), value.instanceID);

            if (!string.IsNullOrEmpty(value.path))
                writer.WriteString(nameof(GameObjectRef.path), value.path);

            if (!string.IsNullOrEmpty(value.name))
                writer.WriteString(nameof(GameObjectRef.name), value.name);

            writer.WriteEndObject();
        }
    }
}
