
# ReflectorNet

[![NuGet](https://img.shields.io/nuget/v/com.IvanMurzak.ReflectorNet?label=NuGet&labelColor=333A41)](https://www.nuget.org/packages/com.IvanMurzak.ReflectorNet/)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue?logoColor=white&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet)
[![netstandard2.1](https://img.shields.io/badge/.NET-netstandard2.1-blue?logoColor=white&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet)
[![Tests](https://github.com/IvanMurzak/ReflectorNet/actions/workflows/release.yml/badge.svg?branch=main)](https://github.com/IvanMurzak/ReflectorNet/actions/workflows/release.yml)

[![Stars](https://img.shields.io/github/stars/IvanMurzak/ReflectorNet 'Stars')](https://github.com/IvanMurzak/ReflectorNet/stargazers)
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/Cgs6nM8BPU)
[![License](https://img.shields.io/github/license/IvanMurzak/ReflectorNet?label=License&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

ReflectorNet is an advanced .NET reflection toolkit specifically designed for AI-driven scenarios. It provides sophisticated reflection-based serialization, deserialization, population, and method invocation capabilities that enable seamless integration between AI systems and .NET applications.

## Main Features

### 🔍 **Intelligent Method Discovery & Invocation**

Discover and invoke methods at runtime using powerful fuzzy matching algorithms. Supports partial method names, parameter matching, and configurable similarity scoring (0-6 levels) for robust method resolution even with incomplete information.

```csharp
var reflector = new Reflector();
var methods = reflector.FindMethod(new MethodRef
{
    TypeName = "TestClass",
    MethodName = "Process",  // Partial match supported
    Namespace = "MyApp.Services"
}, methodNameMatchLevel: 3); // Flexible matching
```

### 📊 **Advanced Reflection-Based Serialization**

Convert complex .NET objects to type-preserving, AI-friendly serialized formats. Supports nested objects, collections, custom types, and maintains full type information for accurate reconstruction.

```csharp
var reflector = new Reflector();
var serialized = reflector.Serialize(complexObject, recursive: true);
var restored = reflector.Deserialize<MyClass>(serialized);
```

### 🔧 **Smart Instance Creation & Population**

Intelligently create instances with automatic constructor resolution, dependency handling, and in-place object population from serialized data.

```csharp
var instance = reflector.CreateInstance<MyClass>();
reflector.TryPopulate(ref instance, serializedData);
```

### 🔄 **Advanced Object Population System**

ReflectorNet provides sophisticated in-place object population capabilities that enable seamless data transfer from serialized formats to existing object instances. The population system offers precise control over which members are populated and comprehensive error handling.

#### **Key Population Features**

##### **🎯 In-Place Population**

```csharp
// Populate existing objects without replacement
var existingObject = new MyClass { Id = 1 };
var serializedData = reflector.Serialize(sourceObject);
var success = reflector.TryPopulate(ref existingObject, serializedData);
```

##### **🔍 Selective Member Population**

```csharp
// Control which fields and properties are populated
reflector.TryPopulate(
    ref targetObject,
    serializedData,
    flags: BindingFlags.Public | BindingFlags.Instance  // Only public members
);
```

##### **📊 Hierarchical Population**

```csharp
// Population with nested object support and depth tracking
var stringBuilder = new StringBuilder();
var success = reflector.TryPopulate(
    ref complexObject,
    serializedData,
    depth: 0,
    stringBuilder: stringBuilder  // Collect detailed operation logs
);
```

##### **🛡️ Type-Safe Population with Validation**

```csharp
// Explicit type validation during population
reflector.TryPopulate(
    ref targetObject,
    serializedData,
    fallbackObjType: typeof(MyExpectedType)  // Ensure type compatibility
);
```

#### **Population Workflow**

1. **Type Resolution**: Automatically resolves target type from serialized data or explicit parameters
2. **Compatibility Validation**: Ensures object compatibility before attempting population
3. **Converter Selection**: Uses the Chain of Responsibility pattern to select optimal converters
4. **Member Population**: Populates fields and properties based on BindingFlags
5. **Error Collection**: Accumulates detailed error messages with hierarchical indentation
6. **Success Reporting**: Returns comprehensive success/failure status

#### **Advanced Population Scenarios**

##### **🔧 Partial Object Updates**

```csharp
// Update only specific fields of existing objects
var partialData = new SerializedMember
{
    fields = new List<SerializedMember>
    {
        new SerializedMember { name = "Name", valueString = "Updated Name" },
        new SerializedMember { name = "Status", valueString = "Active" }
    }
};

reflector.TryPopulate(ref existingObject, partialData);
```

##### **📈 Batch Population with Error Tracking**

```csharp
// Populate multiple objects with consolidated error reporting
var errorLog = new StringBuilder();
var overallSuccess = true;

foreach (var (target, data) in objectDataPairs)
{
    var success = reflector.TryPopulate(
        ref target,
        data,
        stringBuilder: errorLog
    );
    overallSuccess &= success;
}

if (!overallSuccess)
    Console.WriteLine($"Population errors:\n{errorLog}");
```

##### **🔄 State Synchronization**

```csharp
// Synchronize object state from external sources
public void SynchronizeFromJson(ref MyClass target, string jsonData)
{
    var serialized = JsonSerializer.Deserialize<SerializedMember>(jsonData);
    var success = reflector.TryPopulate(ref target, serialized);

    if (!success)
        throw new InvalidOperationException("Failed to synchronize object state");
}
```

##### **🧪 Configuration Management**

```csharp
// Apply configuration updates to existing settings objects
var configObject = LoadCurrentConfiguration();
var updateData = reflector.Serialize(newConfigurationData);

// Apply updates while preserving existing values for unspecified fields
reflector.TryPopulate(ref configObject, updateData);
SaveConfiguration(configObject);
```

### 📋 **Automatic JSON Schema Generation**

Generate comprehensive JSON Schema documentation for methods and types, enabling seamless integration with OpenAPI, code generation tools, and AI systems.

```csharp
var methodSchema = reflector.GetArgumentsSchema(methodInfo);
var typeSchema = reflector.GetSchema<MyClass>();
```

### 🔌 **Extensible Converter System**

Register custom converters using a flexible chain-of-responsibility pattern. Easily extend serialization behavior for any .NET type with specialized logic.

```csharp
reflector.Convertors.Add(new MyCustomConverter<SpecialType>());
```

### 📈 **Comprehensive Type Introspection**

Analyze and discover serializable fields, properties, and type metadata with advanced filtering and analysis capabilities.

```csharp
var fields = reflector.GetSerializableFields(typeof(MyClass));
var properties = reflector.GetSerializableProperties(typeof(MyClass));
```

### 🛡️ **Robust Error Handling & Logging**

Integrated Microsoft.Extensions.Logging support with hierarchical error reporting, detailed diagnostics, and comprehensive validation.

### 🤖 **AI & Integration Ready**

Optimized for AI scenarios with JSON Schema support, dynamic method binding, and cross-language serialization compatibility.

## Architecture Overview

ReflectorNet employs a sophisticated **Chain of Responsibility** pattern with multiple specialized converters:

- **PrimitiveReflectionConvertor**: Handles built-in .NET types (int, string, DateTime, etc.)
- **GenericReflectionConvertor**: Manages custom classes and structs
- **ArrayReflectionConvertor**: Specialized handling for arrays and collections
- **Custom Converters**: Extensible system for specialized type handling

## Advanced Use Cases

### 🔥 Dynamic Scripting & Automation

```csharp
// AI can discover and invoke methods dynamically
var result = reflector.MethodCall(new MethodRef
{
    TypeName = "Calculator",
    MethodName = "Add"
}, inputParameters: parameters);
```

### 📚 API Documentation Generation

```csharp
// Generate comprehensive API documentation
var schema = reflector.GetArgumentsSchema(methodInfo);
// Use schema for OpenAPI/Swagger generation
```

### 🧪 Advanced Testing Frameworks

```csharp
// Dynamic test case generation and execution
var testMethods = reflector.FindMethod(new MethodRef
{
    MethodName = "Test",
    TypeName = "TestClass"
}, methodNameMatchLevel: 2);
```

### 🔄 Configuration & State Management

```csharp
// Serialize complex application state
var appState = reflector.Serialize(applicationState);
// Later restore with exact type preservation
var restored = reflector.Deserialize(appState);
```

## Getting Started

### Installation

```bash
dotnet add package com.IvanMurzak.ReflectorNet
```

### Basic Usage

```csharp
using com.IvanMurzak.ReflectorNet;

var reflector = new Reflector();

// Serialize any object
var serialized = reflector.Serialize(myObject);

// Deserialize with type safety
var restored = reflector.Deserialize<MyClass>(serialized);

// Discover methods dynamically
var methods = reflector.FindMethod(new MethodRef
{
    TypeName = "MyClass",
    MethodName = "MyMethod"
});

// Generate JSON Schema
var schema = reflector.GetSchema<MyClass>();
```

## Core API Reference

### Primary Methods

#### Serialization & Deserialization

- **`Serialize(...)`** - Convert objects to type-preserving serialized representations with support for complex nested structures
- **`Deserialize<T>(...)`** - Reconstruct strongly-typed objects from serialized data with intelligent type resolution
- **`Deserialize(...)`** - Deserialize to specific types with flexible fallback type support

#### Object Management

- **`CreateInstance<T>()`** - Intelligent instance creation with automatic constructor resolution and dependency handling
- **`CreateInstance(Type)`** - Create instances of specific types with support for complex constructors
- **`TryPopulate(...)`** - Advanced in-place object population from serialized data with comprehensive validation and error handling
  - Supports selective member population with BindingFlags control
  - Hierarchical population with depth tracking and detailed error reporting
  - Type-safe validation and compatibility checking
  - Non-destructive updates that preserve existing object structure
  - Batch population capabilities with consolidated error logging
- **`GetDefaultValue<T>()`** - Retrieve intelligent default values for types with custom converter logic
- **`GetDefaultValue(Type)`** - Get default values for specific types with nullable type unwrapping

#### Method Discovery & Invocation

- **`FindMethod(...)`** - Discover methods with fuzzy matching algorithms and configurable similarity scoring (0-6 levels)
- **`MethodCall(...)`** - Dynamically invoke discovered methods with parameter binding and execution control

#### Type Introspection

- **`GetSerializableFields(...)`** - Analyze type structure and discover serializable fields using converter chain
- **`GetSerializableProperties(...)`** - Discover serializable properties with BindingFlags control and logging support

#### JSON Schema Generation

- **`GetSchema<T>()`** - Generate comprehensive JSON Schema for generic types with reference optimization
- **`GetSchema(Type)`** - Create JSON Schema for specific types with primitive handling and documentation extraction
- **`GetArgumentsSchema(...)`** - Generate method parameter schemas for dynamic invocation and API documentation

#### JSON Serialization

- **`JsonSerializer`** - Access to ReflectorNet-optimized JSON serializer with custom converters
- **`JsonSerializerOptions`** - Access to configured JSON serialization options
- **`JsonSchema`** - Access to JSON Schema generation utilities

### Advanced Configuration

#### Custom Converter Registration

```csharp
// Create a custom converter for specialized types
public class MyReflectionConvertor : GenericReflectionConvertor<MyClass>
{
    // Override serialization behavior
    protected override SerializedMember InternalSerialize(
        Reflector reflector, object? obj, Type type,
        string? name = null, bool recursive = true,
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
        int depth = 0, StringBuilder? stringBuilder = null, ILogger? logger = null)
    {
        // Custom serialization logic
        return base.InternalSerialize(reflector, obj, type, name, recursive, flags, depth, stringBuilder, logger);
    }

    // Override priority scoring for better type matching
    public override int SerializationPriority(Type type, ILogger? logger = null)
    {
        // Custom priority logic
        return base.SerializationPriority(type, logger);
    }
}

// Register the custom converter
reflector.Convertors.Add(new MyReflectionConvertor());
```

#### JSON Serialization Configuration

```csharp
// Access to JSON serializer for additional configuration
var jsonSerializer = reflector.JsonSerializer;
jsonSerializer.AddConverter(new MyCustomJsonConverter());

// Access to JSON schema generation
var jsonSchema = reflector.JsonSchema;
var schema = jsonSchema.GetSchema<MyClass>(reflector, justRef: false);
```

#### Converter System Architecture

The ReflectorNet converter system uses a sophisticated priority-based selection:

- **Priority Scoring**: Each converter returns a score (0-10000+) indicating compatibility
- **Automatic Selection**: Highest priority converter is automatically selected
- **Type Inheritance**: Considers inheritance distance for optimal converter matching
- **Extensibility**: Easy to add custom converters for specialized type handling

## Integration Examples

### Unity Game Engine

This project powers [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) for AI-driven Unity development.

### Web APIs

```csharp
// Generate OpenAPI documentation
var methodSchemas = GetAllMethods()
    .Select(m => reflector.GetArgumentsSchema(m))
    .ToList();
```

### AI & Machine Learning

```csharp
// AI can discover and invoke methods based on natural language
var candidates = reflector.FindMethod(new MethodRef
{
    MethodName = aiGeneratedMethodName,
    TypeName = aiGeneratedTypeName
}, methodNameMatchLevel: 3);
```

## Performance & Optimization

- **Lazy Loading**: Types and methods are discovered on-demand
- **Caching**: Reflection metadata is cached for optimal performance
- **Memory Efficient**: Minimal memory footprint with intelligent object pooling
- **Parallel Safe**: Thread-safe operations for concurrent environments

## Contributing

We welcome contributions! Here's how to get involved:

1. **Fork** the repository on [GitHub](https://github.com/IvanMurzak/ReflectorNet)
2. **Create** a feature branch: `git checkout -b feature/amazing-feature`
3. **Implement** your changes with comprehensive tests
4. **Document** your changes and update relevant documentation
5. **Test** thoroughly across supported .NET versions
6. **Submit** a Pull Request with detailed description

### Development Guidelines

- Follow existing code style and patterns
- Add comprehensive unit tests for new features
- Update documentation for API changes
- Ensure compatibility across all supported .NET versions
- Include performance benchmarks for significant changes

### Bug Reports & Feature Requests

Please use GitHub Issues with detailed descriptions, reproduction steps, and environment information.

## License & Support

ReflectorNet is released under the Apache-2.0 License.

For questions, support, or discussions:

- GitHub Issues for bug reports and feature requests
- GitHub Discussions for general questions and community support
- Check the `docs/` folder for detailed documentation and examples

---

Built with ❤️ for the .NET and AI communities

## Attribution & Citation

If you use ReflectorNet, please attribute the project:

"ReflectorNet (c) 2024-2025 Ivan Murzak, licensed under Apache-2.0"

Formal citation (see `CITATION.cff`):

Murzak, I. (2025). ReflectorNet: Advanced .NET Reflection Toolkit for AI. Version 0.2.1. <https://github.com/IvanMurzak/ReflectorNet>

Third-party notices (if any) are listed in `NOTICE`.
