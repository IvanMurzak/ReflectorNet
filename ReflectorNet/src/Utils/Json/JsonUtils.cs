#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using com.IvanMurzak.ReflectorNet.Data;
using com.IvanMurzak.ReflectorNet.Json;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace com.IvanMurzak.ReflectorNet
{
    public static partial class JsonUtils
    {
        static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignore null fields
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                new DefaultJsonTypeInfoResolver()
            ),
            Converters =
            {
                new JsonStringEnumConverter(),
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

        public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null) where T : class
            => JsonSerializer.Deserialize<T>(json, options ?? jsonSerializerOptions);

        public static T? Deserialize<T>(JsonElement jsonElement, JsonSerializerOptions? options = null) where T : class
            => JsonSerializer.Deserialize<T>(jsonElement, options ?? jsonSerializerOptions);

        public static object? Deserialize(JsonElement jsonElement, Type type, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize(jsonElement, type, options ?? jsonSerializerOptions);

        public static object? Deserialize(string json, Type type, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize(json, type, options ?? jsonSerializerOptions);

        public static object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize(ref reader, returnType, options ?? jsonSerializerOptions);

        public static TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions? options = null) where TValue : class
            => JsonSerializer.Deserialize<TValue>(ref reader, options ?? jsonSerializerOptions);

        public static JsonElement SerializeToElement(object data, JsonSerializerOptions? options = null)
            => JsonSerializer.SerializeToElement(data, options ?? jsonSerializerOptions);

        public static string Serialize(object? data, JsonSerializerOptions? options = null)
            => JsonSerializer.Serialize(data, options ?? jsonSerializerOptions);

        public static string ToJson(this IRequestCallTool? data, JsonSerializerOptions? options = null)
        {
            if (data == null)
                return "{}";
            return JsonSerializer.Serialize(data, options ?? jsonSerializerOptions);
        }

        public static string ToJson(this IResponseListTool? data, JsonSerializerOptions? options = null)
        {
            if (data == null)
                return "{}";
            return JsonSerializer.Serialize(data, options ?? jsonSerializerOptions);
        }

        public static string ToJson<T>(this IResponseData<T>? data, JsonSerializerOptions? options = null) where T : class
        {
            if (data == null)
                return "{}";
            return JsonSerializer.Serialize(data, options ?? jsonSerializerOptions);
        }

        public static string JsonSerialize(this object? data, JsonSerializerOptions? options = null)
        {
            if (data == null)
                return "null";
            return JsonSerializer.Serialize(data, options ?? jsonSerializerOptions);
        }

        public static IRequestCallTool? ParseRequestData(this string json, JsonSerializerOptions? options = null)
            => JsonSerializer.Deserialize<IRequestCallTool>(json, options ?? jsonSerializerOptions);

        public static IResponseData<T>? ParseResponseData<T>(this string json, JsonSerializerOptions? options = null) where T : class
            => JsonSerializer.Deserialize<ResponseData<T>>(json, options ?? jsonSerializerOptions);

        public static class Resource
        {
            public static string ToJson(object data)
                => JsonSerializer.Serialize(data, jsonSerializerOptions);
        }
    }
}