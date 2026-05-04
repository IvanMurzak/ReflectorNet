# CLAUDE.md

## What this is

ReflectorNet is a .NET reflection toolkit for AI-driven scenarios: fuzzy method discovery, type-preserving serialization, and dynamic invocation. It ships as the NuGet package `com.IvanMurzak.ReflectorNet` and is the most foundational library in the workspace dependency chain.

## Build / run

```bash
# Build
dotnet build --configuration Release

# Test (multi-TFM: net8.0 and net9.0)
dotnet test --configuration Release --verbosity normal

# Single test by FQN
dotnet test --configuration Release --filter "FullyQualifiedName~com.IvanMurzak.ReflectorNet.Tests.ClassName.MethodName"

# Pack for NuGet
dotnet pack ReflectorNet/ReflectorNet.csproj --configuration Release --output ./packages
```

## Find detail in

- `docs/claude/architecture.md` — Reflector partial-class structure, converter chain, project layout, target frameworks
- `docs/claude/models.md` — `SerializedMember`, `MethodRef`, context objects
- `docs/claude/threading.md` — Registry thread safety pattern
- `docs/claude/release.md` — CI/CD workflows and versioning
