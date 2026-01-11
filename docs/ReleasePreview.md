# Publishing a preview package

This repo is set up to publish preview NuGet packages for all `src/*` projects.

## Versioning

Repo-wide versioning is centralized in `Directory.Build.props`.

- Base version: `VersionPrefix` (e.g. `0.1.0`)
- Preview label: `VersionSuffix` (defaults to `preview.1`)

### Override version in CI

You can override the version in your build/publish pipeline:

- Set the full version:
  - `dotnet pack -c Release -p:Version=0.1.0-preview.3`
- Or set only the suffix:
  - `dotnet pack -c Release -p:VersionSuffix=preview.3`

### Convenience: `PreviewSuffix`

`Directory.Build.targets` provides a `PreviewSuffix` property that (when set) overrides `VersionSuffix` **only during packing**:

- `dotnet pack -c Release -p:PreviewSuffix=preview.3`

## Pack

From the repo root:

- `dotnet pack -c Release`

Packages will be emitted under each project:

- `src/CCValidator/bin/Release/*.nupkg`
- `src/CCValidator.DependencyInjection/bin/Release/*.nupkg`
- `src/CCValidator.AspNetCore/bin/Release/*.nupkg`
- `src/CCValidator.Serilog/bin/Release/*.nupkg`

## Verify contents

Each package includes:

- `README.md`
- `COMPATIBILITY.md`

## Publish

Typical flow (example using `dotnet nuget push`):

- `dotnet nuget push <path-to-nupkg> -k <api-key> -s https://api.nuget.org/v3/index.json`

(Use your normal secret handling for the API key.)
