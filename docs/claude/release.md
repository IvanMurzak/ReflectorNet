# CI/CD

- **PR workflow** (`pull_request.yml`): build + test on ubuntu with .NET 8.0 + 9.0, publishes trx test results
- **Release workflow** (`release.yml`): triggered on push to main — reads version from `ReflectorNet.csproj` `<Version>`, creates GitHub release tag, publishes NuGet package. Version bump = new release.

## Versioning

Version is defined in `ReflectorNet/ReflectorNet.csproj` `<Version>` element. Bumping it and merging to main triggers a NuGet publish.
