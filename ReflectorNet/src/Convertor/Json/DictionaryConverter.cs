/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    /// <summary>
    /// JsonConverter that handles conversion of Dictionary types to and from JSON.
    /// Supports generic Dictionary<TKey, TValue> instances with any key and value types.
    /// Keys are serialized as strings in JSON object properties.
    /// </summary>
    public class DictionaryConverter : JsonSchemaConverter<IDictionary>, IJsonSchemaConverter
    {
        public static JsonNode Schema => new JsonObject
        {
            [JsonSchema.Type] = JsonSchema.Object,
            [JsonSchema.Properties] = new JsonObject(),
            [JsonSchema.AdditionalProperties] = true
        };
        public static JsonNode SchemaRef => new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + StaticId
        };

        public override JsonNode GetSchema() => Schema;
        public override JsonNode GetSchemaRef() => SchemaRef;

        public override IDictionary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected start of object for Dictionary, but got {reader.TokenType}");

            // Get the generic arguments (TKey, TValue) from the dictionary type
            Type keyType = typeof(object);
            Type valueType = typeof(object);

            if (typeToConvert.IsGenericType)
            {
                var genericArgs = typeToConvert.GetGenericArguments();
                if (genericArgs.Length >= 2)
                {
                    keyType = genericArgs[0];
                    valueType = genericArgs[1];
                }
            }
            else if (typeToConvert.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
            {
                // Handle case where typeToConvert implements IDictionary<,> but isn't itself generic
                var dictInterface = typeToConvert.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                var genericArgs = dictInterface.GetGenericArguments();
                keyType = genericArgs[0];
                valueType = genericArgs[1];
            }

            // Create dictionary instance
            IDictionary dictionary;
            if (typeToConvert.IsInterface || typeToConvert.IsAbstract)
            {
                // If the type is an interface or abstract, create a concrete Dictionary<,>
                var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                dictionary = (IDictionary?)Activator.CreateInstance(dictType)
                    ?? throw new JsonException($"Failed to create Dictionary instance for type {dictType.GetTypeName(pretty: true)}");
            }
            else
            {
                // Try to create an instance of the requested type
                try
                {
                    dictionary = (IDictionary?)Activator.CreateInstance(typeToConvert)
                        ?? throw new JsonException($"Failed to create Dictionary instance for type {typeToConvert.GetTypeName(pretty: true)}");
                }
                catch
                {
                    // Fallback to Dictionary<,> if creation fails
                    var dictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                    dictionary = (IDictionary?)Activator.CreateInstance(dictType)
                        ?? throw new JsonException($"Failed to create Dictionary instance for type {dictType.GetTypeName(pretty: true)}");
                }
            }

            // Read the JSON object properties
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return dictionary;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException($"Expected property name in Dictionary, but got {reader.TokenType}");

                var keyString = reader.GetString();

                if (keyString == null)
                    throw new JsonException("Dictionary key cannot be null");

                // Convert the key from string to the appropriate type
                object key;
                if (keyType == typeof(string))
                {
                    key = keyString;
                }
                else
                {
                    // Try to convert the key string to the target key type
                    try
                    {
                        key = ConvertKeyFromString(keyString, keyType);
                    }
                    catch (Exception ex)
                    {
                        throw new JsonException($"Failed to convert dictionary key '{keyString}' to type {keyType.GetTypeName(pretty: true)}: {ex.Message}", ex);
                    }
                }

                // Read the value
                reader.Read();
                object? value;
                try
                {
                    value = System.Text.Json.JsonSerializer.Deserialize(ref reader, valueType, options);
                }
                catch (Exception ex)
                {
                    throw new JsonException($"Failed to deserialize dictionary value for key '{keyString}': {ex.Message}", ex);
                }

                // Add to dictionary
                dictionary[key] = value;
            }

            throw new JsonException("Unexpected end of JSON while reading Dictionary.");
        }

        public override void Write(Utf8JsonWriter writer, IDictionary value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            foreach (DictionaryEntry entry in value)
            {
                // Convert key to string for JSON property name
                var keyString = entry.Key?.ToString() ?? "null";

                writer.WritePropertyName(keyString);

                // Serialize the value
                if (entry.Value == null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    System.Text.Json.JsonSerializer.Serialize(writer, entry.Value, entry.Value.GetType(), options);
                }
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Converts a string key to the target key type.
        /// </summary>
        private object ConvertKeyFromString(string keyString, Type keyType)
        {
            if (keyString == null)
                throw new ArgumentNullException(nameof(keyString));

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(keyType) ?? keyType;

            // Handle enums
            if (underlyingType.IsEnum)
            {
                return Enum.Parse(underlyingType, keyString, ignoreCase: true);
            }

            // Handle Guid
            if (underlyingType == typeof(Guid))
            {
                return Guid.Parse(keyString);
            }

            // Handle DateTime
            if (underlyingType == typeof(DateTime))
            {
                return DateTime.Parse(keyString);
            }

            // Handle DateTimeOffset
            if (underlyingType == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Parse(keyString);
            }

            // Handle TimeSpan
            if (underlyingType == typeof(TimeSpan))
            {
                return TimeSpan.Parse(keyString);
            }

            // Use Convert.ChangeType for primitive types
            try
            {
                return Convert.ChangeType(keyString, underlyingType);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert string '{keyString}' to type {keyType.GetTypeName(pretty: true)}", ex);
            }
        }
    }
}