using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using com.IvanMurzak.ReflectorNet.Json;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class JsonUtils
    {
        public static class Schema
        {
            public const string Type = "type";
            public const string Object = "object";
            public const string Description = "description";
            public const string Properties = "properties";
            public const string Items = "items";
            public const string Array = "array";
            public const string Required = "required";

            public const string Null = "null";
            public const string String = "string";
            public const string Number = "number";

            public const string Id = "$id";
            public const string Defs = "$defs";
            public const string Ref = "$ref";
            public const string RefValue = "#/$defs/";
            public const string SchemaDraft = "$schema";
            public const string SchemaDraftValue = "https://json-schema.org/draft/2020-12/schema";

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
                        if (justRef && !TypeUtils.IsPrimitive(type))
                        {
                            // If justRef is true and the type is not primitive, we return a reference schema
                            schema = new JsonObject
                            {
                                [Ref] = RefValue + type.GetTypeId()
                            };

                            // Get description from the type if available
                            var description = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
                            if (!string.IsNullOrEmpty(description))
                                schema[Description] = JsonValue.Create(description);
                        }
                        else
                        {
                            // For non-nullable types, get the schema directly
                            schema = jsonSerializerOptions.GetJsonSchemaAsNode(
                                type: type,
                                exporterOptions: new JsonSchemaExporterOptions
                                {
                                    TreatNullObliviousAsNonNullable = false,
                                    TransformSchemaNode = (context, node) =>
                                    {
                                        if (context.PropertyInfo == null)
                                            return node;

                                        var description = TypeUtils.GetPropertyDescription(context);

                                        // If the type is primitive, we can return the schema directly
                                        if (TypeUtils.IsPrimitive(context.PropertyInfo.PropertyType))
                                        {
                                            if (!string.IsNullOrEmpty(description))
                                                node[Description] = JsonValue.Create(description);

                                            return node;
                                        }

                                        // Otherwise, we need to ensure the schema is an object
                                        if (node is JsonObject jsonObject)
                                        {
                                            jsonObject[Type] = Object;
                                        }
                                        else
                                        {
                                            node = new JsonObject
                                            {
                                                [Type] = Object,
                                                [Properties] = node
                                            };
                                        }

                                        if (!string.IsNullOrEmpty(description))
                                            node[Description] = JsonValue.Create(description);

                                        // Remove nested schema version if it exists
                                        node.AsObject().Remove(SchemaDraft);

                                        return node;
                                    }
                                });

                            // Get description from the type if available
                            var description = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
                            if (!string.IsNullOrEmpty(description))
                                schema[Description] = JsonValue.Create(description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions and return null or an error message
                    return new JsonObject()
                    {
                        ["error"] = $"Failed to get schema for '{type.GetTypeName(pretty: false)}': {ex.Message}"
                    };
                }

                if (schema == null)
                    return null;

                PostprocessFields(schema);

                if (schema is JsonObject parameterSchemaObject)
                {
                    var propertyDescription = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
                    if (!string.IsNullOrEmpty(propertyDescription))
                        parameterSchemaObject[Description] = JsonValue.Create(propertyDescription);
                }
                else
                {
                    return new JsonObject()
                    {
                        ["error"] = $"Unexpected schema type for '{type.GetTypeName(pretty: false)}'. Json Schema type: {schema.GetType()}"
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
                    return new JsonObject { [Type] = Object };

                var properties = new JsonObject();
                var defines = new JsonObject();
                var required = new JsonArray();

                // Create a schema object manually
                var schema = new JsonObject
                {
                    [SchemaDraft] = JsonValue.Create(SchemaDraftValue),
                    [Type] = Object,
                    [Properties] = properties,
                    [Required] = required,
                    [Defs] = defines
                };

                foreach (var parameter in parameters)
                {
                    var parameterSchema = default(JsonNode);
                    var isPrimitive = TypeUtils.IsPrimitive(parameter.ParameterType);
                    if (isPrimitive)
                    {
                        parameterSchema = GetSchema(parameter.ParameterType, justRef: justRef);
                        if (parameterSchema == null)
                            continue;
                    }
                    else
                    {
                        var typeId = parameter.ParameterType.GetTypeId();
                        if (defines.ContainsKey(typeId))
                        {
                            parameterSchema = GetSchema(parameter.ParameterType, justRef: true);
                        }
                        else
                        {
                            var fullSchema = GetSchema(parameter.ParameterType, justRef: false);
                            if (fullSchema == null)
                                continue;
                            defines[typeId] = fullSchema;
                            parameterSchema = GetSchema(parameter.ParameterType, justRef: true);
                        }

                        if (parameterSchema == null)
                            continue;
                    }
                    // Use JsonSchemaExporter to get the schema for each parameter type

                    properties[parameter.Name!] = parameterSchema;

                    if (parameterSchema is JsonObject parameterSchemaObject)
                    {
                        var propertyDescription = parameter.GetCustomAttribute<DescriptionAttribute>()?.Description;
                        if (!string.IsNullOrEmpty(propertyDescription))
                            parameterSchemaObject[Description] = JsonValue.Create(propertyDescription);
                    }

                    // Check if the parameter has a default value
                    if (!parameter.HasDefaultValue)
                        required.Add(parameter.Name!);

                    // Add generic type parameters recursively if any
                    foreach (var genericArgument in TypeUtils.GetGenericTypes(parameter.ParameterType))
                    {
                        if (TypeUtils.IsPrimitive(genericArgument))
                            continue;

                        var typeId = genericArgument.GetTypeId();
                        if (defines.ContainsKey(typeId))
                            continue;

                        var genericSchema = GetSchema(genericArgument, justRef: false);
                        if (genericSchema != null)
                            defines[typeId] = genericSchema;
                    }
                }

                if (defines.Count == 0)
                    schema.Remove(Defs);
                return schema;
            }

            public static List<JsonNode> FindAllProperties(JsonNode node, string fieldName)
            {
                var result = new List<JsonNode>();
                if (node is JsonObject obj)
                {
                    foreach (var kvp in obj)
                    {
                        if (kvp.Value != null)
                        {
                            if (kvp.Key == fieldName)
                                result.Add(kvp.Value);

                            result.AddRange(FindAllProperties(kvp.Value, fieldName));
                        }
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
            public static void PostprocessFields(JsonNode? node)
            {
                if (node == null)
                    return;

                if (node is JsonObject obj)
                {
                    // Fixing "type" field. It should be not nullable, because current LLM models doesn't support nullable types
                    if (obj.TryGetPropertyValue(Type, out var typeNode))
                    {
                        if (typeNode is JsonValue typeValue)
                        {
                            if (typeNode.ToString() == Array)
                                if (obj.TryGetPropertyValue(Items, out var itemsNode))
                                    PostprocessFields(itemsNode);
                        }
                        else
                        {
                            if (typeNode is JsonArray typeArray)
                            {
                                var correctTypeValue = typeArray
                                    .FirstOrDefault(x => x is JsonValue value && value.ToString() != Null)
                                    ?.ToString();

                                if (correctTypeValue != null)
                                    obj[Type] = JsonValue.Create(correctTypeValue.ToString());
                            }
                        }
                    }

                    foreach (var kvp in obj)
                    {
                        if (kvp.Key == Type)
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
        }
    }
}