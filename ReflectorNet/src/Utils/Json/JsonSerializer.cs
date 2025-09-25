/*
 * ReflectorNet
 * Author: Ivan Murzak (https://github.com/IvanMurzak)
 * Copyright (c) 2025 Ivan Murzak
 * Licensed under the Apache License, Version 2.0. See LICENSE file in the project root for full license information.
 */

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using com.IvanMurzak.ReflectorNet.Json;

namespace com.IvanMurzak.ReflectorNet.Utils
{
    /// <summary>
    /// Provides a comprehensive JSON serialization and deserialization framework specifically optimized
    /// for ReflectorNet's reflection-based operations and dynamic type handling scenarios.
    /// This class wraps and extends .NET's System.Text.Json functionality with ReflectorNet-specific
    /// converters and configuration optimized for AI and dynamic programming environments.
    ///
    /// Key Features:
    /// - ReflectorNet-optimized configuration with custom converters for specialized types
    /// - Intelligent null handling and camelCase naming for cross-language compatibility
    /// - Comprehensive type support including SerializedMember, MethodData, and reflection types
    /// - Integration with Reflector's default value system for robust deserialization
    /// - Support for both streaming and direct serialization/deserialization operations
    ///
    /// Architecture:
    /// - Wraps JsonSerializerOptions with ReflectorNet-specific configuration
    /// - Provides strongly-typed and dynamic serialization methods
    /// - Integrates with Reflector instance for default value generation
    /// - Supports runtime converter registration for extensibility
    ///
    /// This class serves as the primary JSON processing engine for ReflectorNet's serialization
    /// system and is designed to handle complex scenarios involving reflection, dynamic types,
    /// and cross-language serialization requirements common in AI and automation scenarios.
    /// </summary>
    public partial class JsonSerializer
    {
        readonly JsonSerializerOptions jsonSerializerOptions;

        public JsonSerializerOptions JsonSerializerOptions => jsonSerializerOptions;

        /// <summary>
        /// Initializes a new JsonSerializer instance with ReflectorNet-optimized configuration and custom converters.
        /// This constructor sets up a comprehensive JSON serialization environment specifically tuned for
        /// reflection-based operations, dynamic type handling, and ReflectorNet's serialization model.
        ///
        /// Configuration Features:
        /// - Null handling: Configured to ignore null values during serialization for cleaner output
        /// - Naming policy: Uses camelCase property naming for JavaScript/JSON compatibility
        /// - Formatting: Enables indented output for better readability and debugging
        /// - Enum handling: Automatically converts enums to string representations
        /// - Custom converters: Includes specialized converters for ReflectorNet types
        ///
        /// Included Custom Converters:
        /// - JsonStringEnumConverter: Serializes enums as strings instead of numbers
        /// - MethodDataConverter: Handles MethodData serialization/deserialization
        /// - MethodInfoConverter: Manages MethodInfo serialization for reflection scenarios
        /// - SerializedMemberConverter: Core converter for SerializedMember objects
        /// - SerializedMemberListConverter: Handles collections of SerializedMember objects
        ///
        /// The configuration is optimized for ReflectorNet's use cases including dynamic method
        /// invocation, object introspection, and cross-language serialization scenarios.
        /// </summary>
        /// <param name="reflector">The Reflector instance that this JsonSerializer will be associated with.</param>
        /// <exception cref="ArgumentNullException">Thrown when reflector parameter is null.</exception>
        public JsonSerializer(Reflector reflector)
        {
            if (reflector == null)
                throw new ArgumentNullException(nameof(reflector));

            // Add custom converters if needed
            jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Ignore 'null' field and properties
                // DefaultIgnoreCondition = JsonIgnoreCondition.Never, // Include 'null' fields and properties
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                //ReferenceHandler = ReferenceHandler.Preserve,
                WriteIndented = true,
                TypeInfoResolver = JsonTypeInfoResolver.Combine(
                new DefaultJsonTypeInfoResolver()
            ),
                Converters =
                {
                    // Individual converters (temporarily disabled)
                    // new PrimitiveJsonConverter(),
                    // new BoolJsonConverter(),
                    // new EnumJsonConverter(),
                    // new DateTimeJsonConverter(),
                    // new DateTimeOffsetJsonConverter(),
                    // new DecimalJsonConverter(),
                    // new GuidJsonConverter(),
                    // new TimeSpanJsonConverter(),

                    // Original monolithic converter
                    new StringToPrimitiveConverter(),

                    // new JsonStringEnumConverter(),
                    new MethodDataConverter(),
                    new MethodInfoConverter(),
                    new SerializedMemberConverter(reflector),
                    new SerializedMemberListConverter(reflector)
                }
            };
        }

        /// <summary>
        /// Adds a custom JsonConverter to the serializer's converter collection, enabling specialized
        /// serialization and deserialization logic for specific types. This method allows for runtime
        /// extension of the serializer's capabilities to handle custom types or override default behavior.
        ///
        /// Use Cases:
        /// - Custom type serialization: Handle types that require special serialization logic
        /// - Third-party integrations: Add converters for external library types
        /// - Performance optimization: Implement optimized converters for frequently used types
        /// - Protocol compliance: Ensure serialization matches specific API or protocol requirements
        /// - Legacy support: Handle older serialization formats or deprecated type representations
        /// </summary>
        /// <param name="converter">The JsonConverter to add to the converter collection.</param>
        /// <exception cref="ArgumentNullException">Thrown when converter parameter is null.</exception>
        public void AddConverter(JsonConverter converter)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            jsonSerializerOptions.Converters.Add(converter);
        }

        /// <summary>
        /// Removes all custom converters from the serializer, reverting to default .NET JSON serialization behavior.
        /// This method is useful for scenarios where you need to reset the serializer to a clean state or
        /// when troubleshooting converter-related issues.
        ///
        /// Warning: This operation removes all converters including the ReflectorNet-specific converters
        /// that are essential for proper SerializedMember and MethodData handling. Use with caution.
        /// </summary>
        public void ClearConverters()
        {
            jsonSerializerOptions.Converters.Clear();
        }

        /// <summary>
        /// Serializes an object to a JSON string representation using the configured JsonSerializerOptions.
        /// This method provides comprehensive serialization with support for complex types, custom converters,
        /// and ReflectorNet-specific serialization logic.
        ///
        /// Features:
        /// - Null safety: Properly handles null input values
        /// - Custom converter support: Uses registered converters for specialized types
        /// - Formatting: Produces indented JSON for better readability
        /// - Type preservation: Maintains type information where applicable
        /// - Enum handling: Converts enums to string representations
        /// </summary>
        /// <param name="data">The object to serialize. Can be null.</param>
        /// <param name="options">Optional JsonSerializerOptions to override default settings. If null, uses instance configuration.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public string Serialize(object? data, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Serialize(
                value: data,
                options: options ?? jsonSerializerOptions);

        /// <summary>
        /// Serializes an object to a JsonElement representation, providing a structured JSON DOM
        /// for programmatic manipulation and analysis. This method is useful when you need to
        /// work with the JSON structure directly rather than string representation.
        /// </summary>
        /// <param name="data">The object to serialize.</param>
        /// <param name="options">Optional JsonSerializerOptions to override default settings. If null, uses instance configuration.</param>
        /// <returns>A JsonElement containing the serialized object structure.</returns>
        public JsonElement SerializeToElement(object data, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.SerializeToElement(data, options ?? jsonSerializerOptions);

        /// <summary>
        /// Deserializes a JSON string to the specified generic type with comprehensive error handling
        /// and type safety. This method provides strongly-typed deserialization with support for
        /// custom converters and ReflectorNet-specific types.
        /// </summary>
        /// <typeparam name="T">The target type for deserialization.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="options">Optional JsonSerializerOptions to override default settings. If null, uses instance configuration.</param>
        /// <returns>The deserialized object of type T, or null if deserialization fails or JSON represents null.</returns>
        public T? Deserialize<T>(string json, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Deserialize<T>(
                json: json,
                options: options ?? jsonSerializerOptions);

        /// <summary>
        /// Deserializes a JsonElement to the specified generic type with intelligent null handling
        /// and default value generation. This method integrates with Reflector's default value
        /// system to provide appropriate fallbacks when JsonElement is null or invalid.
        /// </summary>
        /// <typeparam name="T">The target type for deserialization.</typeparam>
        /// <param name="reflector">The Reflector instance used for default value generation.</param>
        /// <param name="jsonElement">The JsonElement to deserialize. Can be null.</param>
        /// <param name="options">Optional JsonSerializerOptions to override default settings. If null, uses instance configuration.</param>
        /// <returns>The deserialized object of type T, or the default value for T if JsonElement is null.</returns>
        public T? Deserialize<T>(Reflector reflector, JsonElement? jsonElement, JsonSerializerOptions? options = null)
            => jsonElement.HasValue
                ? System.Text.Json.JsonSerializer.Deserialize<T>(
                    element: jsonElement.Value,
                    options: options ?? jsonSerializerOptions)
                : reflector.GetDefaultValue<T>();

        /// <summary>
        /// Deserializes a JsonElement to the specified type with intelligent null handling
        /// and default value generation. This method provides dynamic type deserialization
        /// with integration to Reflector's type system and default value generation.
        /// </summary>
        /// <param name="reflector">The Reflector instance used for default value generation.</param>
        /// <param name="jsonElement">The JsonElement to deserialize. Can be null.</param>
        /// <param name="type">The target Type for deserialization.</param>
        /// <param name="options">Optional JsonSerializerOptions to override default settings. If null, uses instance configuration.</param>
        /// <returns>The deserialized object of the specified type, or the default value for the type if JsonElement is null.</returns>
        public object? Deserialize(Reflector reflector, JsonElement? jsonElement, Type type, JsonSerializerOptions? options = null)
            => jsonElement.HasValue
                ? System.Text.Json.JsonSerializer.Deserialize(
                    element: jsonElement.Value,
                    returnType: type,
                    options: options ?? jsonSerializerOptions)
                : reflector.GetDefaultValue(type);

        /// <summary>
        /// Deserializes a JSON string to the specified type with dynamic type resolution.
        /// This method provides runtime type deserialization capabilities for scenarios
        /// where the target type is determined at runtime.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="returnType">The target Type for deserialization.</param>
        /// <param name="options">Optional JsonSerializerOptions to override default settings. If null, uses instance configuration.</param>
        /// <returns>The deserialized object of the specified type.</returns>
        public object? Deserialize(string json, Type returnType, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Deserialize(
                json: json,
                returnType: returnType,
                options: options ?? jsonSerializerOptions);

        /// <summary>
        /// Deserializes from a Utf8JsonReader to the specified type, providing efficient
        /// streaming deserialization for large JSON documents or performance-critical scenarios.
        /// </summary>
        /// <param name="reader">The Utf8JsonReader positioned at the JSON content to deserialize.</param>
        /// <param name="returnType">The target Type for deserialization.</param>
        /// <param name="options">Optional JsonSerializerOptions to override default settings. If null, uses instance configuration.</param>
        /// <returns>The deserialized object of the specified type.</returns>
        public object? Deserialize(ref Utf8JsonReader reader, Type returnType, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Deserialize(
                reader: ref reader,
                returnType: returnType,
                options: options ?? jsonSerializerOptions);

        /// <summary>
        /// Deserializes from a Utf8JsonReader to the specified generic type, providing efficient
        /// streaming deserialization with strong typing for large JSON documents or performance-critical scenarios.
        /// </summary>
        /// <typeparam name="TValue">The target type for deserialization.</typeparam>
        /// <param name="reader">The Utf8JsonReader positioned at the JSON content to deserialize.</param>
        /// <param name="options">Optional JsonSerializerOptions to override default settings. If null, uses instance configuration.</param>
        /// <returns>The deserialized object of type TValue.</returns>
        public TValue? Deserialize<TValue>(ref Utf8JsonReader reader, JsonSerializerOptions? options = null)
            => System.Text.Json.JsonSerializer.Deserialize<TValue>(
                reader: ref reader,
                options: options ?? jsonSerializerOptions);
    }
}