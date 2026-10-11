# CI/CD

- **PR workflow** (`pull_request.yml`): build + test on ubuntu with .NET 8.0 + 9.0, publishes trx test results
- **Release workflow** (`release.yml`): `workflow_dispatch` only — merging to `main` publishes nothing. Reads the version from `ReflectorNet.csproj` `<Version>`, runs tests + pack, publishes the NuGet package (via `deploy.yml`), verifies it is live, then creates the tag and GitHub Release. Supports `dry_run=true`.

## Versioning

Version is defined in `ReflectorNet/ReflectorNet.csproj` `<Version>` element. A release is a version bump merged to `main` followed by a human dispatch of `release.yml`; do not bump the version or dispatch a release unless asked.
