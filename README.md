![ReflectorNet: .NET reflection toolkit. Live objects to JSON and back, JSON Schema and method calls for AI agents](https://raw.githubusercontent.com/IvanMurzak/ReflectorNet/main/docs/assets/hero.svg)

[![NuGet](https://img.shields.io/nuget/v/com.IvanMurzak.ReflectorNet?label=NuGet&color=2F6F9F&logo=nuget&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://www.nuget.org/packages/com.IvanMurzak.ReflectorNet/) [![NuGet downloads](https://img.shields.io/nuget/dt/com.IvanMurzak.ReflectorNet?label=Downloads&color=2F6F9F&logo=nuget&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://www.nuget.org/packages/com.IvanMurzak.ReflectorNet/) ![.NET: netstandard2.1, net8.0, net9.0](https://img.shields.io/badge/netstandard2.1%20%7C%208.0%20%7C%209.0-0E7490?logo=dotnet&style=for-the-badge&labelColor=0F172A&logoColor=white)
[![Release workflow](https://img.shields.io/github/actions/workflow/status/IvanMurzak/ReflectorNet/release.yml?label=Release&color=15803D&style=for-the-badge&logo=github&logoColor=white&labelColor=0F172A)](https://github.com/IvanMurzak/ReflectorNet/actions/workflows/release.yml) [![License: Apache 2.0](https://img.shields.io/badge/License-Apache--2.0-047857?style=for-the-badge&labelColor=0F172A&logoColor=white)](https://github.com/IvanMurzak/ReflectorNet/blob/main/LICENSE) [![Discord](https://img.shields.io/badge/Discord-Join-5865F2?logo=discord&style=for-the-badge&labelColor=0F172A&logoColor=white)](https://discord.gg/Cgs6nM8BPU) [![Stand with Ukraine](https://raw.githubusercontent.com/IvanMurzak/IvanMurzak/main/images/ukraine/flag-badge.svg)](https://stand-with-ukraine.pp.ua)

**ReflectorNet** is a .NET reflection toolkit that lets an AI agent, or any tool without compiled
bindings, work with a running program through JSON. It serializes live objects with their type
information and turns them back, changes existing objects in place, generates JSON Schema for types
and method signatures, and finds and calls methods described by name with JSON arguments.

The entry point is the `Reflector` class. Its data model is `SerializedMember`: a JSON-friendly
value that keeps the member name, the type name, nested fields and properties, and the raw value.

![A live .NET object is serialized to SerializedMember JSON and changed back in place; types and methods produce JSON Schema; an AI agent reads the JSON and the schema and sends patches and JSON method calls that ReflectorNet finds and invokes](https://raw.githubusercontent.com/IvanMurzak/ReflectorNet/main/docs/assets/flow.svg)

## Features

- ✅ **Serialize / Deserialize** — live objects to `SerializedMember` JSON and back
- ✅ **In-place changes** — `TryModify`, `TryModifyAt` and `TryPatch` keep object identity
- ✅ **Inspection** — `TryReadAt` one path, `View` a filtered tree, `Grep` by member name
- ✅ **JSON Schema** — for types, method arguments and return values
- ✅ **Method calls** — find methods by fuzzy name and call them with JSON arguments
- ✅ **Extensible** — custom reflection and JSON converters, type blacklist

## Install

```bash
dotnet add package com.IvanMurzak.ReflectorNet
```

Targets `netstandard2.1`, `net8.0` and `net9.0`. Dependencies: `System.Text.Json` and
`Microsoft.Extensions.Logging`.

## Quick start

```csharp
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Model;

var reflector = new Reflector();
var player = new Player { Name = "Ada", Level = 7 };

// Object -> JSON
SerializedMember data = reflector.Serialize(player, name: "player");
string json = reflector.JsonSerializer.Serialize(data);

// JSON -> object
var parsed = reflector.JsonSerializer.Deserialize<SerializedMember>(json)!;
Player? copy = reflector.Deserialize<Player>(parsed);

// Change the live object in place
object? target = player;
var logs = new Logs();
reflector.TryModifyAt<int>(ref target, "Level", 8, logs: logs);
reflector.TryPatch(ref target, """{ "Name": "Grace", "Level": 9 }""", logs: logs);

public class Player
{
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}
```

The JSON of `data` looks like this:

```json
{
  "name": "player",
  "typeName": "Player",
  "value": {},
  "props": [
    { "name": "Name", "typeName": "System.String", "value": "Ada" },
    { "name": "Level", "typeName": "System.Int32", "value": 7 }
  ]
}
```

## Call a method from JSON

Give the agent a schema of the arguments, then call the method with the JSON it sends back.

```csharp
using System.ComponentModel;
using System.Reflection;

MethodInfo buy = typeof(Shop).GetMethod(nameof(Shop.Buy))!;
var argsSchema = reflector.GetArgumentsSchema(buy);
// {"type":"object","properties":{"itemId":{"type":"string"},"count":{"type":"integer"}},
//  "required":["itemId","count"]}

var callArgs = reflector.JsonSerializer.Deserialize<SerializedMemberList>("""
[
  { "name": "itemId", "typeName": "System.String", "value": "potion" },
  { "name": "count",  "typeName": "System.Int32",  "value": 2 }
]
""")!;

string result = reflector.MethodCall(
    reflector,
    new MethodRef { TypeName = "Shop", MethodName = "Buy" },
    inputParameters: callArgs,
    executeInMainThread: false);
// [Success] Execution result: ... "Bought 2 x potion"

public static class Shop
{
    [Description("Buy items and return a receipt.")]
    public static string Buy(string itemId, int count) => $"Bought {count} x {itemId}";
}
```

Type and method names may be partial; match levels and instance methods (`targetObject`) are in the
[usage guide](https://github.com/IvanMurzak/ReflectorNet/blob/main/docs/usage.md).

## Inspect and search

```csharp
reflector.TryReadAt(player, "Level", out SerializedMember? level);   // one value
var tree = reflector.View(player, new ViewQuery { MaxDepth = 1 });   // filtered tree
var hits = reflector.Grep(player, "^Lev");                           // ViewMatch: Path + Value
```

Paths are slash-delimited (`Items/[0]/Count`, `Stats/[hp]`); see [Paths](https://github.com/IvanMurzak/ReflectorNet/blob/main/docs/usage.md#paths).

## Documentation

- [Usage guide](https://github.com/IvanMurzak/ReflectorNet/blob/main/docs/usage.md): serialization, paths, `View` and `Grep`, `TryModifyAt` and `TryPatch`,
  method discovery and calls, JSON Schema, converters and the type blacklist.
- [Architecture](https://github.com/IvanMurzak/ReflectorNet/blob/main/docs/agents/architecture.md), [models](https://github.com/IvanMurzak/ReflectorNet/blob/main/docs/agents/models.md),
  [thread safety](https://github.com/IvanMurzak/ReflectorNet/blob/main/docs/agents/threading.md) and [release process](https://github.com/IvanMurzak/ReflectorNet/blob/main/docs/agents/release.md).
- [Changelog](https://github.com/IvanMurzak/ReflectorNet/releases): GitHub Releases.

ReflectorNet is the reflection layer of [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) and the
AI Game Developer tools. Questions and ideas: [Discord](https://discord.gg/Cgs6nM8BPU).

## Build from source

```bash
dotnet build --configuration Release
dotnet test --configuration Release      # tests run on net8.0 and net9.0
dotnet pack ReflectorNet/ReflectorNet.csproj --configuration Release --output ./packages
```

## License

[Apache License 2.0](https://github.com/IvanMurzak/ReflectorNet/blob/main/LICENSE). Copyright (c) Ivan Murzak.
