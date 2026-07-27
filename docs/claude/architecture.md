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

### Collections — same contract, second chain end

`ArrayReflectionConverter` overrides `Deserialize` wholesale, so a collection never passes through `BaseReflectionConverter.Deserialize`. It therefore carries **its own chain end**, obeying the same rules:

- The per-link work lives in `TryDeserializeCollectionValue` (`protected virtual`), which is QUIET: a `value` payload that is a JSON object carrying no **structural** `SerializedMember` key is `NotApplicable` at `Trace` — the collection chain end goes through the same `SerializedMemberShape.Classify(...).IsForeign`, so `name`/`typeName` do not make a foreign collection payload loud either. **Override that method to own a foreign payload shape on a collection type** — it is the collection equivalent of overriding `TryDeserializeValueInternal`.
- `Deserialize` is the only place allowed to be loud, and it raises one `DeserializationException` when nobody resolved the decline.
- A payload that is neither absent nor a JSON array nor a foreign shape (a string, a number, a malformed `SerializedMember`) is a hard failure and throws.
- `result` is never a fabricated value on a failure path — not `GetDefaultValue(type)`, not an empty `CreateInstance(type)`, not `null`.

**Element failures are all-or-nothing**, and that is a deliberate divergence from the member-set rule above. A member set settles for an honest `PartiallyApplied` because by the time a write fails it has already run side-effecting consumer setters that cannot be undone. A collection is BUILT and handed back — nothing reaches a live target until the caller assigns it — so atomicity is genuinely achievable, and returning a short or hole-punched collection that looks complete would be a fabrication rather than a compromise. A failing element records its index in the `MemberApplicationReport` and abandons the whole collection.

⚠ **An element resolving to `null` is NOT a failure** and must not be made one. A blacklisted element type resolves to `null` by design, and a consumer converter resolves a cleared or deleted object reference to `null` as its *correct* answer (`UnityGenericReflectionConverter` does exactly this). The collection cannot tell that apart from an unresolved reference, so it keeps the `null` and **discloses** it with one `Info` line naming how many elements resolved to null — never an `Error`. The single exception is a non-nullable value type, for which no converter can legitimately answer `null`; that is raised. Regression coverage: `ReflectorNet.Tests/src/ReflectorTests/CollectionDeserializationFailureTests.cs`.

Any exception escaping an element is re-raised as a `DeserializationException` (wrapping, e.g., a `JsonException` from a mistyped element). `Reflector.MethodCall`, `TryModify` and `TryDeserializeValueReporting` catch only that type, so anything else would escape the call instead of being reported.

### Member application (two-phase)

`BaseReflectionConverter.Deserialize` RESOLVEs every `fields`/`props` member before writing any of them, so a payload that fails mid-way never leaves the target half-written. Three terminal states, recorded per member in `MemberApplicationReport` (a `Logs` subclass — pass it as the existing `logs` argument to opt in): `Applied`, `Rejected` (resolve failed → nothing written), `PartiallyApplied` (a setter threw during apply → the report names what landed). There is deliberately **no rollback**: consumer setters have side effects, so writing an old value back is a new mutation, not an undo.
