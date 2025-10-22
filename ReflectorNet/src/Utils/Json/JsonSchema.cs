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
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet.Json;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /// <summary>
    /// Provides comprehensive JSON Schema generation capabilities for .NET types, method parameters,
    /// and complex object structures. This class enables automatic schema creation for API documentation,
    /// form generation, validation, and AI-driven development scenarios.
    ///
    /// Core Capabilities:
    /// - Type-to-Schema Conversion: Generates JSON Schema Draft 2020-12 compliant schemas from .NET types
    /// - Method Parameter Schemas: Creates schemas for method parameters to enable dynamic invocation
    /// - Reference Optimization: Supports both full schema definitions and compact $ref references
    /// - Documentation Integration: Extracts descriptions from DescriptionAttribute and XML documentation
    /// - Primitive Handling: Optimized schema generation for built-in .NET types
    /// - Complex Type Support: Handles nested objects, collections, generics, and inheritance
    ///
    /// Schema Generation Modes:
    /// - Full Schema (justRef=false): Complete schema with all properties and type definitions
    /// - Reference Schema (justRef=true): Compact $ref pointing to definitions in $defs section
    /// - Hybrid Mode: Combines both approaches for optimal schema size and readability
    ///
    /// Integration Features:
    /// - ReflectorNet Converter System: Leverages registered converters for custom schema logic
    /// - Type Introspection: Uses Reflector's field and property discovery for accurate schemas
    /// - Error Handling: Provides detailed error information for schema generation failures
    /// - Extensibility: Supports custom schema converters through IJsonSchemaConverter interface
    ///
    /// This class is essential for scenarios involving dynamic method invocation, API documentation
    /// generation, form creation, and AI systems that need to understand .NET type structures.
    /// </summary>
    public partial class JsonSchema
    {
        /// <summary>
        /// Generates a comprehensive JSON Schema representation for the specified generic type.
        /// This method provides flexible schema generation supporting both full schema definitions
        /// and compact reference schemas, with intelligent handling of primitive vs complex types.
        ///
        /// Schema Generation Features:
        /// - Type unwrapping: Automatically handles nullable types by unwrapping to underlying type
        /// - Converter integration: Leverages registered JsonConverter implementations for custom schema logic
        /// - Reference optimization: Generates compact $ref schemas for complex types when justRef=true
        /// - Documentation extraction: Includes descriptions from DescriptionAttribute and type metadata
        /// - Error handling: Provides detailed error information for schema generation failures
        /// - Primitive handling: Optimizes schema generation for built-in types
        ///
        /// Schema Modes:
        /// - Full schema (justRef=false): Complete schema definition with all properties and nested types
        /// - Reference schema (justRef=true): Compact $ref pointing to type definition in $defs
        /// - Primitive inline: Primitive types are always inlined regardless of justRef setting
        ///
        /// Generated schemas conform to JSON Schema Draft 2020-12 specification.
        /// </summary>
        /// <typeparam name="T">The type for which to generate the JSON Schema.</typeparam>
        /// <param name="reflector">The Reflector instance used for type analysis and converter access.</param>
        /// <returns>A JsonNode containing the JSON Schema representation of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when schema generation fails for the specified type.</exception>
        public JsonNode GetSchema<T>(Reflector reflector, JsonObject? defines = null)
            => GetSchema(reflector, typeof(T), defines);

        /// <summary>
        /// Generates a comprehensive JSON Schema representation for the specified generic type.
        /// This method provides flexible schema generation supporting both full schema definitions
        /// and compact reference schemas, with intelligent handling of primitive vs complex types.
        ///
        /// Schema Generation Features:
        /// - Type unwrapping: Automatically handles nullable types by unwrapping to underlying type
        /// - Converter integration: Leverages registered JsonConverter implementations for custom schema logic
        /// - Reference optimization: Generates compact $ref schemas for complex types when justRef=true
        /// - Documentation extraction: Includes descriptions from DescriptionAttribute and type metadata
        /// - Error handling: Provides detailed error information for schema generation failures
        /// - Primitive handling: Optimizes schema generation for built-in types
        ///
        /// Schema Modes:
        /// - Full schema (justRef=false): Complete schema definition with all properties and nested types
        /// - Reference schema (justRef=true): Compact $ref pointing to type definition in $defs
        /// - Primitive inline: Primitive types are always inlined regardless of justRef setting
        ///
        /// Generated schemas conform to JSON Schema Draft 2020-12 specification.
        /// </summary>
        /// <typeparam name="T">The type for which to generate the JSON Schema.</typeparam>
        /// <param name="reflector">The Reflector instance used for type analysis and converter access.</param>
        /// <returns>A JsonNode containing the JSON Schema representation of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when schema generation fails for the specified type.</exception>
        public JsonNode GetSchemaRef<T>(Reflector reflector)
            => GetSchemaRef(reflector, typeof(T));

        /// <summary>
        /// Generates a comprehensive JSON Schema representation for the specified type.
        /// This method provides flexible schema generation supporting both full schema definitions
        /// and compact reference schemas, with intelligent handling of primitive vs complex types.
        ///
        /// Schema Generation Features:
        /// - Type unwrapping: Automatically handles nullable types by unwrapping to underlying type
        /// - Converter integration: Leverages registered JsonConverter implementations for custom schema logic
        /// - Reference optimization: Generates compact $ref schemas for complex types when justRef=true
        /// - Documentation extraction: Includes descriptions from DescriptionAttribute and type metadata
        /// - Error handling: Provides detailed error information for schema generation failures
        /// - Primitive handling: Optimizes schema generation for built-in types
        ///
        /// Schema Modes:
        /// - Full schema (justRef=false): Complete schema definition with all properties and nested types
        /// - Reference schema (justRef=true): Compact $ref pointing to type definition in $defs
        /// - Primitive inline: Primitive types are always inlined regardless of justRef setting
        ///
        /// Generated schemas conform to JSON Schema Draft 2020-12 specification.
        /// </summary>
        /// <param name="reflector">The Reflector instance used for type analysis and converter access.</param>
        /// <param name="type">The Type for which to generate the JSON Schema.</param>
        /// <returns>A JsonNode containing the JSON Schema representation of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when schema generation fails for the specified type.</exception>
        public JsonNode GetSchema(Reflector reflector, Type type, JsonObject? defines = null)
        {
            // Handle nullable types
            type = Nullable.GetUnderlyingType(type) ?? type;
            var schema = default(JsonNode);
            var typeId = type.GetSchemaTypeId();

            try
            {
                var definesNeeded = defines == null;
                defines ??= new JsonObject();

                var defineContainsType = defines.ContainsKey(typeId);

                var jsonConverter = reflector.JsonSerializerOptions.GetConverter(type);
                if (jsonConverter is IJsonSchemaConverter schemeConvertor)
                {
                    // Add placeholder to prevent infinite recursion
                    if (definesNeeded && !defineContainsType)
                        defines[typeId] = new JsonObject { [Type] = Object };

                    schema = schemeConvertor.GetSchema();

                    if (definesNeeded && !defineContainsType)
                        defines[typeId] = schema;

                    foreach (var defType in schemeConvertor.GetDefinedTypes())
                    {
                        var defTypeId = defType.GetSchemaTypeId();
                        if (defines.ContainsKey(defTypeId))
                            continue;

                        // Add placeholder to prevent infinite recursion
                        defines[defTypeId] = new JsonObject { [Type] = Object };

                        var def = GetSchema(reflector, defType, defines);
                        defines[defTypeId] = def;
                    }
                }
                else
                {
                    // Add placeholder to prevent infinite recursion
                    // if (definesNeeded && !defineContainsType)
                    //     defines[typeId] = new JsonObject { [Type] = Object };

                    schema = GenerateSchemaFromType(reflector, type, defines);

                    // if (definesNeeded && !defineContainsType)
                    //     defines[typeId] = schema;

                    if (definesNeeded && defines.Count > 0)
                        schema[Defs] = defines;
                }

                // Get description from the type if available
                var description = TypeUtils.GetDescription(type);
                if (!string.IsNullOrEmpty(description))
                    schema[Description] = JsonValue.Create(description);
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

            if (schema is not JsonObject)
            {
                return new JsonObject()
                {
                    [Error] = $"Unexpected schema type for '{type.GetTypeName(pretty: false)}'. Json Schema type: {schema.GetType().GetTypeName()}"
                };
            }
            return schema;
        }

        /// <summary>
        /// Generates a comprehensive JSON Schema representation for the specified type.
        /// This method provides flexible schema generation supporting both full schema definitions
        /// and compact reference schemas, with intelligent handling of primitive vs complex types.
        ///
        /// Schema Generation Features:
        /// - Type unwrapping: Automatically handles nullable types by unwrapping to underlying type
        /// - Converter integration: Leverages registered JsonConverter implementations for custom schema logic
        /// - Reference optimization: Generates compact $ref schemas for complex types when justRef=true
        /// - Documentation extraction: Includes descriptions from DescriptionAttribute and type metadata
        /// - Error handling: Provides detailed error information for schema generation failures
        /// - Primitive handling: Optimizes schema generation for built-in types
        ///
        /// Schema Modes:
        /// - Full schema (justRef=false): Complete schema definition with all properties and nested types
        /// - Reference schema (justRef=true): Compact $ref pointing to type definition in $defs
        /// - Primitive inline: Primitive types are always inlined regardless of justRef setting
        ///
        /// Generated schemas conform to JSON Schema Draft 2020-12 specification.
        /// </summary>
        /// <param name="reflector">The Reflector instance used for type analysis and converter access.</param>
        /// <param name="type">The Type for which to generate the JSON Schema.</param>
        /// <returns>A JsonNode containing the JSON Schema representation of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when schema generation fails for the specified type.</exception>
        public JsonNode GetSchemaRef(Reflector reflector, Type type)
        {
            // Handle nullable types
            type = Nullable.GetUnderlyingType(type) ?? type;
            var schema = default(JsonNode);

            try
            {
                var jsonConverter = reflector.JsonSerializerOptions.GetConverter(type);
                if (jsonConverter is IJsonSchemaConverter schemeConvertor)
                {
                    schema = schemeConvertor.GetSchemaRef();
                }
                else
                {
                    var typeId = type.GetSchemaTypeId();
                    // If justRef is true and the type is not primitive, we return a reference schema
                    schema = new JsonObject
                    {
                        [Ref] = RefValue + typeId
                    };
                }

                // Get description from the type if available
                var description = TypeUtils.GetDescription(type);
                if (!string.IsNullOrEmpty(description))
                    schema[Description] = JsonValue.Create(description);
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

            if (schema is not JsonObject)
            {
                return new JsonObject()
                {
                    [Error] = $"Unexpected schema type for '{type.GetTypeName(pretty: false)}'. Json Schema type: {schema.GetType().GetTypeName()}"
                };
            }
            return schema;
        }


        /// <summary>
        /// Recursively collects all nested non-primitive types from a given type and adds them to the definitions.
        /// This method traverses through properties, fields, generic arguments, and collection item types
        /// to find all types that need to be included in the $defs section.
        /// </summary>
        /// <param name="reflector">The Reflector instance used for type analysis.</param>
        /// <param name="type">The type to analyze for nested types.</param>
        /// <param name="defines">The JsonObject to accumulate type definitions.</param>
        /// <param name="visitedTypes">Set of already visited types to prevent infinite recursion.</param>
        void CollectNestedTypes(Reflector reflector, Type type, HashSet<Type>? visitedTypes = null)
        {
            visitedTypes ??= new HashSet<Type>();

            // Avoid infinite recursion
            if (visitedTypes.Contains(type))
                return;

            visitedTypes.Add(type);

            // Skip primitive types
            if (TypeUtils.IsPrimitive(type))
                return;

            // Handle generic type arguments (e.g., List<T>, Dictionary<K,V>)
            foreach (var genericArgument in TypeUtils.GetGenericTypes(type))
                CollectNestedTypes(reflector, genericArgument, visitedTypes);

            // Handle collection item types (e.g., T[], List<T>, IEnumerable<T>)
            if (TypeUtils.IsIEnumerable(type))
            {
                var itemType = TypeUtils.GetEnumerableItemType(type);
                if (itemType != null)
                    CollectNestedTypes(reflector, itemType, visitedTypes);
            }

            // Handle properties and fields to find nested types
            var properties = reflector.GetSerializableProperties(type);
            if (properties != null)
            {
                foreach (var prop in properties)
                    CollectNestedTypes(reflector, prop.PropertyType, visitedTypes);
            }

            var fields = reflector.GetSerializableFields(type);
            if (fields != null)
            {
                foreach (var field in fields)
                    CollectNestedTypes(reflector, field.FieldType, visitedTypes);
            }
        }

        /// <summary>
        /// Generates a comprehensive JSON Schema for method parameters, creating schemas suitable for
        /// dynamic method invocation, API documentation, form generation, and parameter validation.
        /// This method analyzes method signatures and produces schemas that enable type-safe parameter
        /// binding in dynamic execution environments.
        ///
        /// Schema Structure:
        /// - Root object schema with "properties" containing each parameter
        /// - "required" array listing parameters without default values
        /// - "$defs" section containing complex type definitions to avoid duplication
        /// - Parameter descriptions extracted from DescriptionAttribute annotations
        /// - Proper JSON Schema Draft 2020-12 compliance
        ///
        /// Parameter Analysis:
        /// - Name extraction: Uses parameter names for schema property keys
        /// - Type resolution: Generates appropriate schemas for each parameter type
        /// - Default value detection: Automatically determines required vs optional parameters
        /// - Documentation: Extracts descriptions from method parameter attributes
        /// - Generic handling: Recursively processes generic type arguments
        ///
        /// Advanced Features:
        /// - Primitive optimization: Inline primitive type schemas for better performance
        /// - Complex type deduplication: Uses $defs to avoid schema repetition
        /// - Recursive type support: Handles nested complex types and their dependencies
        /// - Generic type expansion: Properly handles generic method parameters and constraints
        ///
        /// Use Cases:
        /// - API documentation generation
        /// - Dynamic form creation for method invocation
        /// - Parameter validation in scripting scenarios
        /// - IDE tooling and intellisense support
        /// - Code generation and template systems
        /// </summary>
        /// <param name="reflector">The Reflector instance used for type analysis and schema generation.</param>
        /// <param name="method">The MethodInfo for which to generate the parameter schema.</param>
        /// <param name="justRef">Whether to use compact references for complex types in parameter schemas. Default is false.</param>
        /// <returns>A JsonNode containing the complete JSON Schema for the method's parameters.</returns>
        /// <exception cref="ArgumentNullException">Thrown when method parameter is null.</exception>
        public JsonNode GetArgumentsSchema(Reflector reflector, MethodInfo method, bool justRef = false, JsonObject? defines = null)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return new JsonObject { [Type] = Object };

            var types = parameters
                .Select(p => (
                    type: p.ParameterType,
                    name: p.Name ?? throw new InvalidOperationException($"Parameter in method '{method.Name}' has no name."),
                    description: TypeUtils.GetDescription(p),
                    required: !p.HasDefaultValue));

            return GenerateSchema(reflector, types, justRef, defines);
        }

        /// <summary>
        /// Generates a comprehensive JSON Schema for the return type of a method.
        /// This method analyzes method return types and produces schemas that describe the structure
        /// of the returned data, enabling type-safe result handling in dynamic execution environments.
        ///
        /// Schema Structure:
        /// - Root object schema with "properties" containing the "result" property
        /// - "required" array listing the "result" property (for non-void returns)
        /// - "$defs" section containing complex type definitions to avoid duplication
        /// - Return type wrapped as a property named "result"
        /// - Proper JSON Schema Draft 2020-12 compliance
        ///
        /// Schema Generation Features:
        /// - Async unwrapping: Automatically unwraps Task&lt;T&gt; and ValueTask&lt;T&gt; to return schema for T
        /// - Void handling: Returns null for void, Task, and ValueTask (methods with no return value)
        /// - Type resolution: Generates appropriate schemas for complex return types
        /// - Reference optimization: Supports both full schema definitions and compact $ref references
        /// - Documentation: Extracts descriptions from return type attributes
        /// - Proper $defs handling: Complex types are placed in $defs section with $ref references
        ///
        /// Return Type Handling:
        /// - void → null (no return value)
        /// - Task → null (async method with no return value)
        /// - ValueTask → null (async method with no return value)
        /// - Task&lt;string&gt; → object schema with "result" property of type string (unwrapped)
        /// - ValueTask&lt;int&gt; → object schema with "result" property of type int (unwrapped)
        /// - Any other type → object schema with "result" property of that type with $defs support
        ///
        /// Use Cases:
        /// - API documentation generation for response schemas
        /// - Dynamic result type validation
        /// - Code generation and template systems
        /// - AI-driven method understanding and invocation
        /// </summary>
        /// <param name="reflector">The Reflector instance used for type analysis and schema generation.</param>
        /// <param name="methodInfo">The MethodInfo for which to generate the return type schema.</param>
        /// <param name="justRef">Whether to use compact references for complex types. Default is false.</param>
        /// <returns>A JsonNode containing the JSON Schema for the method's return type, or null for void/Task/ValueTask.</returns>
        /// <exception cref="ArgumentNullException">Thrown when methodInfo parameter is null.</exception>
        public JsonNode? GetReturnSchema(Reflector reflector, MethodInfo methodInfo, bool justRef = false, JsonObject? defines = null)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            var returnType = methodInfo.ReturnType;

            // Handle void, Task, and ValueTask - these have no return value
            if (returnType == typeof(void) ||
                returnType == typeof(Task) ||
                returnType == typeof(ValueTask))
                return null;

            // Check if return type is Task<T> or ValueTask<T>
            var isAsyncWrapper = returnType.IsGenericType &&
                (returnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                 returnType.GetGenericTypeDefinition() == typeof(ValueTask<>));

            // Unwrap Task<T>/ValueTask<T> to get the inner T
            var unwrappedType = isAsyncWrapper
                ? returnType.GetGenericArguments()[0]
                : returnType;

            var isNullable = false;

            // Check for Nullable<T> (value types like int?, DateTime?)
            var nullableUnderlyingType = Nullable.GetUnderlyingType(unwrappedType);
            if (nullableUnderlyingType != null)
            {
                // This handles Nullable<T> for value types (e.g., Task<int?>, int?)
                isNullable = true;
                unwrappedType = nullableUnderlyingType;
            }
            else if (!unwrappedType.IsValueType)
            {
                // For reference types, check nullability using NullabilityInfoContext
#if NET5_0_OR_GREATER
                try
                {
                    var nullabilityContext = new NullabilityInfoContext();
                    var nullabilityInfo = nullabilityContext.Create(methodInfo.ReturnParameter);

                    isNullable = nullabilityInfo.ReadState == NullabilityState.Nullable;
                    // If the return type is Task<T> or ValueTask<T>, check the generic argument nullability
                    if (isAsyncWrapper)
                    {
                        // Check the nullability of the T inside Task<T> or ValueTask<T>
                        if (nullabilityInfo.GenericTypeArguments.Length > 0)
                            isNullable |= nullabilityInfo.GenericTypeArguments[0].ReadState == NullabilityState.Nullable;
                    }
                }
                catch (Exception)
                {
                    // If we can't determine nullability, assume not nullable
                    isNullable = false;
                }
#else
                // For .NET Standard 2.1 and earlier, we cannot determine reference type nullability
                // Assume not nullable by default
                isNullable = false;
#endif
            }

            var types = new (Type type, string name, string? description, bool required)[]
            {
                (
                    type: unwrappedType,
                    name: Result,
                    description: null,
                    required: isNullable == false
                )
            };
            return GenerateSchema(reflector, types, justRef, defines);
        }

        /// <summary>
        /// Generates a JSON Schema for a collection of types, typically representing method parameters.
        /// This method constructs a schema object with properties for each type, handling required fields,
        /// </summary>
        /// <param name="reflector"></param>
        /// <param name="types"></param>
        /// <param name="justRef"></param>
        /// <param name="defines">If it is not null, it will be used to fill new definitions without injecting it into the schema. Needed for recursive call case, when need to generate a single top level definitions object.</param>
        /// <returns></returns>
        public JsonNode GenerateSchema(
            Reflector reflector,
            IEnumerable<(Type type, string name, string? description, bool required)> types,
            bool justRef = false,
            JsonObject? defines = null)
        {
            var needToAddDefines = defines == null;
            defines ??= new();

            var properties = new JsonObject();
            var required = new JsonArray();

            // Create a schema object manually
            var schema = new JsonObject
            {
                // [SchemaDraft] = JsonValue.Create(SchemaDraftValue),
                [Type] = Object,
                [Properties] = properties
            };

            foreach (var parameter in types)
            {
                var parameterSchema = default(JsonNode);
                var isPrimitive = TypeUtils.IsPrimitive(parameter.type);
                if (isPrimitive)
                {
                    parameterSchema = GetSchema(reflector, parameter.type, defines: defines);
                }
                else
                {
                    parameterSchema = GetSchemaRef(reflector, parameter.type);

                    var typeId = parameter.type.GetSchemaTypeId();
                    if (!defines.ContainsKey(typeId))
                    {
                        var fullSchema = GetSchema(reflector, parameter.type, defines: defines);
                        if (fullSchema == null)
                            continue;
                        defines[typeId] = fullSchema;
                    }
                }

                if (parameterSchema == null)
                    continue;

                properties[parameter.name!] = parameterSchema;

                if (parameterSchema is JsonObject parameterSchemaObject)
                {
                    var propertyDescription = parameter.description;
                    if (!string.IsNullOrEmpty(propertyDescription))
                        parameterSchemaObject[Description] = JsonValue.Create(propertyDescription);
                }

                // Check if the parameter has a default value
                if (parameter.required)
                    required.Add(parameter.name!);

                // Add generic type parameters recursively if any
                foreach (var genericArgument in TypeUtils.GetGenericTypes(parameter.type))
                {
                    if (TypeUtils.IsPrimitive(genericArgument))
                        continue;

                    var typeId = genericArgument.GetSchemaTypeId();
                    if (defines.ContainsKey(typeId))
                        continue;

                    var genericSchema = GetSchema(reflector, genericArgument, defines: defines);
                    if (genericSchema != null)
                        defines[typeId] = genericSchema;
                }
            }

            if (defines.Count > 0 && needToAddDefines)
                schema[Defs] = defines;
            if (required.Count > 0)
                schema[Required] = required;

            return schema;
        }
    }
}