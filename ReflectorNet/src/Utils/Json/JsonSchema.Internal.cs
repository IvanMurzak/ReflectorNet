/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    public partial class JsonSchema
    {
        public static List<JsonNode?> FindAllProperties(JsonNode node, string fieldName)
        {
            var result = new List<JsonNode?>();
            if (node is JsonObject obj)
            {
                foreach (var kvp in obj)
                {
                    if (kvp.Value == null)
                    {
                        if (kvp.Key == fieldName)
                            result.Add(null);
                    }
                    else
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
        JsonNode GenerateSchemaFromType(Reflector reflector, Type type, JsonObject defines)
        {
            // Handle primitive types
            if (TypeUtils.IsPrimitive(type))
            {
                return GeneratePrimitiveSchema(type);
            }

            // Handle generic collections, arrays and IEnumerable (T[], List<T>, IEnumerable<T>, etc.)
            if (TypeUtils.IsIEnumerable(type))
            {
                var itemType = TypeUtils.GetEnumerableItemType(type);
                if (itemType != null)
                {
                    var itemTypeId = itemType.GetSchemaTypeId();
                    var isItemPrimitive = TypeUtils.IsPrimitive(itemType);

                    if (!isItemPrimitive && defines.ContainsKey(itemTypeId) == false)
                    {
                        // Add placeholder first to prevent infinite recursion
                        defines[itemTypeId] = new JsonObject { [Type] = Object };
                        defines[itemTypeId] = GetSchema(reflector, itemType, defines: defines);
                    }

                    var typeId = type.GetSchemaTypeId();
                    if (defines.ContainsKey(typeId) == false)
                    {
                        // Add placeholder first to prevent infinite recursion
                        defines[typeId] = new JsonObject
                        {
                            [Type] = Array,
                            [Items] = isItemPrimitive
                                ? GetSchema(reflector, itemType, defines: defines)
                                : GetSchemaRef(reflector, itemType)
                        };
                    }

                    return new JsonObject
                    {
                        [Type] = Array,
                        [Items] = isItemPrimitive
                            ? GetSchema(reflector, itemType, defines: defines)
                            : GetSchemaRef(reflector, itemType)
                    };
                }
            }

            // Handle regular objects by introspecting their fields and properties
            var properties = new JsonObject();
            var required = new JsonArray();
            var schema = new JsonObject { [Type] = Object };

            defines ??= new();

            // Get serializable fields
            var fields = reflector.GetSerializableFields(type);
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;

                    var underlyingType = Nullable.GetUnderlyingType(field.FieldType);
                    var isPrimitive = TypeUtils.IsPrimitive(underlyingType ?? field.FieldType);
                    var fieldName = field.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? field.Name;
                    var schemaRef = isPrimitive
                        ? GetSchema(reflector, field.FieldType, defines: defines)
                        : GetSchemaRef(reflector, field.FieldType);

                    if (!isPrimitive)
                    {
                        var typeId = field.FieldType.GetSchemaTypeId();
                        if (!defines.ContainsKey(typeId))
                        {
                            // Add placeholder first to prevent infinite recursion
                            defines[typeId] = new JsonObject { [Type] = Object };
                            defines[typeId] = GetSchema(reflector, field.FieldType, defines: defines);
                        }
                    }

                    // Add description if available
                    var description = TypeUtils.GetFieldDescription(field);
                    if (!string.IsNullOrEmpty(description) && schemaRef is JsonObject fieldSchemaObj)
                        fieldSchemaObj[Description] = JsonValue.Create(description);

                    properties[fieldName] = schemaRef;

                    // Fields are typically required unless they are nullable
                    if (underlyingType == null && !field.FieldType.IsClass)
                        required.Add(fieldName);
                }
            }

            // Get serializable properties
            var props = reflector.GetSerializableProperties(type);
            if (props != null)
            {
                foreach (var prop in props)
                {
                    if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;

                    var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType);
                    var isPrimitive = TypeUtils.IsPrimitive(underlyingType ?? prop.PropertyType);
                    var propName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;
                    var schemaRef = isPrimitive
                        ? GetSchema(reflector, prop.PropertyType, defines: defines)
                        : GetSchemaRef(reflector, prop.PropertyType);

                    if (!isPrimitive)
                    {
                        var typeId = prop.PropertyType.GetSchemaTypeId();
                        if (!defines.ContainsKey(typeId))
                        {
                            // Add placeholder first to prevent infinite recursion
                            defines[typeId] = new JsonObject { [Type] = Object };
                            defines[typeId] = GetSchema(reflector, prop.PropertyType, defines: defines);
                        }
                    }

                    // Add description if available
                    var description = TypeUtils.GetPropertyDescription(prop);
                    if (!string.IsNullOrEmpty(description) && schemaRef is JsonObject propSchemaObj)
                        propSchemaObj[Description] = JsonValue.Create(description);

                    properties![propName] = schemaRef;

                    // Properties are required if they are value types and not nullable
                    if (underlyingType == null && !prop.PropertyType.IsClass && prop.CanWrite)
                        required.Add(propName);
                }
            }

            // Add required array if it has items
            if (required.Count > 0)
                schema[Required] = required;

            if (properties.Count > 0)
                schema[Properties] = properties;

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

            // Handle enum types
            if (underlyingType.IsEnum)
            {
                var enumValues = new JsonArray();
                foreach (var enumValue in Enum.GetValues(underlyingType))
                {
                    enumValues.Add(JsonValue.Create(enumValue.ToString()));
                }

                return new JsonObject
                {
                    [Type] = String,
                    ["enum"] = enumValues
                };
            }

            // Default for unknown primitives
            return new JsonObject { [Type] = String };
        }
    }
}