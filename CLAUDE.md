# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build & Test
```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release --verbosity normal

# Build and pack for NuGet
dotnet pack ReflectorNet/ReflectorNet.csproj --configuration Release --output ./packages
```

### Project Structure
- **ReflectorNet/**: Main library project - advanced .NET reflection toolkit
- **ReflectorNet.Tests/**: Primary test suite using xUnit
- **ReflectorNet.Tests.OuterAssembly/**: Test assembly for cross-assembly reflection scenarios
- **ConsoleApp/**: Console application for testing and examples

## Architecture Overview

ReflectorNet implements a sophisticated **Chain of Responsibility** pattern for object serialization/deserialization:

### Core Components

#### Reflector Class (`src/Reflector/Reflector.cs`)
- Main entry point providing serialization, deserialization, and object population capabilities
- Partial class split across multiple files for different concerns
- Uses converter registry system for extensible type handling

#### Converter System (`src/Converter/`)
- **IReflectionConverter**: Interface defining converter contract
- **Chain of Responsibility**: Multiple specialized converters handle different types:
  - `PrimitiveReflectionConverter`: Built-in .NET types (int, string, DateTime, etc.)
  - `GenericReflectionConverter`: Custom classes and structs
  - `ArrayReflectionConverter`: Arrays and collections
  - `BaseReflectionConverter`: Abstract base with common functionality

#### Key Models (`src/Model/`)
- **SerializedMember**: Type-preserving serialized object representation
- **MethodRef**: Method reference for dynamic discovery and invocation
- **MethodData**: Method metadata and parameter information

#### Utilities (`src/Utils/`)
- **JsonSchema**: JSON Schema generation for types and methods
- **JsonSerializer**: ReflectorNet-optimized JSON serialization
- **TypeUtils**: Type resolution and naming utilities
- **StringUtils**: String manipulation and formatting

### Target Frameworks
- **.NET Standard 2.0** & **2.1**: Broad compatibility
- **.NET 9.0**: Latest framework features
- **LangVersion 11.0**: Modern C# language features
- **Nullable enabled**: Strict null reference types

## Key Features Implementation

### Method Discovery & Invocation
- Fuzzy matching algorithms with configurable similarity levels (0-6)
- Dynamic method binding with parameter type resolution
- Support for partial method names and namespace matching
- Cross-assembly method discovery capabilities

### Object Population System
- In-place object updates without replacement
- Selective member population with BindingFlags control
- Hierarchical population with depth tracking
- Comprehensive error reporting with detailed logging

### JSON Schema Generation
- OpenAPI-compatible schema generation
- Method parameter schemas for dynamic invocation
- Type introspection with reference optimization
- Support for complex nested types and collections

## Development Patterns

### Converter Priority System
Each converter implements `SerializationPriority(Type type)` returning a score (0-10000+):
- Higher scores indicate better type compatibility
- Automatic selection of optimal converter for each type
- Considers inheritance distance for matching

### Error Handling
- Hierarchical error reporting with depth-based indentation
- StringBuilder-based error accumulation
- Microsoft.Extensions.Logging integration throughout
- Comprehensive validation at each operation level

### Extensibility
- Custom converter registration via `Reflector.Converters.Add()`
- Override priority scoring for specialized type handling
- Pluggable serialization behavior for any .NET type

## Testing Strategy
- **xUnit** testing framework
- Cross-assembly testing with separate test assembly
- Comprehensive coverage of serialization scenarios
- Performance and compatibility testing across target frameworks

## Package Information
- **NuGet Package**: `com.IvanMurzak.ReflectorNet`
- **Current Version**: 1.0.5
- **Dependencies**: Microsoft.Extensions.Logging 9.0.7, System.Text.Json 9.0.7
- **Runtime Identifiers**: Supports Windows, Linux, and macOS (x64, x86, ARM64)