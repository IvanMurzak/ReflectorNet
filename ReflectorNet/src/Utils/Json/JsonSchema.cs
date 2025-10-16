/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.ComponentModel;
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
        /// <param name="justRef">Whether to generate a compact reference schema for non-primitive types. Default is false.</param>
        /// <returns>A JsonNode containing the JSON Schema representation of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when schema generation fails for the specified type.</exception>
        public JsonNode GetSchema<T>(Reflector reflector, bool justRef = false)
            => GetSchema(reflector, typeof(T), justRef);

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
        /// <param name="justRef">Whether to generate a compact reference schema for non-primitive types. Default is false.</param>
        /// <returns>A JsonNode containing the JSON Schema representation of the specified type.</returns>
        /// <exception cref="InvalidOperationException">Thrown when schema generation fails for the specified type.</exception>
        public JsonNode GetSchema(Reflector reflector, Type type, bool justRef = false)
        {
            // Handle nullable types
            type = Nullable.GetUnderlyingType(type) ?? type;

            var schema = default(JsonNode);

            try
            {
                var jsonConverter = reflector.JsonSerializerOptions.GetConverter(type);
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
                        // If not justRef, we generate the full schema
                        schema = GenerateSchemaFromType(reflector, type);

                        // Get description from the type if available
                        var description = TypeUtils.GetDescription(type);
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
        /// <summary>
        /// Generates a JSON Schema with proper $defs handling for complex types.
        /// This method handles type deduplication by placing complex type definitions in the $defs section
        /// and using $ref references to avoid schema repetition.
        /// </summary>
        /// <param name="reflector">The Reflector instance used for type analysis and schema generation.</param>
        /// <param name="type">The Type for which to generate the schema.</param>
        /// <param name="justRef">Whether to use compact references for complex types. Default is false.</param>
        /// <param name="defines">Optional JsonObject to accumulate type definitions. If null, a new object is created.</param>
        /// <returns>A tuple containing the generated schema and the definitions object.</returns>
        private (JsonNode schema, JsonObject? defines) GenerateSchemaWithDefs(Reflector reflector, Type type, bool justRef = false, JsonObject? defines = null)
        {
            var isPrimitive = TypeUtils.IsPrimitive(type);
            JsonNode schema;

            if (isPrimitive)
            {
                schema = GetSchema(reflector, type, justRef: justRef);
            }
            else
            {
                schema = GetSchema(reflector, type, justRef: true);
                var typeId = type.GetTypeId();

                defines ??= new JsonObject();

                if (!defines.ContainsKey(typeId))
                {
                    var fullSchema = GetSchema(reflector, type, justRef: false);
                    defines[typeId] = fullSchema;
                }

                // Add generic type parameters recursively if any
                foreach (var genericArgument in TypeUtils.GetGenericTypes(type))
                {
                    if (TypeUtils.IsPrimitive(genericArgument))
                        continue;

                    var genericTypeId = genericArgument.GetTypeId();
                    if (defines.ContainsKey(genericTypeId))
                        continue;

                    var genericSchema = GetSchema(reflector, genericArgument, justRef: false);
                    if (genericSchema != null)
                        defines[genericTypeId] = genericSchema;
                }
            }

            return (schema, defines);
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
        public JsonNode GetArgumentsSchema(Reflector reflector, MethodInfo method, bool justRef = false)
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
                // [SchemaDraft] = JsonValue.Create(SchemaDraftValue),
                [Type] = Object,
                [Properties] = properties,
                [Required] = required
            };

            foreach (var parameter in parameters)
            {
                var (parameterSchema, updatedDefines) = GenerateSchemaWithDefs(reflector, parameter.ParameterType, justRef, defines);
                defines = updatedDefines;

                if (parameterSchema == null)
                    continue;

                properties[parameter.Name!] = parameterSchema;

                if (parameterSchema is JsonObject parameterSchemaObject)
                {
                    var propertyDescription = TypeUtils.GetDescription(parameter);
                    if (!string.IsNullOrEmpty(propertyDescription))
                        parameterSchemaObject[Description] = JsonValue.Create(propertyDescription);
                }

                // Check if the parameter has a default value
                if (!parameter.HasDefaultValue)
                    required.Add(parameter.Name!);
            }

            if (defines != null && defines.Count > 0)
                schema[Defs] = defines;
            return schema;
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
        public JsonNode? GetReturnSchema(Reflector reflector, MethodInfo methodInfo, bool justRef = false)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            var returnType = methodInfo.ReturnType;
            var unwrappedType = Nullable.GetUnderlyingType(returnType);
            var isNullable = unwrappedType != null;

            unwrappedType ??= returnType;

            // Handle void, Task, and ValueTask - these have no return value
            if (unwrappedType == typeof(void) ||
                unwrappedType == typeof(Task) ||
                unwrappedType == typeof(ValueTask))
                return null;

            // Unwrap Task<T> and ValueTask<T> to get the actual return type T
            if (unwrappedType.IsGenericType)
            {
                var genericDefinition = unwrappedType.GetGenericTypeDefinition();
                if (genericDefinition == typeof(Task<>) || genericDefinition == typeof(ValueTask<>))
                {
                    var taskGenericArg = unwrappedType.GetGenericArguments()[0];

                    // Check if T in Task<T> is nullable (e.g., Task<int?> or Task<string?>)
                    var nullableUnderlyingType = Nullable.GetUnderlyingType(taskGenericArg);
                    if (nullableUnderlyingType != null)
                    {
                        // T is a nullable value type (e.g., int?, bool?)
                        isNullable = true;
                        unwrappedType = nullableUnderlyingType;
                    }
                    else
                    {
                        unwrappedType = taskGenericArg;
                    }
                }
            }

            // Check for nullable reference types using NullabilityInfoContext
            if (!isNullable && (unwrappedType.IsClass || unwrappedType.IsInterface || unwrappedType.IsArray))
            {
                try
                {
#if NET5_0_OR_GREATER
                    var nullabilityContext = new System.Reflection.NullabilityInfoContext();
                    var nullabilityInfo = nullabilityContext.Create(methodInfo.ReturnParameter);

                    // For Task<T> or ValueTask<T>, check the generic argument's nullability
                    if (returnType.IsGenericType)
                    {
                        var genericDef = returnType.GetGenericTypeDefinition();
                        if (genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>))
                        {
                            isNullable = nullabilityInfo.GenericTypeArguments.Length > 0 &&
                                        nullabilityInfo.GenericTypeArguments[0].ReadState == System.Reflection.NullabilityState.Nullable;
                        }
                        else
                        {
                            isNullable = nullabilityInfo.ReadState == System.Reflection.NullabilityState.Nullable;
                        }
                    }
                    else
                    {
                        isNullable = nullabilityInfo.ReadState == System.Reflection.NullabilityState.Nullable;
                    }
#endif
                }
                catch
                {
                    // If we can't determine nullability, assume not nullable
                }
            }

            // Generate schema for the return type using the shared method
            var (resultSchema, defines) = GenerateSchemaWithDefs(reflector, unwrappedType, justRef);

            // Create the root schema object in the same format as GetArgumentsSchema
            var schema = new JsonObject { [Type] = Object };

            if (resultSchema != null)
            {
                schema[Properties] = new JsonObject { [Result] = resultSchema };

                if (!isNullable)
                    schema[Required] = new JsonArray { Result };
            }

            if (defines != null && defines.Count > 0)
                schema[Defs] = defines;

            return schema;
        }
    }
}