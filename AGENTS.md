# AGENTS.md

ReflectorNet is a .NET reflection toolkit for AI-driven scenarios: fuzzy method discovery,
type-preserving serialization, and dynamic invocation. It ships as the NuGet package
`com.IvanMurzak.ReflectorNet` and is the most foundational library in the workspace dependency chain.

## Build / test

```bash
dotnet build --configuration Release
dotnet test --configuration Release --verbosity normal   # multi-TFM: net8.0 + net9.0
dotnet test --configuration Release --filter "FullyQualifiedName~com.IvanMurzak.ReflectorNet.Tests.ClassName.MethodName"
dotnet pack ReflectorNet/ReflectorNet.csproj --configuration Release --output ./packages
```

## Layout

- `ReflectorNet/` — the library (the only packed project). New source goes under `src/<area>/`
  (`Reflector/` partial class, `Converter/`, `Model/`, `Extension/`, `Utils/`, `Exception/`).
- `ReflectorNet.Tests/` — xUnit tests under `src/`, grouped by feature; tests inherit `BaseTest`.
- `ReflectorNet.Tests.OuterAssembly/` — separate assembly for cross-assembly reflection tests.
- `ConsoleApp/` — console app for manual testing.
- `commands/` — maintainer PowerShell scripts (version bump, release notes).
- `docs/claude/` — agent reference docs (below); `docs/plan/` — design notes.
- `.github/workflows/` — PR build/test (`pull_request.yml`) and the dispatch-only release.

## Find detail in

- `docs/claude/architecture.md` — Reflector partial-class structure, converter chain, target frameworks
- `docs/claude/models.md` — `SerializedMember`, `MethodRef`, context objects
- `docs/claude/threading.md` — Registry thread safety pattern
- `docs/claude/release.md` — CI/CD workflows and versioning
- `.github/copilot-instructions.md` — code style and test naming conventions
