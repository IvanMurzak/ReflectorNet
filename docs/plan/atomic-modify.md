# Plan: Atomic Path-Based Modification API

## Context

ReflectorNet's `TryModify` requires supplying the full nested `SerializedMember` graph down to the target. For arrays there is an additional hard limitation: `TryModifyField` calls `TypeMemberUtils.GetField(arrayType, "[2]")` which always returns null. Targeting a single element in an array currently requires replacing the entire array.

**Goal**: add an ergonomic, truly atomic modification API that navigates directly to a specific field, array element, or dictionary entry and modifies only that — without constructing full intermediate objects.

```csharp
// Modify only the name field on the existing User at index 2 (no new User created)
reflector.TryModifyAt(ref database, "users/[2]/name", "Bob");

// Partial update of a complex object — only listed fields are touched
var patch = new SerializedMember { typeName = typeof(User).GetTypeId() };
patch.SetFieldValue(reflector, "name", "Bob");
reflector.TryModifyAt(ref database, "users/[2]", patch);

// JSON patch — modify multiple fields at different depths at once
reflector.TryPatch(ref database, """
{
  "admin": { "name": "Carol" },
  "users": { "[2]": { "name": "Bob", "email": "bob@example.com" } },
  "config": { "[maxRetries]": 5 }
}
""");

// Type replacement — replace StringData field with derived StringDataAdvanced
var advanced = SerializedMember.FromValue(reflector, typeof(StringDataAdvanced), instance);
reflector.TryModifyAt(ref database, "admin/displayName", advanced);
```

---

## Path Format

| Segment form | Meaning |
|---|---|
| `fieldName` or `PropertyName` | Field or property by name (plain, no brackets) |
| `[i]` where inner value is integer AND obj is Array/IList | Array index |
| `[key]` where obj is `IDictionary<string, T>` | Dictionary string key |
| `[key]` where obj is `IDictionary<int, T>` | Dictionary integer key |

**Disambiguation**: if the segment starts with `[…]`, check runtime type of `obj`:

- `Array` / `IList<T>` → integer index (inner must parse as int)
- `IDictionary<K, V>` → key (parse inner to K via `Convert.ChangeType`)
- Neither → log detailed error and return false

Leading `#/` stripped automatically (compatible with `SerializationContext` path format).

---

## Error Messages

Every navigation failure must include the segment name explicitly:

```
Segment 'admin' not found on type 'Database'.
Available fields: admin, users, config
Available properties: Id, Name
```

```
Bracket segment '[99]' index out of range on type 'User[]'. Array length is 3.
```

```
Bracket segment '[badKey]' cannot be used as index on type 'User[]'. Expected integer index.
```

```
Bracket segment '[myKey]' cannot be used as key on type 'Database'.
Type is not an array, list, or dictionary.
```

Errors are accumulated in the `Logs` object (same pattern as `TryModify`) — not thrown as exceptions.

---

## Implementation

### 1. `Reflector.ModifyAt.cs` — new partial file

**Location**: `ReflectorNet/src/Reflector/Reflector.ModifyAt.cs`

#### Public API

```csharp
// Primary overload — full control via SerializedMember
public bool TryModifyAt(
    ref object? obj, string path, SerializedMember value,
    Type? fallbackObjType = null, int depth = 0, Logs? logs = null,
    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
    ILogger? logger = null)

// Convenience generic overload — ideal for leaf/primitive targets
public bool TryModifyAt<T>(
    ref object? obj, string path, T value,
    Type? fallbackObjType = null, int depth = 0, Logs? logs = null,
    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
    ILogger? logger = null)
// body: SerializedMember.FromValue<T>(this, value) → call primary overload
```

#### Path parsing helpers (private static)

```csharp
private static string[] ParsePath(string path)
// Strip "#/" prefix. Split on '/'. Use string.Split(new char[]{'/'}, RemoveEmptyEntries)
// for netstandard2.1 compatibility.

private static bool TryParseBracketSegment(string segment, out string innerKey)
// Returns true if segment is "[anything]"; innerKey = content between brackets.
```

#### Core algorithm — `TryModifyAt` (primary)

```
1. segments = ParsePath(path)
2. If empty → terminal: return TryModify(ref obj, value, fallbackObjType, ...)
3. segment = segments[0]; remainingPath = join(segments[1..], "/")
4. If TryParseBracketSegment(segment, out innerKey):
       → TryModifyAtBracketed(ref obj, segment, innerKey, remainingPath, value, ...)
   Else:
       → TryModifyAtMember(ref obj, segment, remainingPath, value, ...)
```

#### `TryModifyAtBracketed` (private)

Dispatches on runtime type of `obj`:

```
if obj is null:
    logs.Error("Cannot navigate segment '...' on null object.", depth)
    return false

if obj is Array or IList<T>:
    if !int.TryParse(innerKey, out int idx):
        logs.Error("Bracket segment '[innerKey]' cannot be used as index on type '...'.
                    Expected integer index.", depth)
        return false
    → TryModifyAtArrayIndex(ref obj, segment, idx, remainingPath, value, ...)

elif TypeUtils.IsDictionary(objType):
    args = TypeUtils.GetDictionaryGenericArguments(objType)
    keyType = args[0]
    try: dictKey = Convert.ChangeType(innerKey, keyType)
    catch: logs.Error("Bracket segment '[innerKey]' cannot be converted to
                       key type '...' on type '...'.", depth); return false
    → TryModifyAtDictKey(ref obj, segment, dictKey, remainingPath, value, ...)

else:
    logs.Error("Bracket segment '[innerKey]' cannot be used on type '...'.
                Type is not an array, list, or dictionary.", depth)
    return false
```

#### `TryModifyAtArrayIndex` (private)

```
1. if idx < 0 or idx >= length:
       logs.Error("Bracket segment '[i]' index out of range on type '...'.
                   Array length is N.", depth)
       return false
2. elementType = TypeUtils.GetEnumerableItemType(objType)
3. currentElement = array.GetValue(idx) or list[idx]
4. Type-replacement check (see below, only if remainingPath is empty)
5. success = TryModifyAt(ref currentElement, remainingPath, value, elementType, depth+1, ...)
6. if success: array.SetValue(currentElement, idx) or list[idx] = currentElement
7. return success
```

#### `TryModifyAtDictKey` (private)

```
1. dict = (IDictionary)obj
2. currentElement = dict.Contains(dictKey) ? dict[dictKey] : null
3. valueType = GetDictionaryGenericArguments(objType)[1]
4. Type-replacement check (see below, only if remainingPath is empty)
5. success = TryModifyAt(ref currentElement, remainingPath, value, valueType, depth+1, ...)
6. if success: dict[dictKey] = currentElement
7. return success
```

#### `TryModifyAtMember` (private)

```
1. Try TypeMemberUtils.GetField(objType, flags, memberName):
       if not found → go to step 2
       currentValue = fieldInfo.GetValue(obj)
       Type-replacement check (see below, only if remainingPath is empty)
       success = TryModifyAt(ref currentValue, remainingPath, value, fieldInfo.FieldType, depth+1, ...)
       if success: fieldInfo.SetValue(obj, currentValue)   // struct-safe (mirrors BaseReflectionConverter.Modify.cs:342)
       return success

2. Try TypeMemberUtils.GetProperty(objType, flags, memberName):
       if not found → go to step 3
       check CanWrite; if not: logs.Error("Property '...' is read-only.", depth); return false
       currentValue = propInfo.GetValue(obj)
       Type-replacement check (see below, only if remainingPath is empty)
       success = TryModifyAt(ref currentValue, remainingPath, value, propInfo.PropertyType, depth+1, ...)
       if success: propInfo.SetValue(obj, currentValue)
       return success

3. // Neither field nor property found
   (fieldNames, propNames) = GetCachedSerializableMemberNames(reflector, objType, flags, logger)
   logs.Error(
     "Segment 'memberName' not found on type 'objType'.\n"
     + "Available fields: field1, field2, ...\n"
     + "Available properties: prop1, prop2, ...", depth)
   return false
```

#### Type-replacement check (applied in array/dict/member helpers)

Only at **terminal step** (`string.IsNullOrEmpty(remainingPath)`):

```csharp
if (string.IsNullOrEmpty(remainingPath))
{
    var desiredType = TypeUtils.GetTypeWithNamePriority(value, declaredType, out _);
    if (desiredType != null
        && currentValue != null
        && desiredType != currentValue.GetType()
        && declaredType.IsAssignableFrom(desiredType))
    {
        currentValue = null; // force fresh instance creation via TryModify's null branch
    }
}
```

---

### 2. `Reflector.Patch.cs` — new partial file (JSON patch)

**Location**: `ReflectorNet/src/Reflector/Reflector.Patch.cs`

Modifies multiple fields at different depths in a single call using a JSON document as the patch descriptor. Follows **JSON Merge Patch** semantics (RFC 7396) extended with bracket-notation keys for array/dictionary access.

#### Public API

```csharp
// JsonElement overload (primary)
public bool TryPatch(
    ref object? obj, JsonElement patch,
    Type? fallbackObjType = null, int depth = 0, Logs? logs = null,
    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
    ILogger? logger = null)

// String convenience overload (parses JSON internally)
public bool TryPatch(
    ref object? obj, string json,
    Type? fallbackObjType = null, int depth = 0, Logs? logs = null,
    BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
    ILogger? logger = null)
// body: parse json → JsonDocument → call JsonElement overload
```

#### Patch document format

- **JSON object** → navigate: each key is a path segment (field name, `[i]`, `[key]`).
- **JSON primitive / array** → set value: deserialize and assign to current node.
- **`"$type"` key** inside a JSON object → optional type hint for type replacement. Value is the full type name string (same as `SerializedMember.typeName`).

Example:

```json
{
  "admin": {
    "$type": "com.MyApp.AdminUser",
    "name": "Carol",
    "level": 3
  },
  "users": {
    "[2]": {
      "email": "bob@example.com"
    }
  },
  "config": {
    "[maxRetries]": 5,
    "[timeout]": 30
  }
}
```

#### Core algorithm — `TryPatchInternal` (recursive, private)

```
TryPatchInternal(ref obj, patch JsonElement, objType, depth, logs, flags, logger):

  1. If patch.ValueKind == Null:
       obj = null; return true

  2. If patch.ValueKind != Object (primitive, bool, array):
       // Leaf value — set directly using existing TryModify
       var member = SerializedMember.FromJson(objType ?? obj?.GetType(), patch)
       return TryModify(ref obj, member, objType, depth, logs, flags, logger)

  3. // patch is a JSON object — navigate its keys
     Extract optional "$type" from patch properties
     Apply type-replacement check using $type (same logic as TryModifyAt)

     var overallSuccess = true
     foreach property in patch.EnumerateObject():
       if property.Name == "$type": continue  // already consumed above

       if TryParseBracketSegment(property.Name, out innerKey):
         success = TryPatchBracketed(ref obj, property.Name, innerKey, property.Value, ...)
       else:
         success = TryPatchMember(ref obj, property.Name, property.Value, ...)

       overallSuccess &= success

     return overallSuccess
```

**`TryPatchMember`** and **`TryPatchBracketed`**: same navigation logic as `TryModifyAtMember` / `TryModifyAtBracketed`, but the "value" is a `JsonElement` subtree rather than a `SerializedMember`. At each level, call `TryPatchInternal` recursively.

Error messages follow the same pattern as `TryModifyAt` (segment name always explicit).

---

### 3. `ArrayReflectionConverter.Modify.cs` — new partial file

**Location**: `ReflectorNet/src/Converter/Reflection/ArrayReflectionConverter.Modify.cs`

Overrides `TryModify` so the existing API (without a path string) also supports partial array element modification when `data.fields` contains `[i]`-named entries.

**Algorithm**:

```
override TryModify(...):
  1. If data.valueJsonElement is present → base.TryModify (full replacement, unchanged)
  2. Partition data.fields into: indexedFields (IsArrayIndexName) and non-indexed
  3. If no indexedFields → base.TryModify (unchanged)
  4. Validate obj is non-null; elementType = TypeUtils.GetEnumerableItemType(type)
  5. For each indexedField:
       idx = ParseArrayIndex(indexedField.name); bounds check with explicit error
       currentElement = array[idx]
       reflector.TryModify(ref currentElement, indexedField, elementType, depth+1, ...)
       if success: array[idx] = currentElement
  6. Return AND of all results
```

Private helpers:

```csharp
private static bool IsArrayIndexName(string? name)  // "[digits]" only
private static int ParseArrayIndex(string name)
```

---

### 4. Tests — `AtomicModifyTests.cs`

**Location**: `ReflectorNet.Tests/src/ReflectorTests/AtomicModifyTests.cs`

Uses `SolarSystem` / `SolarSystem.CelestialBody` / `GameObjectRef`.

| Test | Method | Path / JSON |
|---|---|---|
| `TryModifyAt_RootField` | `TryModifyAt<float>` | `"globalOrbitSpeedMultiplier"` |
| `TryModifyAt_TwoLevelField` | `TryModifyAt<int>` | `"sun/instanceID"` |
| `TryModifyAt_ArrayElementField` | `TryModifyAt<float>` | `"celestialBodies/[0]/orbitRadius"` → only `[0]` changes |
| `TryModifyAt_InvalidMember_DetailedError` | `TryModifyAt<float>` | `"doesNotExist"` → false, Logs has "Segment 'doesNotExist' not found" |
| `TryModifyAt_OutOfBoundsIndex_DetailedError` | `TryModifyAt<float>` | `"celestialBodies/[99]/orbitRadius"` → false, Logs has "'[99]' index out of range" |
| `TryModifyAt_DictionaryStringKey` | `TryModifyAt<int>` | `"config/[timeout]"` on `Dictionary<string,int>` |
| `TryModifyAt_DictionaryIntKey` | `TryModifyAt<string>` | `"lookup/[3]"` on `Dictionary<int,string>` |
| `TryModifyAt_TypeReplacement` | `TryModifyAt(SerializedMember)` | replace base type field with derived |
| `TryModifyAt_PartialPatch` | `TryModifyAt(SerializedMember)` | navigate to `[0]` and apply partial fields |
| `TryPatch_MultipleFieldsAtOnce` | `TryPatch(string json)` | JSON with two nested modifications at once |
| `TryPatch_ArrayElementAndField` | `TryPatch(JsonElement)` | `"[2]"` key navigates into array element |
| `TryPatch_WithTypeHint` | `TryPatch(string json)` | `"$type"` key triggers type replacement |
| `ArrayConverter_PartialElementModify` | `TryModify(SerializedMember)` | `[1]`-named field entry → only that element changes |

---

## Files Created (no existing files modified)

| File | Purpose |
|---|---|
| `ReflectorNet/src/Reflector/Reflector.ModifyAt.cs` | Path-based single-target modification |
| `ReflectorNet/src/Reflector/Reflector.Patch.cs` | JSON patch document — multi-path modification |
| `ReflectorNet/src/Converter/Reflection/ArrayReflectionConverter.Modify.cs` | Partial array element modification via `TryModify` |
| `ReflectorNet.Tests/src/ReflectorTests/AtomicModifyTests.cs` | Tests for all new functionality |

---

## Key Reused Utilities

| Utility | File | Used for |
|---|---|---|
| `TypeMemberUtils.GetField / GetProperty` | `src/Utils/TypeMemberUtils.cs` | member lookup |
| `TypeUtils.GetEnumerableItemType(type)` | `src/Utils/TypeUtils.Collections.cs` | array element type |
| `TypeUtils.IsDictionary(type)` | `src/Utils/TypeUtils.Collections.cs` | dictionary detection |
| `TypeUtils.GetDictionaryGenericArguments(type)` | `src/Utils/TypeUtils.Collections.cs` | key/value types |
| `TypeUtils.GetTypeWithNamePriority(data, fallback, out _)` | `src/Utils/TypeUtils.GetType.cs` | type replacement / `$type` resolution |
| `declaredType.IsAssignableFrom(desiredType)` | `src/Utils/TypeUtils.Helpers.cs` | assignability check |
| `StringUtils.GetPadding(depth)` | `src/Utils/StringUtils.cs` | log indentation |
| `SerializedMember.FromValue<T>(reflector, value)` | `src/Model/SerializedMember.Static.cs` | generic overload factory |
| `SerializedMember.FromJson(type, jsonElement)` | `src/Model/SerializedMember.Static.cs` | JSON-to-member for `TryPatch` leaves |
| `GetCachedSerializableMemberNames(...)` | `BaseReflectionConverter.Modify.cs` | "Available fields/props" in error messages |
| `IsGenericList(type, out elementType)` | `ArrayReflectionConverter.cs` | reused in override |

---

## Design Notes

**"Truly atomic"**: `TryModifyAt(ref db, "users/[2]/name", "Bob")` navigates directly to the `name` field on the existing `User` at index 2. No new User is created, no other field on the User changes, and no other element in the array is touched.

**`TryPatch` for multi-field**: when multiple fields at different depths need to change in a single operation, use `TryPatch` with a JSON document. The JSON keys drive navigation, leaf values are set directly.

**Type info in `TryPatch`**: the `"$type"` key inside any JSON object node specifies the desired type (full type name). When the desired type is a subtype of the declared type, the existing instance is discarded and a new one is created — enabling polymorphic replacement in JSON form.

**`Logs` throughout**: every public method takes `Logs? logs` and accumulates errors using the same depth-aware pattern as `TryModify`. Errors include explicit segment names. Nothing is thrown.

**No changes to existing files**: all new functionality is additive through new partial-class files and a new test file.

---

## Verification

```bash
dotnet build --configuration Release
dotnet test --configuration Release --filter "FullyQualifiedName~AtomicModifyTests"
dotnet test --configuration Release --verbosity normal
```
