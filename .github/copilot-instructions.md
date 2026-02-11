# Project Guidelines

## Code Style
- **Language**: C# (LangVersion 11.0), .NET Standard 2.0/2.1, .NET 9.0
- **Formatting**:
  - **Indentation**: 4 spaces.
  - **Braces**: Allman style (braces on new line).
  - **Namespaces**: Reverse domain style (e.g., `com.IvanMurzak.ReflectorNet`).
- **Naming**:
  - Classes/Methods/Properties: `PascalCase`.
  - Private Fields: `camelCase` (e.g., `jsonSchema`).
  - Protected Fields: `_camelCase` (e.g., `_output`).
  - Locals/Params: `camelCase`.
- **Documentation**: Extensive XML documentation (`///`) on public members.
- **Reference**: See `ReflectorNet/src/Reflector/Reflector.cs` for style examples.

## Architecture
- **Pattern**: Chain of Responsibility for object serialization/deserialization.
- **Core Components**:
  - **Reflector** (`ReflectorNet/src/Reflector/`): Main entry point (partial class).
  - **Converters** (`ReflectorNet/src/Converter/`): `IReflectionConverter` implementations (Primitive, Generic, Array).
  - **Models** (`ReflectorNet/src/Model/`): `SerializedMember`, `MethodRef`.
- **Key Files**:
  - `ReflectorNet/src/Reflector/Reflector.cs` (Core logic).
  - `ReflectorNet.Tests/src/BaseTest.cs` (Test base).

## Build and Test
- **Restore**: `dotnet restore`
- **Build**: `dotnet build --configuration Release`
- **Test**: `dotnet test --configuration Release --verbosity normal`
- **Pack**: `dotnet pack ReflectorNet/ReflectorNet.csproj --configuration Release --output ./packages`

## Project Conventions
- **Logging**: Extensive use of `Microsoft.Extensions.Logging.ILogger`.
- **Trace Logs**: Use specific emojis for trace stages (`Consts.Emoji`).
- **Error Handling**: Prefer logging warnings and returning `null` over throwing exceptions for resolution failures.
- **Converters**: Implement `SerializationPriority` to score applicability (0-10000+).
- **Tests**:
  - Inherit from `BaseTest`.
  - Naming: `Method_State_ExpectedBehavior` (e.g., `TryUnstringify_JsonElement_Object_Stringified`).
  - Use `_output` for test logging.

## Integration Points
- **Dependencies**: `Microsoft.Extensions.Logging`.
- **Cross-Assembly**: `ReflectorNet.Tests.OuterAssembly` tests cross-assembly reflection.

## Security
- **Reflection**: Handle dynamic method invocation and type resolution carefully.
- **Validation**: Validate schema and inputs before reflection operations.
