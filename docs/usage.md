# ReflectorNet usage guide

The reference behind the [README](../README.md). All examples use one `Reflector` and these types:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;

var reflector = new Reflector();
var logs = new Logs(); // optional: collects errors and warnings instead of throwing

public class Player
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public List<Item> Items { get; set; } = new();
    public Dictionary<string, int> Stats { get; set; } = new();
}

public class Item
{
    [Description("Item identifier, for example 'potion'.")]
    public string Id { get; set; } = string.Empty;
    public int Count { get; set; }
}

public static class Shop
{
    [Description("Buy items and return a receipt.")]
    public static string Buy(string itemId, int count) => $"Bought {count} x {itemId}";
}
```

## Contents

- [Serialization](#serialization)
- [Paths](#paths)
- [Reading and searching](#reading-and-searching)
- [Changing objects in place](#changing-objects-in-place)
- [Finding and calling methods](#finding-and-calling-methods)
- [JSON Schema](#json-schema)
- [Converters and the type blacklist](#converters-and-the-type-blacklist)

## Serialization

`Serialize` turns a live object into a `SerializedMember`. Complex objects get nested `fields` and
`props` entries; primitives and converter-backed values go into the `value` JSON payload.
`Deserialize` builds the object back.

```csharp
SerializedMember data = reflector.Serialize(player, name: "player");
string json = reflector.JsonSerializer.Serialize(data);

var parsed = reflector.JsonSerializer.Deserialize<SerializedMember>(json)!;
Player? copy = reflector.Deserialize<Player>(parsed);
```

Serialization tracks visited objects and writes `$ref` entries for repeated references, so cyclic
graphs do not recurse forever. Read a primitive back with `member.GetValue<T>(reflector)`.

## Paths

Path-based APIs (`TryReadAt`, `TryModifyAt`, `View`) use slash-delimited paths:

| Segment | Meaning | Example |
|---|---|---|
| `Name` | Field or property | `Items` |
| `[0]` | Array or `IList` index | `Items/[0]/Count` |
| `[key]` | Dictionary key | `Stats/[hp]` |

A leading `#/` is accepted and stripped, so `$ref` paths from serialization work as input.

## Reading and searching

```csharp
// One value
if (reflector.TryReadAt(player, "Items/[0]/Count", out SerializedMember? count, logs: logs))
    Console.WriteLine(count!.GetValue<int>(reflector));

// A filtered tree of the whole object or a subtree
SerializedMember? view = reflector.View(player, new ViewQuery { NamePattern = "Name|Level", MaxDepth = 1 });

// Flat path/value matches anywhere in the live graph, including list items.
// Prints "Items/[0]/Count = 3", ...
foreach (ViewMatch match in reflector.Grep(player, "^Count$"))
    Console.WriteLine($"{match.Path} = {match.Value.GetValue<int>(reflector)}");
```

`ViewQuery` options:

| Option | Description |
|---|---|
| `Path` | Navigate to a subtree before serializing. |
| `MaxDepth` | Limit the tree depth. `0` returns only the root envelope. |
| `NamePattern` | Case-insensitive .NET regex matched against field and property names. |
| `TypeFilter` | Keep branches whose type is assignable to this `Type`. |

When the filters match nothing, `View` still returns the root envelope, so the caller sees the type.
`View` filters the serialized tree; use `Grep` to search inside arrays and lists.

## Changing objects in place

All modify APIs take `ref object?` and keep the root object identity where possible. They return
`false` and write details to `Logs` instead of throwing.

**One path** with `TryModifyAt`:

```csharp
object? target = player;

reflector.TryModifyAt<int>(ref target, "Items/[0]/Count", 10, logs: logs);
reflector.TryModifyAt<int>(ref target, "Stats/[hp]", 100, logs: logs); // adds the key if missing

// Apply a partial SerializedMember to a complex node
var itemPatch = new SerializedMember { typeName = typeof(Item).GetTypeId() };
itemPatch.SetPropertyValue(reflector, "Count", 20);
reflector.TryModifyAt(ref target, "Items/[0]", itemPatch, logs: logs);
```

**Several values** with `TryPatch` (JSON Merge Patch style):

```csharp
reflector.TryPatch(ref target, """
{
  "Level": 9,
  "Items": { "[0]": { "Count": 15 } }
}
""", logs: logs);
```

- Object keys navigate into fields, properties, list indexes (`"[0]"`) or dictionary keys.
- Scalar values set the value; `null` sets `null` where the type allows it.
- `"$type"` replaces the current value with a compatible subtype before the other keys apply.
- Invalid JSON, unknown members, read-only properties and failed key conversions return `false`.

**A whole `SerializedMember`** with `TryModify`:

```csharp
var patch = new SerializedMember { typeName = typeof(Player).GetTypeId() };
patch.SetPropertyValue(reflector, "Level", 12);
reflector.TryModify(ref target, patch, logs: logs);
```

## Finding and calling methods

`FindMethod` searches the loaded assemblies for methods that match a `MethodRef`:

```csharp
IEnumerable<MethodInfo> methods = reflector.FindMethod(
    new MethodRef { TypeName = "Shop", MethodName = "Buy" },
    typeNameMatchLevel: 6,
    methodNameMatchLevel: 6);
```

Match levels (the minimum score a name must reach):

| Level | Match |
|---|---|
| `6` | Exact, case-sensitive |
| `5` | Exact, case-insensitive |
| `4` | Prefix, case-sensitive |
| `3` | Prefix, case-insensitive |
| `2` | Contains, case-sensitive |
| `1` | Contains, case-insensitive (default) |
| `0` | No filtering on this name |

Set `MethodRef.Namespace` with `knownNamespace: true` to search one namespace, and
`parametersMatchLevel` to match parameter names and types.

`MethodCall` finds the method, deserializes the arguments, invokes it and returns the result as text:

```csharp
string result = reflector.MethodCall(
    reflector,
    new MethodRef { TypeName = "shop", MethodName = "buy" }, // fuzzy names work with the default levels
    inputParameters: new SerializedMemberList
    {
        reflector.Serialize("potion", name: "itemId"),
        reflector.Serialize(2, name: "count")
    },
    executeInMainThread: false);
```

The result starts with `[Success]` and the JSON value, or describes the error: no match, several
candidates to choose from, an argument that failed to deserialize (the method is then not called),
or the exception thrown. For instance methods, pass `targetObject` as a `SerializedMember`;
without it ReflectorNet tries to create an instance of the declaring type.

To host your own tools, `MethodWrapper` wraps one `MethodInfo` with its `InputSchema`,
`OutputSchema` and `Description` and invokes it with named arguments (`InvokeDict`).

## JSON Schema

```csharp
JsonNode typeSchema = reflector.GetSchema<Player>();    // full schema with $defs
JsonNode typeRef = reflector.GetSchemaRef<Player>();    // {"$ref":"#/$defs/Player"}

MethodInfo buy = typeof(Shop).GetMethod(nameof(Shop.Buy))!;
JsonNode input = reflector.GetArgumentsSchema(buy);     // arguments as one object schema
JsonNode? output = reflector.GetReturnSchema(buy);      // {"type":"object","properties":{"result":...}}
```

Covered: fields and properties, primitives, collections, dictionaries, generics and nested types;
`$defs` reuse; nullable and optional parameters; `Task<T>` and `ValueTask<T>` return types are
unwrapped; `[Description]` text on members; custom output through `IJsonSchemaConverter`.

## Converters and the type blacklist

A priority-based registry picks the reflection converter for each type: every `IReflectionConverter`
scores how well it handles a type, and the highest score wins. Built in:
`PrimitiveReflectionConverter`, `ArrayReflectionConverter`, `GenericReflectionConverter<T>`,
`TypeReflectionConverter` and `AssemblyReflectionConverter`.

```csharp
var reflector = new Reflector();
reflector.Converters.Add(new MyReflectionConverter());        // custom traversal, creation or mutation
reflector.JsonSerializer.AddConverter(new MyJsonConverter()); // custom JSON or schema
```

Add JSON converters right after creating the `Reflector`: `System.Text.Json` locks its options after
the first serialization, and a later `AddConverter` call throws `InvalidOperationException`.

Base classes for your own converters: `GenericReflectionConverter<T>`,
`LazyGenericReflectionConverter` (optional dependency resolved by type name),
`IgnoreFieldsAndPropertiesReflectionConverter<T>` (shallow types) and `JsonSchemaConverter<T>`.

Exclude types from serialization and schemas:

```csharp
reflector.Converters.BlacklistType(typeof(ExpensiveRuntimeType));
reflector.Converters.BlacklistTypes("Some.Namespace.InternalType");
reflector.Converters.BlacklistTypeInAssembly("MyCompany.Game", "MyCompany.Game.SecretState");
```

The check covers base types, interfaces, array element types and generic arguments.
