using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using com.IvanMurzak.ReflectorNet.Json;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public static partial class JsonUtils
    {
        public static partial class Schema
        {
            public const string Type = "type";
            public const string Object = "object";
            public const string Description = "description";
            public const string Properties = "properties";
            public const string Items = "items";
            public const string Array = "array";
            public const string Required = "required";
            public const string Error = "error";

            public const string Null = "null";
            public const string String = "string";
            public const string Integer = "integer"; // int, long
            public const string Number = "number"; // float, double, supports int as well
            public const string Minimum = "minimum";
            public const string Maximum = "maximum";

            public const string Id = "$id";
            public const string Defs = "$defs";
            public const string Ref = "$ref";
            public const string RefValue = "#/$defs/";
            public const string SchemaDraft = "$schema";
            public const string SchemaDraftValue = "https://json-schema.org/draft/2020-12/schema";

            public static JsonNode GetSchema<T>() => GetSchema(typeof(T));
            public static JsonNode GetSchema(Type type, bool justRef = false)
            {
                // Handle nullable types
                type = Nullable.GetUnderlyingType(type) ?? type;

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
                            var description = TypeUtils.GetDescription(type);
                            if (!string.IsNullOrEmpty(description))
                                schema[Description] = JsonValue.Create(description);
                        }
                        else
                        {
                            // Try to use built-in JsonSchemaExporter first
                            JsonNode? builtInSchema = null;
                            try
                            {
                                builtInSchema = jsonSerializerOptions.GetJsonSchemaAsNode(
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

                                            if (node == null)
                                            {
                                                node = new JsonObject
                                                {
                                                    [Type] = Object,
                                                    [Properties] = node
                                                };
                                            }
                                            else if (node is JsonObject jsonObject)
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
                            }
                            catch
                            {
                                // Ignore errors from built-in schema exporter
                                builtInSchema = null;
                            }

                            // Check if the built-in schema is valid (not just 'true' or empty)
                            var isValidBuiltInSchema = builtInSchema != null &&
                                builtInSchema is JsonObject builtInObj &&
                                builtInObj.ContainsKey(Type);

                            if (isValidBuiltInSchema)
                            {
                                schema = builtInSchema;
                            }
                            else
                            {
                                // Fallback: generate schema using ReflectorNet's introspection capabilities
                                schema = GenerateSchemaFromType(type);
                            }

                            // Get description from the type if available
                            var description = type.GetCustomAttribute<DescriptionAttribute>()?.Description;
                            if (!string.IsNullOrEmpty(description) && schema is JsonObject schemaObj)
                                schemaObj[Description] = JsonValue.Create(description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions and return null or an error message
                    return new JsonObject()
                    {
                        [Error] = $"Failed to get schema for '{type.GetTypeName(pretty: false)}':\n{ex.Message}\n{ex.StackTrace}\n"
                    };
                }

                if (schema == null)
                    throw new InvalidOperationException($"Failed to get schema for type '{type.GetTypeName(pretty: false)}'.");

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
                        [Error] = $"Unexpected schema type for '{type.GetTypeName(pretty: false)}'. Json Schema type: {schema.GetType().GetTypeName()}"
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
        }
    }
}