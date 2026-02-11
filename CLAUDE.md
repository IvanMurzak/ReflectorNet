# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

```bash
# Build the solution
dotnet build --configuration Release

# Run all tests (multi-TFM: net8.0 and net9.0)
dotnet test --configuration Release --verbosity normal

# Run a single test by fully qualified name
dotnet test --configuration Release --filter "FullyQualifiedName~com.IvanMurzak.ReflectorNet.Tests.ClassName.MethodName"

# Run tests matching a pattern
dotnet test --configuration Release --filter "FullyQualifiedName~SchemaTests"

# Pack for NuGet
dotnet pack ReflectorNet/ReflectorNet.csproj --configuration Release --output ./packages
```

## Project Layout

- **ReflectorNet/** - Main library (NuGet: `com.IvanMurzak.ReflectorNet`)
- **ReflectorNet.Tests/** - xUnit test suite
- **ReflectorNet.Tests.OuterAssembly/** - Separate assembly used by tests for cross-assembly reflection scenarios
- **ConsoleApp/** - Console app for manual testing

## Target Frameworks & Language

- **Library**: `netstandard2.1`, `net8.0`, `net9.0` — LangVersion `10.0`
- **Tests**: `net8.0`, `net9.0` — LangVersion `11.0`
- Nullable enabled, ImplicitUsings disabled throughout
- Root namespace: `com.IvanMurzak.ReflectorNet`

## Architecture

ReflectorNet is a reflection toolkit for AI-driven .NET scenarios — fuzzy method discovery, type-preserving serialization, and dynamic invocation.

### Reflector (partial class, split across 10 files)

`Reflector` is the main entry point in `src/Reflector/`. Each concern is a separate partial file:

| File | Responsibility |
|------|---------------|
| `Reflector.cs` | Constructor, reference resolution |
| `Reflector.Serialize.cs` | Object → `SerializedMember` |
| `Reflector.Deserialize.cs` | `SerializedMember` → object |
| `Reflector.Populate.cs` | In-place object updates |
| `Reflector.FindMethod.cs` | Fuzzy method discovery (match levels 1-6) |
| `Reflector.CallMethod.cs` | Dynamic method invocation |
| `Reflector.Json.cs` | JSON serialization integration |
| `Reflector.Registry.cs` | Converter registry (nested `Registry` class) |
| `Reflector.DefaultValue.cs` | Default value resolution |
| `Reflector.Equals.cs` | Deep equality comparison |
| `Reflector.Error.cs` | Hierarchical error formatting |

### Converter System (Chain of Responsibility)

Located in `src/Converter/`. The `Registry` selects the best converter by querying each for `SerializationPriority(Type)` — highest score wins.

**Reflection converters** (`src/Converter/Reflection/`):
- `BaseReflectionConverter<T>` — abstract base (partial: `.Serialize`, `.Deserialize`, `.Populate`, `.DefaultValue`)
- `PrimitiveReflectionConverter` — built-in types (int, string, DateTime, etc.)
- `GenericReflectionConverter<T>` — custom classes/structs (fallback)
- `ArrayReflectionConverter` — arrays and collections (partial: `.Deserialize`)
- `TypeReflectionConverter`, `AssemblyReflectionConverter` — System.Type, Assembly
- `LazyGenericReflectionConverter` — runtime type resolution for optional dependencies
- `IgnoreFieldsAndPropertiesReflectionConverter` — selective member exclusion

**JSON converters** (`src/Converter/Json/`): System.Text.Json converters for many .NET types. `IJsonSchemaConverter` interface enables custom JSON Schema generation.

### Key Models (`src/Model/`)

- `SerializedMember` — type-preserving intermediate representation (partial: `.Static`)
- `MethodRef` — method reference for fuzzy discovery
- `MethodData` — method metadata and parameters
- `SerializationContext` / `DeserializationContext` — state during (de)serialization

### Thread Safety

Registry uses `ConcurrentBag`/`ConcurrentDictionary` with a cache-replacement invalidation pattern (replace entire dictionary reference rather than clearing) for thread-safe converter and blacklist cache management.

## CI/CD

- **PR workflow** (`pull_request.yml`): build + test on ubuntu with .NET 8.0 + 9.0, publishes trx test results
- **Release workflow** (`release.yml`): triggered on push to main — reads version from `ReflectorNet.csproj` `<Version>`, creates GitHub release tag, publishes NuGet package. Version bump = new release.

## Versioning

Version is defined in `ReflectorNet/ReflectorNet.csproj` `<Version>` element. Bumping it and merging to main triggers a NuGet publish.
