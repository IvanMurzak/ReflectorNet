using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet.Json
{
    public class MethodInfoConverter : JsonConverter<MethodInfo>
    {
        static class Json
        {
            public const string Name = "name";
            public const string DeclaringType = "declaringType";
            public const string Parameters = "parameters";
            public const string Type = "type";
        }
        public override MethodInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var typeName = root.GetProperty(Json.DeclaringType).GetString();
            var methodName = root.GetProperty(Json.Name).GetString();
            var parameterTypes = new List<Type>();

            if (root.TryGetProperty(Json.Parameters, out var parametersElement))
            {
                foreach (var param in parametersElement.EnumerateArray())
                {
                    var paramTypeName = param.GetProperty(Json.Type).GetString();
                    var paramType = TypeUtils.GetType(paramTypeName);
                    if (paramType != null)
                        parameterTypes.Add(paramType);
                }
            }

            var declaringType = TypeUtils.GetType(typeName);
            if (declaringType == null)
                throw new JsonException($"Could not find type: {typeName}");

            var method = string.IsNullOrEmpty(methodName)
                ? null
                : declaringType.GetMethod(methodName!, parameterTypes.ToArray());
            if (method == null)
                throw new JsonException($"Could not find method: {methodName} on type: {typeName}");

            return method;
        }

        public override void Write(Utf8JsonWriter writer, MethodInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(Json.Name, value.Name);
            writer.WriteString(Json.DeclaringType, value.DeclaringType?.GetTypeName(pretty: false));

            writer.WritePropertyName(Json.Parameters);
            writer.WriteStartArray();
            foreach (var param in value.GetParameters())
            {
                writer.WriteStartObject();
                writer.WriteString(Json.Name, param.Name);
                writer.WriteString(Json.Type, param.ParameterType.GetTypeName(pretty: false));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}