using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using com.IvanMurzak.ReflectorNet.Json;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public partial class JsonSerializer
    {
        readonly JsonSerializerOptions jsonSerializerOptions;

        public JsonSerializerOptions JsonSerializerOptions => jsonSerializerOptions;

        public JsonSerializer(Reflector reflector)
        {
            if (reflector == null)
                throw new ArgumentNullException(nameof(reflector));

            // Add custom converters if needed
            jsonSerializerOptions = new JsonSerializerOptions
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
                    new SerializedMemberConverter(reflector),
                    new SerializedMemberListConverter(reflector)
                }
            };
        }

        public void AddConverter(JsonConverter converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            jsonSerializerOptions.Converters.Add(converter);
        }

        public string Serialize(object? data, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Serialize(
                value: data,
                options: options ?? jsonSerializerOptions);

        public JsonElement SerializeToElement(object data, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.SerializeToElement(data, options ?? jsonSerializerOptions);

        public T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Deserialize<T>(
                json: json,
                options: options ?? jsonSerializerOptions);

        public T? Deserialize<T>(Reflector reflector, JsonElement? jsonElement, JsonSerializerOptions? options = null)
            => jsonElement.HasValue
                ? System.Text.Json.JsonSerializer.Deserialize<T>(
                    element: jsonElement.Value,
                    options: options ?? jsonSerializerOptions)
                : reflector.GetDefaultValue<T>();

        public object? Deserialize(Reflector reflector, JsonElement? jsonElement, Type type, JsonSerializerOptions? options = null)
            => jsonElement.HasValue
                ? System.Text.Json.JsonSerializer.Deserialize(
                    element: jsonElement.Value,
                    returnType: type,
                    options: options ?? jsonSerializerOptions)
                : reflector.GetDefaultValue(type);

        public object? Deserialize(string json, Type returnType, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Deserialize(
                json: json,
                returnType: returnType,
                options: options ?? jsonSerializerOptions);

        public object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Deserialize(
                reader: ref reader,
                returnType: returnType,
                options: options ?? jsonSerializerOptions);

        public TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Deserialize<TValue>(
                reader: ref reader,
                options: options ?? jsonSerializerOptions);
    }
}