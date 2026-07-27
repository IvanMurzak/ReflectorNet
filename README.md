# ReflectorNet

[![NuGet](https://img.shields.io/nuget/v/com.IvanMurzak.ReflectorNet?label=NuGet&labelColor=333A41)](https://www.nuget.org/packages/com.IvanMurzak.ReflectorNet/)
[![netstandard2.1](https://img.shields.io/badge/.NET-netstandard2.1-blue?logoColor=white&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue?logoColor=white&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-blue?logoColor=white&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet)
[![Tests](https://github.com/IvanMurzak/ReflectorNet/actions/workflows/release.yml/badge.svg?branch=main)](https://github.com/IvanMurzak/ReflectorNet/actions/workflows/release.yml)

[![Stars](https://img.shields.io/github/stars/IvanMurzak/ReflectorNet 'Stars')](https://github.com/IvanMurzak/ReflectorNet/stargazers)
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/Cgs6nM8BPU)
[![License](https://img.shields.io/github/license/IvanMurzak/ReflectorNet?label=License&labelColor=333A41)](https://github.com/IvanMurzak/ReflectorNet/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

ReflectorNet is a .NET reflection toolkit for dynamic automation, AI-assisted tooling, testing, and runtime inspection. It can serialize live objects with type metadata, deserialize them back, modify existing instances in place, inspect object graphs by path or pattern, generate JSON Schema, and discover or invoke methods from partial runtime descriptions.

The main entry point is `Reflector`. The central data model is `SerializedMember`, a JSON-friendly representation of a .NET value that preserves the member name, resolved type, fields, properties, and raw JSON value payload.

## Index

- [ReflectorNet](#reflectornet)
  - [Index](#index)
  - [Why ReflectorNet](#why-reflectornet)
  - [Installation](#installation)
  - [Target Frameworks](#target-frameworks)
  - [Quick Start](#quick-start)
  - [Core Features](#core-features)
    - [Type-Preserving Serialization](#type-preserving-serialization)
    - [In-Place Modification](#in-place-modification)
    - [Path Syntax](#path-syntax)
  - [Object Inspection](#object-inspection)
    - [`TryReadAt`](#tryreadat)
    - [`View`](#view)
    - [`Grep`](#grep)
  - [Object Modification](#object-modification)
    - [`TryModifyAt`](#trymodifyat)
    - [`TryPatch`](#trypatch)
  - [Dynamic Method Workflows](#dynamic-method-workflows)
    - [Find Methods](#find-methods)
    - [Invoke Methods](#invoke-methods)
  - [JSON Schema Generation](#json-schema-generation)
  - [Converters and Extensibility](#converters-and-extensibility)
  - [Project Layout](#project-layout)
  - [Development](#development)
  - [License](#license)

## Why ReflectorNet

Standard reflection is powerful, but it is low-level. ReflectorNet wraps reflection in higher-level operations that are useful when the caller does not have a compiled, strongly typed integration path.

Key capabilities:

- Type-preserving serialization through `SerializedMember`.
- Full object reconstruction with flexible type resolution.
- In-place object modification without replacing the root reference.
- Path-based reads and writes for fields, properties, list items, array items, and dictionary entries.
- JSON Merge Patch style updates for multi-field modifications.
- Regex-based object graph search with `Grep`.
- JSON Schema generation for types, method arguments, and method return values.
- Fuzzy method discovery and dynamic invocation through `MethodRef` and `MethodCall`.
- Extensible reflection and JSON converter registries.
- Optional type blacklisting for excluding unsafe, irrelevant, or expensive types from reflection workflows.

## Installation

```bash
dotnet add package com.IvanMurzak.ReflectorNet
```

## Target Frameworks

The library targets:

- `netstandard2.1`
- `net8.0`
- `net9.0`

The test suite targets `net8.0` and `net9.0`.

## Quick Start

```csharp
using System.Collections.Generic;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;

var reflector = new Reflector();

var player = new PlayerState
{
    Name = "Ada",
    Level = 7,
    Inventory = new Inventory
    {
        Items = new List<ItemStack>
        {
            new ItemStack { ItemId = "health_potion", Quantity = 3 }
        }
    }
};

SerializedMember snapshot = reflector.Serialize(player);
PlayerState? copy = reflector.Deserialize<PlayerState>(snapshot);

object? liveObject = player;
var logs = new Logs();

reflector.TryModifyAt<int>(
    ref liveObject,
    "Inventory/Items/[0]/Quantity",
    10,
    logs: logs);

reflector.TryReadAt(
    liveObject,
    "Inventory/Items/[0]/Quantity",
    out SerializedMember? quantity);

Console.WriteLine(quantity?.GetValue<int>(reflector));

public sealed class PlayerState
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public Inventory Inventory { get; set; } = new Inventory();
}

public sealed class Inventory
{
    public List<ItemStack> Items { get; set; } = new List<ItemStack>();
}

public sealed class ItemStack
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
```

## Core Features

### Type-Preserving Serialization

`Reflector.Serialize` turns a live object into a `SerializedMember`. For complex objects, fields and properties are represented as nested `SerializedMember` entries. Primitive and converter-backed values are stored in the `value` JSON payload.

```csharp
var data = reflector.Serialize(player, name: "player");
string json = reflector.JsonSerializer.Serialize(data);
```

`Reflector.Deserialize` reconstructs the object from that representation.

```csharp
var restored = reflector.Deserialize<PlayerState>(data);
```

ReflectorNet also tracks visited objects during serialization and can emit `$ref` entries for repeated references, helping avoid endless recursion in cyclic object graphs.

### In-Place Modification

`TryModify` applies a `SerializedMember` onto an existing object instance.

```csharp
object? target = player;
var patch = new SerializedMember
{
    typeName = typeof(PlayerState).GetTypeId()
};

patch.SetPropertyValue(reflector, "Level", 8);

bool ok = reflector.TryModify(ref target, patch, logs: logs);
```

This is useful for stateful systems such as games, editors, services, and test harnesses where keeping object identity matters.

### Path Syntax

Path-based APIs use slash-delimited paths:

| Segment | Meaning | Example |
| --- | --- | --- |
| `Name` | Field or property | `Inventory` |
| `[0]` | Array or `IList` index | `Items/[0]` |
| `[key]` | Dictionary key | `Settings/[timeout]` |

A leading `#/` is accepted and stripped, which makes paths compatible with `SerializationContext` reference paths.

## Object Inspection

### `TryReadAt`

`TryReadAt` navigates to one value and serializes only that target.

```csharp
if (reflector.TryReadAt(player, "Inventory/Items/[0]/ItemId", out var itemId))
{
    Console.WriteLine(itemId!.GetValue<string>(reflector));
}
```

Invalid paths return `false` and write details into `Logs` when supplied.

### `View`

`View` returns a serialized tree for the whole object or a navigated subtree, with optional filters.

```csharp
SerializedMember? view = reflector.View(player, new ViewQuery
{
    Path = "Inventory",
    NamePattern = "Item|Quantity",
    MaxDepth = 3
});
```

`ViewQuery` supports:

| Option | Description |
| --- | --- |
| `Path` | Navigate before serialization. |
| `MaxDepth` | Limit the returned tree depth. `0` returns only the root envelope. |
| `NamePattern` | Case-insensitive .NET regex matched against field and property names. |
| `TypeFilter` | Keep branches whose resolved type is assignable to the supplied `Type`. |

When filters match nothing, `View` keeps the root envelope so callers still know what object type was inspected.

### `Grep`

`Grep` searches the live object graph for matching field or property names and returns flat path/value matches.

```csharp
IReadOnlyList<ViewMatch> matches = reflector.Grep(player, "^Quantity$");

foreach (var match in matches)
{
    Console.WriteLine($"{match.Path}: {match.Value.GetValue<int>(reflector)}");
}
```

Use `Grep` when you need to search inside arrays or lists. `View` filters the serialized tree; `Grep` walks the live object graph.

## Object Modification

### `TryModifyAt`

`TryModifyAt` changes one target path without touching sibling values.

```csharp
object? target = player;

reflector.TryModifyAt<int>(
    ref target,
    "Inventory/Items/[0]/Quantity",
    12,
    logs: logs);
```

The same path syntax works for object members, lists, arrays, and dictionaries. For dictionaries, missing keys can be added when the key can be converted to the dictionary key type.

You can also apply a partial `SerializedMember` to a complex node:

```csharp
var itemPatch = new SerializedMember
{
    typeName = typeof(ItemStack).GetTypeId()
};

itemPatch.SetPropertyValue(reflector, "Quantity", 20);

reflector.TryModifyAt(
    ref target,
    "Inventory/Items/[0]",
    itemPatch,
    logs: logs);
```

### `TryPatch`

`TryPatch` applies a JSON Merge Patch style document. It is useful when multiple values need to be updated in one call.

```csharp
reflector.TryPatch(ref target, """
{
  "Level": 9,
  "Inventory": {
    "Items": {
      "[0]": {
        "Quantity": 15
      }
    }
  }
}
""", logs: logs);
```

Patch behavior:

- JSON object keys navigate into fields, properties, array/list indexes, or dictionary keys.
- JSON scalar values set the current value.
- `null` sets the current value to `null` when the target type allows it.
- `$type` can request a compatible subtype replacement before applying the remaining keys.
- Invalid JSON, unknown members, incompatible type hints, read-only properties, and failed key conversions return `false` and are reported through `Logs`.

## Dynamic Method Workflows

### Find Methods

`FindMethod` searches loaded assemblies for methods matching a `MethodRef`. Matching can be exact or fuzzy.

```csharp
using com.IvanMurzak.ReflectorNet.Model;

var filter = new MethodRef
{
    Namespace = typeof(PlayerCommands).Namespace,
    TypeName = "PlayerCommands",
    MethodName = "GrantItem"
};

var methods = reflector.FindMethod(
    filter,
    knownNamespace: true,
    typeNameMatchLevel: 6,
    methodNameMatchLevel: 6);
```

String match levels:

| Level | Match |
| --- | --- |
| `6` | Exact, case-sensitive |
| `5` | Exact, case-insensitive |
| `4` | Prefix, case-sensitive |
| `3` | Prefix, case-insensitive |
| `2` | Contains, case-sensitive |
| `1` | Contains, case-insensitive |
| `0` | Disabled |

Parameter matching can also be enabled with `parametersMatchLevel`.

### Invoke Methods

`MethodCall` combines method discovery, parameter deserialization, target instance handling, invocation, and JSON result formatting.

```csharp
var args = new SerializedMemberList
{
    reflector.Serialize("health_potion", name: "itemId"),
    reflector.Serialize(2, name: "quantity")
};

string result = reflector.MethodCall(
    reflector,
    new MethodRef
    {
        TypeName = "PlayerCommands",
        MethodName = "GrantItem"
    },
    inputParameters: args,
    executeInMainThread: false);
```

For instance methods, pass `targetObject` as a serialized object. If no target is supplied, ReflectorNet attempts to create an instance of the declaring type.

## JSON Schema Generation

ReflectorNet can generate JSON Schema for types and methods. This is especially useful for AI function calling, tooling UIs, runtime validation, and API documentation.

```csharp
using System.Reflection;

var typeSchema = reflector.GetSchema<PlayerState>();
var typeRef = reflector.GetSchemaRef<PlayerState>();

MethodInfo method = typeof(PlayerCommands).GetMethod(nameof(PlayerCommands.GrantItem))!;

var inputSchema = reflector.GetArgumentsSchema(method);
var outputSchema = reflector.GetReturnSchema(method);
```

Schema generation supports:

- Fields and properties discovered through the reflection converter chain.
- Primitive, collection, dictionary, generic, and nested types.
- `$defs` reuse for complex types.
- Nullable and optional method parameter handling.
- Return type unwrapping for `Task<T>` and `ValueTask<T>`.
- Descriptions from `DescriptionAttribute`.
- Custom schema output through `IJsonSchemaConverter`.

## Converters and Extensibility

ReflectorNet uses a priority-based converter registry. Each `IReflectionConverter` reports how well it can handle a type, and the registry selects the highest-priority converter.

Default reflection converters include:

- `PrimitiveReflectionConverter` for primitive and common value types.
- `ArrayReflectionConverter` for arrays and list-like collections.
- `GenericReflectionConverter<object>` for ordinary classes and structs.
- `TypeReflectionConverter` for `System.Type`.
- `AssemblyReflectionConverter` for `System.Reflection.Assembly`.

Register a reflection converter when a type needs custom object traversal, creation, or mutation behavior:

```csharp
using com.IvanMurzak.ReflectorNet.Converter;

reflector.Converters.Add(new MyReflectionConverter());
```

Register a JSON converter when a type needs custom JSON transport or schema behavior:

```csharp
reflector.JsonSerializer.AddConverter(new MyJsonConverter());
```

Useful built-in extension points:

- `GenericReflectionConverter<T>` for normal custom object handling.
- `LazyGenericReflectionConverter` for optional runtime dependencies resolved by type name.
- `IgnoreFieldsAndPropertiesReflectionConverter<T>` for treating selected types as shallow or read-only.
- `IJsonSchemaConverter` and `JsonSchemaConverter<T>` for custom JSON Schema definitions.

Types can be excluded from reflection processing through the registry blacklist:

```csharp
reflector.Converters.BlacklistType(typeof(ExpensiveRuntimeType));
reflector.Converters.BlacklistTypes("Some.Namespace.InternalType");
reflector.Converters.BlacklistTypeInAssembly("MyCompany.Game", "MyCompany.Game.SecretState");
```

Blacklist checks include inheritance, implemented interfaces, arrays, and generic type arguments.

## Project Layout

```text
ReflectorNet/
  ReflectorNet/                    Main library project
  ReflectorNet.Tests/              xUnit tests
  ReflectorNet.Tests.OuterAssembly/ Cross-assembly test models
  ConsoleApp/                      Manual schema and behavior checks
  docs/                            Maintainer notes and architecture documentation
  commands/                        Release and version helper scripts
```

Important library areas:

- `src/Reflector/` contains the `Reflector` partial class split by responsibility.
- `src/Model/` contains `SerializedMember`, `SerializedMemberList`, `MethodRef`, `MethodData`, `Logs`, and view models.
- `src/Converter/Reflection/` contains the reflection converter chain.
- `src/Converter/Json/` contains System.Text.Json converters and schema-aware converters.
- `src/Utils/Json/` contains JSON serialization and schema generation utilities.

## Development

Restore and build:

```bash
dotnet restore
dotnet build ReflectorNet.sln
```

Run tests:

```bash
dotnet test ReflectorNet.sln
```

Create a NuGet package locally:

```bash
dotnet pack ReflectorNet/ReflectorNet.csproj -c Release
```

## License

ReflectorNet is licensed under the Apache License 2.0. See [LICENSE](LICENSE) for details.

Copyright (c) Ivan Murzak.
