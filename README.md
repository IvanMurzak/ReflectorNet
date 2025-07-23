
# ReflectorNet

[![nuget](https://img.shields.io/nuget/v/com.IvanMurzak.ReflectorNet)](https://www.nuget.org/packages/com.IvanMurzak.ReflectorNet/) ![License](https://img.shields.io/github/license/IvanMurzak/ReflectorNet) [![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

[![.NET CI](https://github.com/IvanMurzak/ReflectorNet/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/IvanMurzak/ReflectorNet/actions/workflows/dotnet.yml)

ReflectorNet is a powerful .NET library designed to provide advanced reflection-based serialization, deserialization, and dynamic method invocation capabilities. It enables developers to inspect, manipulate, and interact with .NET objects, types, and methods at runtime, making it ideal for building tools, frameworks, and applications that require deep introspection or dynamic behavior.

## Main Features

- **Advanced Reflection-Based Serialization & Deserialization**
  Seamlessly convert .NET objects—including complex types, collections, and custom structures—to and from a type-preserving, flexible serialized format (`SerializedMember`). Supports custom converters for specialized scenarios.

- **Dynamic Method Discovery & Invocation**
  Locate and invoke methods at runtime using powerful filters (namespace, type, method name, parameters). Supports both static and instance methods, including asynchronous (`Task`) methods, enabling dynamic scripting and automation.

- **Automatic JSON Schema Generation**
  Generate JSON Schema representations for method parameters and return types. This enables integration with code generation, documentation, validation tools, and OpenAPI workflows.

- **Extensible Converter & Populator Registry**
  Register custom converters and populators using a flexible, chain-of-responsibility pattern. Easily extend serialization and deserialization logic for any .NET type.

- **Comprehensive Type Introspection**
  Analyze and discover serializable fields, properties, and type metadata. Perform advanced type matching, filtering, and metadata extraction for deep introspection.

- **Robust Error Handling & Logging**
  Integrated error reporting and support for `Microsoft.Extensions.Logging` provide detailed traceability, diagnostics, and debugging.

- **Integration-Ready**
  Designed for seamless integration with scripting engines, test frameworks, code analyzers, and documentation generators.

## Project Goals

- **Maximum Flexibility:** Support a broad spectrum of .NET types and use cases, from simple primitives to deeply nested object graphs and custom types.
- **Easy Extensibility:** Allow users to plug in custom converters, populators, and serialization strategies with minimal effort.
- **Type Safety:** Ensure type information is preserved throughout all serialization, deserialization, and dynamic invocation processes.
- **Dynamic Automation:** Empower dynamic code execution, scripting, and automation by exposing runtime method invocation and object manipulation.
- **Seamless Integration:** Provide schema and metadata generation for use in code generation, documentation, validation, and interoperability workflows.

## Example Use Cases

- Building scripting engines or automation tools that need to invoke .NET methods dynamically.
- Creating serialization frameworks that require deep type introspection and custom logic.
- Generating OpenAPI/JSON Schema documentation for .NET APIs.
- Developing test frameworks or code analyzers that operate on runtime metadata.

## Getting Started

1. Add the ReflectorNet NuGet package to your project.
2. Use the `Reflector` class to serialize, deserialize, or invoke methods dynamically.
3. Register custom converters as needed for your types.

See the `docs/` folder and code comments for more details and advanced usage examples.
This project is used in [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP).

## Usage API

- `Reflector.Serialize(...)`
- `Reflector.Deserialize(...)`
- `Reflector.GetSerializableFields(...)`
- `Reflector.GetSerializableProperties(...)`
- `Reflector.Populate(...)`
- `Reflector.PopulateAsProperty(...)`

### Override ReflectionConvertor for a custom type

You may need to override convertor in the same way as JsonConvertor works if you have a custom type that should be handled differently.

Here is custom class sample.

```csharp
public class MyClass
{
    public int health = 100;
}
```

Create custom convertor

```csharp
public class MyReflectionConvertor : GenericReflectionConvertor<MyClass>
{

}
```

Register the convertor

```csharp
Reflector.Registry.Add(new MyReflectionConvertor());
```

## Contribution

Contributions are welcome! If you would like to help improve ReflectorNet, please follow these steps:

1. **Fork the repository** on [GitHub](https://github.com/IvanMurzak/ReflectorNet).
2. **Create a new branch** for your feature or bugfix:
   `git checkout -b my-feature`
3. **Make your changes** and add tests if applicable.
4. **Commit your changes** with a clear and descriptive message.
5. **Push your branch** to your forked repository.
6. **Open a Pull Request** to the `main` branch of the original repository. Please describe your changes and reference any related issues.

Before submitting, ensure your code follows the project's style and passes all tests. For major changes, please open an issue first to discuss your proposal.

Thank you for contributing to ReflectorNet!
