using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public partial class JsonSchema
    {
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
        void PostprocessFields(JsonNode? node)
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

        /// <summary>
        /// Generate a JSON schema from a type using ReflectorNet's introspection capabilities.
        /// This method uses the Reflector's converter system to understand the type structure.
        /// </summary>
        JsonNode GenerateSchemaFromType(Reflector reflector, Type type)
        {
            // Handle primitive types
            if (TypeUtils.IsPrimitive(type))
            {
                return GeneratePrimitiveSchema(type);
            }

            // Handle arrays and collections
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return new JsonObject
                {
                    [Type] = Array,
                    [Items] = elementType != null ? GetSchema(reflector, elementType, justRef: !TypeUtils.IsPrimitive(elementType)) : new JsonObject()
                };
            }

            // Handle generic collections (List<T>, IEnumerable<T>, etc.)
            if (type.IsGenericType && TypeUtils.IsIEnumerable(type))
            {
                var itemType = TypeUtils.GetEnumerableItemType(type);
                if (itemType != null)
                {
                    return new JsonObject
                    {
                        [Type] = Array,
                        [Items] = GetSchema(reflector, itemType, justRef: !TypeUtils.IsPrimitive(itemType))
                    };
                }
            }

            // Handle regular objects by introspecting their fields and properties
            var schema = new JsonObject
            {
                [Type] = Object,
                [Properties] = new JsonObject()
            };

            var properties = schema[Properties] as JsonObject;
            var required = new JsonArray();

            // Get serializable fields
            var fields = reflector.GetSerializableFields(type);
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    var fieldSchema = GetSchema(reflector, field.FieldType, justRef: !TypeUtils.IsPrimitive(field.FieldType));

                    // Add description if available
                    var description = TypeUtils.GetFieldDescription(field);
                    if (!string.IsNullOrEmpty(description) && fieldSchema is JsonObject fieldSchemaObj)
                        fieldSchemaObj[Description] = JsonValue.Create(description);

                    properties![field.Name] = fieldSchema;

                    // Fields are typically required unless they are nullable
                    var underlyingType = Nullable.GetUnderlyingType(field.FieldType);
                    if (underlyingType == null && !field.FieldType.IsClass)
                        required.Add(field.Name);
                }
            }

            // Get serializable properties
            var props = reflector.GetSerializableProperties(type);
            if (props != null)
            {
                foreach (var prop in props)
                {
                    var propSchema = GetSchema(reflector, prop.PropertyType, justRef: !TypeUtils.IsPrimitive(prop.PropertyType));

                    // Add description if available
                    var description = TypeUtils.GetPropertyDescription(prop);
                    if (!string.IsNullOrEmpty(description) && propSchema is JsonObject propSchemaObj)
                        propSchemaObj[Description] = JsonValue.Create(description);

                    properties![prop.Name] = propSchema;

                    // Properties are required if they are value types and not nullable
                    var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                    if (underlyingType == null && !prop.PropertyType.IsClass && prop.CanWrite)
                        required.Add(prop.Name);
                }
            }

            // Add required array if it has items
            if (required.Count > 0)
                schema[Required] = required;

            return schema;
        }

        /// <summary>
        /// Generate schema for primitive types
        /// </summary>
        JsonNode GeneratePrimitiveSchema(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType == typeof(string))
                return new JsonObject { [Type] = String };

            if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
                underlyingType == typeof(short) || underlyingType == typeof(byte) ||
                underlyingType == typeof(sbyte) || underlyingType == typeof(ushort) ||
                underlyingType == typeof(uint) || underlyingType == typeof(ulong))
                return new JsonObject { [Type] = Integer };

            if (underlyingType == typeof(float) || underlyingType == typeof(double) ||
                underlyingType == typeof(decimal))
                return new JsonObject { [Type] = Number };

            if (underlyingType == typeof(bool))
                return new JsonObject { [Type] = Boolean };

            if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
                return new JsonObject { [Type] = String, ["format"] = "date-time" };

            if (underlyingType == typeof(Guid))
                return new JsonObject { [Type] = String, ["format"] = "uuid" };

            // Default for unknown primitives
            return new JsonObject { [Type] = String };
        }
    }
}