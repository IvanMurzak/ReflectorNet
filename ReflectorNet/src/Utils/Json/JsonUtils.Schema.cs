#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using com.IvanMurzak.ReflectorNet.Json;

namespace com.IvanMurzak.ReflectorNet
{
    public static partial class JsonUtils
    {
        public static JsonNode? GetSchema<T>() => GetSchema(typeof(T));
        public static JsonNode? GetSchema(Type type, bool justRef = false)
        {
            // Handle nullable types
            var underlyingNullableType = Nullable.GetUnderlyingType(type);
            if (underlyingNullableType != null)
                type = underlyingNullableType;

            var schema = default(JsonNode);

            try
            {
                var jsonConverter = jsonSerializerOptions.GetConverter(type);
                if (jsonConverter is IJsonSchemaConverter schemeConvertor)
                {
                    schema = justRef
                        ? schemeConvertor.GetSchemeRef()
                        : schemeConvertor.GetScheme();
                }
                else
                {
                    schema = jsonSerializerOptions.GetJsonSchemaAsNode(
                        type: type,
                        exporterOptions: new JsonSchemaExporterOptions
                        {
                            TreatNullObliviousAsNonNullable = false
                        });
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and return null or an error message
                return new JsonObject()
                {
                    ["error"] = $"Failed to get schema for '{type.FullName}': {ex.Message}"
                };
            }

            if (schema == null)
                return null;

            PostprocessFields(schema);

            if (schema is JsonObject parameterSchemaObject)
            {
                var propertyDescription = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (!string.IsNullOrEmpty(propertyDescription))
                    parameterSchemaObject["description"] = JsonValue.Create(propertyDescription);
            }
            else
            {
                return new JsonObject()
                {
                    ["error"] = $"Unexpected schema type for '{type.FullName}'. Json Schema type: {schema.GetType()}"
                };
            }
            return schema;
        }
        public static JsonNode? GetArgumentsSchema(MethodInfo method, bool justRef = false)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return new JsonObject { ["type"] = "object" };

            var properties = new JsonObject();
            var required = new JsonArray();
            // Create a schema object manually
            var schema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required
            };

            foreach (var parameter in parameters)
            {
                // Use JsonSchemaExporter to get the schema for each parameter type
                var parameterSchema = GetSchema(parameter.ParameterType, justRef: justRef);
                if (parameterSchema == null)
                    continue;

                properties[parameter.Name!] = parameterSchema;

                if (parameterSchema is JsonObject parameterSchemaObject)
                {
                    var propertyDescription = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
                    if (!string.IsNullOrEmpty(propertyDescription))
                        parameterSchemaObject["description"] = JsonValue.Create(propertyDescription);
                }

                // Check if the parameter has a default value
                if (!parameter.HasDefaultValue)
                    required.Add(parameter.Name!);
            }
            return schema;
        }

        public static List<JsonNode> FindAllProperties(JsonNode node, string fieldName)
        {
            var result = new List<JsonNode>();
            if (node is JsonObject obj)
            {
                foreach (var kvp in obj)
                {
                    if (kvp.Key == fieldName)
                        result.Add(kvp.Value);

                    if (kvp.Value != null)
                        result.AddRange(FindAllProperties(kvp.Value, fieldName));
                }
            }
            else if (node is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item != null)
                        result.AddRange(FindAllProperties(item, fieldName));
                }
            }
            return result;
        }
        public static void PostprocessFields(JsonNode node)
        {
            if (node == null)
                return;

            if (node is JsonObject obj)
            {
                // Fixing "type" field. It should be not nullable, because current LLM models doesn't support nullable types
                if (obj.TryGetPropertyValue("type", out var typeNode))
                {
                    if (typeNode is JsonValue typeValue)
                    {
                        if (typeNode.ToString() == "array")
                            if (obj.TryGetPropertyValue("items", out var itemsNode))
                                PostprocessFields(itemsNode);
                    }
                    else
                    {
                        if (typeNode is JsonArray typeArray)
                        {
                            var correctTypeValue = typeArray
                                .FirstOrDefault(x => x is JsonValue value && value.ToString() != "null")
                                ?.ToString();

                            if (correctTypeValue != null)
                                obj["type"] = JsonValue.Create(correctTypeValue.ToString());
                        }
                    }
                }

                foreach (var kvp in obj)
                {
                    if (kvp.Key == "type")
                        continue;

                    if (kvp.Value != null)
                        PostprocessFields(kvp.Value);
                }
            }
            if (node is JsonArray arr)
            {
                foreach (var item in arr)
                    if (item != null)
                        PostprocessFields(item);
            }
        }

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

        private static bool IsNullable(Type type)
        {
            if (!type.IsValueType)
                return true; // Reference types are nullable
            return Nullable.GetUnderlyingType(type) != null; // Nullable value types
        }
    }
}