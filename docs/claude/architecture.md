# Architecture

ReflectorNet is a reflection toolkit for AI-driven .NET scenarios — fuzzy method discovery, type-preserving serialization, and dynamic invocation.

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

## Reflector (partial class, split across 10 files)

`Reflector` is the main entry point in `src/Reflector/`. Each concern is a separate partial file:

| File | Responsibility |
|------|---------------|
| `Reflector.cs` | Constructor, reference resolution |
| `Reflector.Serialize.cs` | Object → `SerializedMember` |
| `Reflector.Deserialize.cs` | `SerializedMember` → object |
| `Reflector.Modify.cs` | In-place object updates |
| `Reflector.FindMethod.cs` | Fuzzy method discovery (match levels 1-6) |
| `Reflector.CallMethod.cs` | Dynamic method invocation |
| `Reflector.Json.cs` | JSON serialization integration |
| `Reflector.Registry.cs` | Converter registry (nested `Registry` class) |
| `Reflector.DefaultValue.cs` | Default value resolution |
| `Reflector.Equals.cs` | Deep equality comparison |
| `Reflector.Error.cs` | Hierarchical error formatting |

## Converter System (Chain of Responsibility)

Located in `src/Converter/`. The `Registry` selects the best converter by querying each for `SerializationPriority(Type)` — highest score wins.

**Reflection converters** (`src/Converter/Reflection/`):
- `BaseReflectionConverter<T>` — abstract base (partial: `.Serialize`, `.Deserialize`, `.Modify`, `.DefaultValue`)
- `PrimitiveReflectionConverter` — built-in types (int, string, DateTime, etc.)
- `GenericReflectionConverter<T>` — custom classes/structs (fallback)
- `ArrayReflectionConverter` — arrays and collections (partial: `.Deserialize`)
- `TypeReflectionConverter`, `AssemblyReflectionConverter` — System.Type, Assembly
- `LazyGenericReflectionConverter` — runtime type resolution for optional dependencies
- `IgnoreFieldsAndPropertiesReflectionConverter` — selective member exclusion

**JSON converters** (`src/Converter/Json/`): System.Text.Json converters for many .NET types. `IJsonSchemaConverter` interface enables custom JSON Schema generation.

### Deserialization outcome (tri-state) — read before touching a converter's error handling

A converter answers a `value` payload with one of three outcomes (`DeserializationOutcome`), never a bare boolean:

| outcome | meaning | severity |
|---|---|---|
| `Handled` | value produced | — |
| `NotApplicable` | not this converter's shape; another may handle it. **Not an error** | `Trace` |
| `Failed` | IS this converter's shape but invalid | `Error` |

`SerializedMemberShape.Classify` decides which, from the payload's property names alone: an unknown key and no **structural** `SerializedMember` key (e.g. a consumer object reference `{"instanceID":"…"}`) → `NotApplicable`; a **structural** key **plus** an unknown one → `Failed`; `{}` or all-recognised keys → a valid `SerializedMember`.

⚠ The discriminator is a **structural** key — `value`, `fields`, `props` (`SerializedMemberShape.StructuralKeys`) — not any recognised key. `name` and `typeName` (`DescriptiveKeys`) are ordinary English words that foreign payloads carry too: Unity-MCP's `GameObjectRefConverter` writes `name` and its `ComponentRefConverter` writes `typeName`, so `{"instanceID":"7","typeName":"UnityEngine.Rigidbody"}` is a legitimate reference. Counting a descriptive key as evidence of intent (5.3.3) classified such references as `Failed` and threw `Unexpected property name: 'instanceID'` before the consumer's converter could resolve them. A genuinely mistyped `SerializedMember` carrying no structural key is not swallowed by the narrower rule — it still fails, at the chain end, with the same unknown-key diagnostic.

**Chain semantics — loudness belongs at the END.** First `Handled` wins; first `Failed` aborts and propagates; and only when *every* link declined does the chain end raise one explicit `DeserializationException`. The chain ends are `BaseReflectionConverter.Deserialize` and `TryDeserializeValueReporting` (which backs `SetField`/`SetProperty`).

⚠ **Never log `Error` for a non-error condition.** Two regressions have come out of collapsing these states into one signal: reporting both as success handed back a fabricated `default(T)` indistinguishable from a real value (`12f99093`), and then reporting both as failure severed every consumer converter that calls `base.TryDeserializeValueInternal(...)` and resolves a foreign shape itself — plus it logged `Error` for an ordinary condition, which reddened 11 downstream tests. `TryDeserializeValueInternal` and the 8-arg `TryDeserializeValue` are that consumer seam: their signatures **and** their decline behaviour (return `true`, leave `result` null) are load-bearing. Regression coverage: `ReflectorNet.Tests/src/ReflectorTests/ConverterFallThroughTests.cs`.

The **8-arg `TryDeserializeValue` is the only overridable seam**; the `out DeserializationOutcome` overload is a non-virtual wrapper that delegates to it. Keep it that way — if the wrapper carried the implementation, an external subclass overriding the virtual would still compile, still say `override`, and never be called.

**Known gap (pre-existing, not closed by the tri-state work):** `ArrayReflectionConverter` overrides `Deserialize` wholesale, so arrays and collections never reach the chain-end check. A non-array `value` payload still returns `null` with only a `Warning`, and its `TryDeserializeValueInternal` can answer a hard failure with `GetDefaultValue(type)` / an empty `CreateInstance(type)`. That is the same silent-substitution shape `12f99093` fixed elsewhere; `outcome` is the mechanism to close it when someone takes it on.

### Member application (two-phase)

`BaseReflectionConverter.Deserialize` RESOLVEs every `fields`/`props` member before writing any of them, so a payload that fails mid-way never leaves the target half-written. Three terminal states, recorded per member in `MemberApplicationReport` (a `Logs` subclass — pass it as the existing `logs` argument to opt in): `Applied`, `Rejected` (resolve failed → nothing written), `PartiallyApplied` (a setter threw during apply → the report names what landed). There is deliberately **no rollback**: consumer setters have side effects, so writing an old value back is a new mutation, not an undo.
