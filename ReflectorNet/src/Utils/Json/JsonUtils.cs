using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Json;
using System.Linq;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class JsonUtils
    {
        public const string Null = "null";
        public const string EmptyJsonObject = "{}";

        static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            // DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignore 'null' field and properties
            DefaultIgnoreCondition = JsonIgnoreCondition.Never, // Include 'null' fields and properties
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                new DefaultJsonTypeInfoResolver()
            ),
            Converters =
            {
                new JsonStringEnumConverter(),
                new MethodInfoConverter(),
                new SerializedMemberConverter(),
                new SerializedMemberListConverter(),

                // new SerializedMemberConverterFactory()
            }
        };

        public static JsonSerializerOptions JsonSerializerOptions => jsonSerializerOptions;

        public static void AddConverter(JsonConverter converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            jsonSerializerOptions.Converters.Add(converter);
        }
        public static bool RemoveConverter<T>() where T : JsonConverter
        {
            var converter = jsonSerializerOptions.Converters.FirstOrDefault(c => c.GetType() == typeof(T));
            if (converter != null)
                return jsonSerializerOptions.Converters.Remove(converter);
            return false;
        }

        public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize<T>(
                json: json,
                options: options ?? jsonSerializerOptions);

        public static T? Deserialize<T>(Reflector reflector, JsonElement? jsonElement, JsonSerializerOptions? options = null)
            => jsonElement.HasValue
                ? JsonSerializer.Deserialize<T>(
                    element: jsonElement.Value,
                    options: options ?? jsonSerializerOptions)
                : reflector.GetDefaultValue<T>();

        public static object? Deserialize(Reflector reflector, JsonElement? jsonElement, Type type, JsonSerializerOptions? options = null)
            => jsonElement.HasValue
                ? JsonSerializer.Deserialize(
                    element: jsonElement.Value,
                    returnType: type,
                    options: options ?? jsonSerializerOptions)
                : reflector.GetDefaultValue(type);

        public static object? Deserialize(string json, Type type, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize(json, type, options ?? jsonSerializerOptions);

        public static object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize(ref reader, returnType, options ?? jsonSerializerOptions);

        public static TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize<TValue>(ref reader, options ?? jsonSerializerOptions);

        public static JsonElement ToJsonElement(this object data, JsonSerializerOptions? options = null)
            => JsonSerializer.SerializeToElement(data, options ?? jsonSerializerOptions);

        public static JsonElement? ToJsonElement(this JsonNode? node)
        {
            if (node == null)
                return null;

            // Convert JsonNode to JsonElement
            var jsonString = node.ToJsonString();

            // Parse the JSON string into a JsonElement
            using var document = JsonDocument.Parse(jsonString);
            return document.RootElement.Clone();
        }

        public static string ToJson(this object? data, JsonSerializerOptions? options = null)
            => ToJson(data, Null, options ?? jsonSerializerOptions);

        public static string ToJsonOrEmptyJsonObject(this object? data, JsonSerializerOptions? options = null)
            => ToJson(data, EmptyJsonObject, options);

        public static string ToJson(this object? data, string defaultValue, JsonSerializerOptions? options = null)
        {
            if (data == null)
                return defaultValue;
            return JsonSerializer.Serialize(data, options ?? jsonSerializerOptions);
        }
    }
}