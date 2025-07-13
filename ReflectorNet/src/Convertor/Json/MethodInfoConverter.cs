using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Json
{
    public class MethodInfoConverter : JsonConverter<MethodInfo>
    {
        public override MethodInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var typeName = root.GetProperty("declaringType").GetString();
            var methodName = root.GetProperty("name").GetString();
            var parameterTypes = new List<Type>();

            if (root.TryGetProperty("parameters", out var parametersElement))
            {
                foreach (var param in parametersElement.EnumerateArray())
                {
                    var paramTypeName = param.GetProperty("type").GetString();
                    var paramType = Type.GetType(paramTypeName);
                    if (paramType != null)
                        parameterTypes.Add(paramType);
                }
            }

            var declaringType = Type.GetType(typeName);
            if (declaringType == null)
                throw new JsonException($"Could not find type: {typeName}");

            var method = declaringType.GetMethod(methodName, parameterTypes.ToArray());
            if (method == null)
                throw new JsonException($"Could not find method: {methodName} on type: {typeName}");

            return method;
        }

        public override void Write(Utf8JsonWriter writer, MethodInfo value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("name", value.Name);
            writer.WriteString("declaringType", value.DeclaringType?.AssemblyQualifiedName);

            writer.WritePropertyName("parameters");
            writer.WriteStartArray();
            foreach (var param in value.GetParameters())
            {
                writer.WriteStartObject();
                writer.WriteString("name", param.Name);
                writer.WriteString("type", param.ParameterType.AssemblyQualifiedName);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }
}