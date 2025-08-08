using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.ReflectorNet
{
    using JsonSerializer = com.IvanMurzak.ReflectorNet.Utils.JsonSerializer;

    public partial class Reflector
    {
        readonly JsonSerializer jsonSerializer;
        readonly JsonSchema jsonSchema = new();

        public JsonSerializerOptions JsonSerializerOptions => jsonSerializer.JsonSerializerOptions;
        public JsonSerializer JsonSerializer => jsonSerializer;
        public JsonSchema JsonSchema => jsonSchema;

        /// <summary>
        /// Generates a JSON Schema representation for the specified generic type parameter.
        /// This method provides comprehensive schema generation supporting both simple references
        /// and full schema definitions with proper type metadata and documentation.
        ///
        /// Behavior:
        /// - Type resolution: Automatically handles nullable types by unwrapping to underlying type
        /// - Reference mode: When justRef=true, generates compact $ref schemas for non-primitive types
        /// - Full schema mode: When justRef=false, generates complete schema definitions with properties
        /// - Primitive optimization: Generates inline schemas for primitive types regardless of justRef setting
        /// - Documentation extraction: Includes descriptions from DescriptionAttribute and XML documentation
        /// - Recursive handling: Manages complex nested types and generic type parameters
        /// - Error handling: Provides detailed error information for schema generation failures
        /// </summary>
        /// <typeparam name="T">The type for which to generate the JSON Schema.</typeparam>
        /// <param name="justRef">Whether to generate a compact reference schema (true) or full schema definition (false). Default is false.</param>
        /// <returns>A JsonNode containing the JSON Schema representation of the specified type.</returns>
        public JsonNode GetSchema<T>(bool justRef = false)
            => jsonSchema.GetSchema<T>(this, justRef);

        /// <summary>
        /// Generates a JSON Schema representation for the specified type.
        /// This method provides comprehensive schema generation supporting both simple references
        /// and full schema definitions with proper type metadata and documentation.
        ///
        /// Behavior:
        /// - Type resolution: Automatically handles nullable types by unwrapping to underlying type
        /// - Reference mode: When justRef=true, generates compact $ref schemas for non-primitive types
        /// - Full schema mode: When justRef=false, generates complete schema definitions with properties
        /// - Primitive optimization: Generates inline schemas for primitive types regardless of justRef setting
        /// - Documentation extraction: Includes descriptions from DescriptionAttribute and XML documentation
        /// - Recursive handling: Manages complex nested types and generic type parameters
        /// - Error handling: Provides detailed error information for schema generation failures
        /// </summary>
        /// <param name="type">The Type for which to generate the JSON Schema.</param>
        /// <param name="justRef">Whether to generate a compact reference schema (true) or full schema definition (false). Default is false.</param>
        /// <returns>A JsonNode containing the JSON Schema representation of the specified type.</returns>
        public JsonNode GetSchema(Type type, bool justRef = false)
            => jsonSchema.GetSchema(this, type, justRef);

        /// <summary>
        /// Generates a comprehensive JSON Schema for method parameters, enabling dynamic method invocation
        /// and parameter validation scenarios. This method creates schemas suitable for form generation,
        /// API documentation, and parameter validation in dynamic execution environments.
        ///
        /// Behavior:
        /// - Parameter analysis: Examines all method parameters including names, types, and default values
        /// - Schema structure: Creates object schema with properties for each parameter
        /// - Required fields: Automatically determines required vs optional parameters based on default values
        /// - Type definitions: Includes $defs section for complex types to avoid duplication
        /// - Documentation: Extracts parameter descriptions from DescriptionAttribute annotations
        /// - Generic support: Handles generic method parameters and their type constraints
        /// - Recursive schemas: Properly handles nested complex types and generic type arguments
        /// - Validation support: Includes appropriate validation constraints for parameter types
        ///
        /// The generated schema follows JSON Schema Draft 2020-12 specification and includes:
        /// - Object schema with properties for each parameter
        /// - Required array listing mandatory parameters
        /// - $defs section containing complex type definitions
        /// - Description annotations from method parameter attributes
        /// </summary>
        /// <param name="methodInfo">The MethodInfo for which to generate the parameter schema.</param>
        /// <param name="justRef">Whether to use compact references for complex types. Default is false.</param>
        /// <returns>A JsonNode containing the complete JSON Schema for the method's parameters.</returns>
        public JsonNode GetArgumentsSchema(MethodInfo methodInfo, bool justRef = false)
            => jsonSchema.GetArgumentsSchema(this, methodInfo, justRef);
    }
}
