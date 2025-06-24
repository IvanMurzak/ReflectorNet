# How to deploy to NuGet

## Build

```bash
cd ReflectorNet
dotnet build -c Release
dotnet pack -c Release -o ..\packages
```

## Deploy

```bash
dotnet nuget push ..\packages\ReflectorNet.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

Or upload using [this page](https://www.nuget.org/packages/manage/upload).
