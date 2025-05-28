# ReflectorNet

[![nuget](https://img.shields.io/nuget/v/com.IvanMurzak.ReflectorNet)](https://www.nuget.org/packages/com.IvanMurzak.ReflectorNet/) ![License](https://img.shields.io/github/license/IvanMurzak/ReflectorNet) [![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

AI targeted solution for searching and calling any C# method in runtime using json. It is a similar to Json RPC, just polished for AI usage and provides API to modify existed C# in memory object using json data.

This project is used in [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP).

# Usage API

- `Reflector.Serialize(...)`
- `Reflector.Deserialize(...)`
- `Reflector.GetSerializableFields(...)`
- `Reflector.GetSerializableProperties(...)`
- `Reflector.Populate(...)`
- `Reflector.PopulateAsProperty(...)`

# Override ReflectionConvertor for a custom type

You may need to override convertor in the same way as JsonConvertor works if you have a custom type that should be handled differently.

Here is custom class sample.

```csharp
public class MyClass
{
    public int health = 100;
}
```

Create custom convertor

```csharp
public class MyReflectionConvertor : GenericReflectionConvertor<MyClass>
{

}
```

Register the convertor

```csharp
Reflector.Registry.Add(new MyReflectionConvertor());
```
