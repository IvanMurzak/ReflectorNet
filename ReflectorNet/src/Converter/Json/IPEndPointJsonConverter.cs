/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion between JSON objects and System.Net.IPEndPoint.
    /// </summary>
    public class IPEndPointJsonConverter : JsonSchemaConverter<IPEndPoint>, IJsonSchemaConverter
    {
        private const string AddressProperty = "address";
        private const string PortProperty = "port";

        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject
            {
                [AddressProperty] = IPAddressJsonConverter.SchemaRef,
                [PortProperty] = new JsonObject
                {
                    [JsonSchema.Type] = JsonSchema.Integer,
                    ["minimum"] = 0,
                    ["maximum"] = 65535
                }
            },
            [JsonSchema.Required] = new JsonArray { AddressProperty, PortProperty }
        };

        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + StaticId
        };

        public override JsonNode GetSchemaRef() => SchemaRef;
        public override JsonNode GetSchema() => Schema;

        public override IPEndPoint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject for IPEndPoint.");

            IPAddress address = null;
            int port = 0;
            bool addressSet = false;
            bool portSet = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();

                    if (string.Equals(propertyName, AddressProperty, StringComparison.OrdinalIgnoreCase))
                    {
                        address = System.Text.Json.JsonSerializer.Deserialize<IPAddress>(ref reader, options);
                        addressSet = true;
                    }
                    else if (string.Equals(propertyName, PortProperty, StringComparison.OrdinalIgnoreCase))
                    {
                        port = reader.GetInt32();
                        portSet = true;
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }

            if (!addressSet || !portSet)
                throw new JsonException("IPEndPoint requires both 'address' and 'port' properties.");

            return new IPEndPoint(address, port);
        }

        public override void Write(Utf8JsonWriter writer, IPEndPoint value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(AddressProperty);
            System.Text.Json.JsonSerializer.Serialize(writer, value.Address, options);
            writer.WriteNumber(PortProperty, value.Port);
            writer.WriteEndObject();
        }
    }
}
