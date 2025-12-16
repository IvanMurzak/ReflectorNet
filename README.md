# ReflectorNet

[![NuGet](https://img.shields.io/nuget/v/com.IvanMurzak.ReflectorNet?label=NuGet&labelColor=333A41)](https://www.nuget.org/packages/com.IvanMurzak.ReflectorNet/)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue?logoColor=white&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet)
[![netstandard2.1](https://img.shields.io/badge/.NET-netstandard2.1-blue?logoColor=white&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet)
[![Tests](https://github.com/IvanMurzak/ReflectorNet/actions/workflows/release.yml/badge.svg?branch=main)](https://github.com/IvanMurzak/ReflectorNet/actions/workflows/release.yml)

[![Stars](https://img.shields.io/github/stars/IvanMurzak/ReflectorNet 'Stars')](https://github.com/IvanMurzak/ReflectorNet/stargazers)
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/Cgs6nM8BPU)
[![License](https://img.shields.io/github/license/IvanMurzak/ReflectorNet?label=License&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

**ReflectorNet** is a sophisticated .NET reflection toolkit designed to bridge the gap between static .NET applications and dynamic, AI-driven environments. It provides robust serialization, intelligent method discovery, and dynamic invocation capabilities that allow AI agents to interact with .NET codebases safely and effectively.

## 🚀 Why ReflectorNet?

Traditional reflection is brittle and requires exact matches. ReflectorNet is built for flexibility:

*   **🤖 AI-Ready**: Designed for scenarios where inputs (from LLMs) might be partial or fuzzy.
*   **🔍 Fuzzy Matching**: Discover methods and types even with incomplete names or parameters (configurable match levels 0-6).
*   **📦 Type-Safe Serialization**: Preserves full type information, supporting complex nested objects, collections, and custom types.
*   **🔄 In-Place Population**: Update existing object instances from serialized data without breaking references.
*   **📄 JSON Schema Generation**: Automatically generate schemas for your types and methods to feed into LLM context windows.

## 📦 Installation

```bash
dotnet add package com.IvanMurzak.ReflectorNet
```

## ⚡ Quick Start

### 1. Setup

The `Reflector` class is your main entry point.

```csharp
using com.IvanMurzak.ReflectorNet;

var reflector = new Reflector();
```

### 2. Serialization

Convert any .NET object into a `SerializedMember` intermediate representation. This preserves type metadata that standard JSON serializers might lose.

```csharp
var myObject = new MyComplexClass { Id = 1, Name = "Test" };

// Serialize to intermediate representation
SerializedMember serialized = reflector.Serialize(myObject);

// Convert to JSON string if needed
string json = reflector.JsonSerializer.Serialize(serialized);
```

### 3. Deserialization

Reconstruct objects with full type fidelity.

```csharp
// Restore to a specific type
MyComplexClass restored = reflector.Deserialize<MyComplexClass>(serialized);

// Or let Reflector resolve the type automatically
object restoredObj = reflector.Deserialize(serialized);
```

### 4. In-Place Population

Update an existing object instance with new data. This is crucial for maintaining object identity in stateful applications (like Unity games or long-running services).

```csharp
var existingInstance = new MyComplexClass();

// Populate 'existingInstance' with data from 'serialized'
// Returns true if successful
bool success = reflector.TryPopulate(ref existingInstance, serialized);
```

### 5. Dynamic Method Invocation

Allow AI to find and call methods without knowing the exact signature.

```csharp
using com.IvanMurzak.ReflectorNet.Model;

// 1. Define what we are looking for (can be partial)
var methodRef = new MethodRef
{
    TypeName = "Calculator",
    MethodName = "Add", // Could be "AddValues" or "CalculateAdd" depending on match level
    InputParameters = new List<MethodRef.Parameter>
    {
        new MethodRef.Parameter { Name = "a", Value = "10" },
        new MethodRef.Parameter { Name = "b", Value = "20" }
    }
};

// 2. Call the method
// Note: We pass 'reflector' as the first argument to handle internal deserialization context
string result = reflector.MethodCall(
    reflector,
    methodRef,
    methodNameMatchLevel: 3 // Allow fuzzy matching
);

Console.WriteLine(result); // Output: [Success] 30
```

## 🏗️ Architecture

ReflectorNet is built on a **Chain of Responsibility** pattern to handle the complexity of .NET types.

### Core Components

*   **`Reflector`**: The orchestrator. It manages the registry of converters and exposes the high-level API.
*   **`Registry`**: Holds a prioritized list of `IReflectionConverter`s. When you serialize or deserialize, the registry finds the best converter for the specific type.
*   **`SerializedMember`**: The universal data model. It represents any .NET object (primitive, class, array) in a serializable format that holds both value and type metadata.

### Built-in Converters

ReflectorNet comes with a set of standard converters:
1.  **`PrimitiveReflectionConverter`**: Handles `int`, `string`, `bool`, `DateTime`, etc.
2.  **`ArrayReflectionConverter`**: Handles arrays (`T[]`) and generic lists (`List<T>`).
3.  **`GenericReflectionConverter<T>`**: The fallback for custom classes and structs.
4.  **`TypeReflectionConverter`** & **`AssemblyReflectionConverter`**: Specialized handling for `System.Type` and `System.Reflection.Assembly`.

### Extensibility

You can create custom converters for your own types by implementing `IReflectionConverter` or inheriting from `GenericReflectionConverter<T>` and registering them:

```csharp
reflector.Converters.Add(new MyCustomConverter());
```

## 🛠️ Advanced Features

### JSON Schema Generation

Generate schemas to describe your C# types to an LLM.

```csharp
// Get schema for a type
var typeSchema = reflector.GetSchema<MyClass>();

// Get schema for method arguments (great for function calling)
var methodSchema = reflector.GetArgumentsSchema(myMethodInfo);
```
### 🧩 The Converter System (Custom Serialization)

ReflectorNet's power lies in its extensible **Converter System**. If you have "exotic" data models (e.g., third-party types you can't modify, complex graphs, or types needing special handling like `System.Type`), you can write a custom converter.

#### How it Works

1.  **Interface**: All converters implement `IReflectionConverter`.
2.  **Base Class**: Most custom converters should inherit from `BaseReflectionConverter<T>` or `GenericReflectionConverter<T>`.
3.  **Priority**: ReflectorNet asks every registered converter: *"Can you handle this type, and how well?"* (via `SerializationPriority`). The one with the highest score wins.
    *   Exact match: Highest priority.
    *   Inheritance match: Lower priority (based on distance).
    *   No match: Zero.

#### Creating a Custom Converter

Here is an example of a converter for a hypothetical `ThirdPartyWidget` that should be serialized as a simple string instead of a complex object.

<details>
<summary>Click to see the code example</summary>

```csharp
using com.IvanMurzak.ReflectorNet.Converter;
using com.IvanMurzak.ReflectorNet.Model;

// 1. Inherit from GenericReflectionConverter<T> for the target type
public class WidgetConverter : GenericReflectionConverter<ThirdPartyWidget>
{
    // 2. Override SerializationPriority if you need special matching logic
    // (The default implementation already handles inheritance distance perfectly)

    // 3. Override InternalSerialize to customize output
    protected override SerializedMember InternalSerialize(
        Reflector reflector,
        object? obj,
        Type type,
        string? name = null,
        bool recursive = true,
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
        int depth = 0,
        StringBuilder? stringBuilder = null,
        ILogger? logger = null,
        SerializationContext? context = null)
    {
        if (obj is ThirdPartyWidget widget)
        {
            // Serialize as a simple string value instead of an object with fields
            return SerializedMember.FromValue(
                reflector,
                type,
                value: $"Widget:{widget.Id}",
                name: name
            );
        }

        return base.InternalSerialize(reflector, obj, type, name, recursive, flags, depth, stringBuilder, logger, context);
    }

    // 4. Override CreateInstance if the type has no parameterless constructor
    public override object? CreateInstance(Reflector reflector, Type type)
    {
        return new ThirdPartyWidget("default-id");
    }
}

// 5. Register it
reflector.Converters.Add(new WidgetConverter());
```

</details>

### 📜 Custom JSON Schema Generation

While `ReflectionConverter` handles runtime object manipulation, you might also want to control how your types are described in the generated JSON Schema (used by LLMs to understand your data structure).

ReflectorNet allows you to customize this by implementing the `IJsonSchemaConverter` interface. This is often done by inheriting from `JsonSchemaConverter<T>`, which combines standard JSON serialization with schema generation.

#### How it Works

1.  **Interface**: Implement `IJsonSchemaConverter`.
2.  **Registration**: Add the converter to `reflector.JsonSerializer`.
3.  **Generation**: When `reflector.GetSchema()` is called, it checks if a registered converter exists for a type. If that converter implements `IJsonSchemaConverter`, it delegates schema creation to it.

#### Example: Custom Schema for a Widget

Suppose you have a `ThirdPartyWidget` that serializes to a string (e.g., "Widget:123"), and you want the LLM to know this format.

<details>
<summary>Click to see the code example</summary>

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Converter.Json;
using com.IvanMurzak.ReflectorNet.Utils;

// 1. Inherit from JsonSchemaConverter<T>
public class WidgetSchemaConverter : JsonSchemaConverter<ThirdPartyWidget>
{
    // 2. Define the Schema Definition (what the type looks like)
    public override JsonNode GetSchema()
    {
        return new JsonObject
        {
            [JsonSchema.Type] = "string",
            [JsonSchema.Pattern] = "^Widget:\\d+$",
            [JsonSchema.Description] = "A widget identifier in the format 'Widget:{id}'"
        };
    }

    // 3. Define the Schema Reference (how other types refer to it)
    public override JsonNode GetSchemaRef()
    {
        // Standard way to refer to the definition
        return new JsonObject
        {
            [JsonSchema.Ref] = JsonSchema.RefValue + TypeUtils.GetSchemaTypeId<ThirdPartyWidget>()
        };
    }

    // 4. Implement standard System.Text.Json logic (optional if only used for schema, but recommended)
    public override void Write(Utf8JsonWriter writer, ThirdPartyWidget value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"Widget:{value.Id}");
    }

    public override ThirdPartyWidget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        // Parse "Widget:123" back to object...
        return new ThirdPartyWidget(str.Split(':')[1]);
    }
}

// 5. Register it with the JSON Serializer
reflector.JsonSerializer.AddConverter(new WidgetSchemaConverter());
```

</details>

> **Note:** You can use `ReflectionConverter` (for runtime logic) and `JsonSchemaConverter` (for schema/transport) together for the same type if needed.

### Fuzzy Matching Levels

When searching for methods, you can tune the strictness:
*   **6**: Exact match
*   **5**: Case-insensitive match
*   **4**: Starts with (Case-sensitive)
*   **3**: Starts with (Case-insensitive)
*   **2**: Contains (Case-sensitive)
*   **1**: Contains (Case-insensitive)

## 🤝 Contributing

Contributions are welcome! Please submit Pull Requests to the `main` branch.

1.  Fork the repository.
2.  Create a feature branch.
3.  Commit your changes.
4.  Push to the branch.
5.  Open a Pull Request.

## 📄 License

This project is licensed under the Apache-2.0 License. Copyright - Ivan Murzak.
