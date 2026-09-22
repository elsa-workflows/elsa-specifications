# Elsa Specifications

Shared package metadata contracts and source generation tooling for Elsa Workflows packages.

This repository contains the extracted package manifest specification and build-time generator. It preserves the existing `elsa-package.json` wire format, schema versions, MSBuild properties, validation rules, and generation behavior.

## Packages

- `Elsa.Specifications.PackageManifests` contains the manifest contract and validation model.
- `Elsa.Specifications.PackageManifest.Generator` generates and packages `elsa-package.json` files for consumer packages.

Add the generator to a package project with `PrivateAssets="all"`:

```xml
<PackageReference Include="Elsa.Specifications.PackageManifest.Generator" Version="0.0.1" PrivateAssets="all" />
```

See the package-specific READMEs for usage and the supported MSBuild properties.

## Publishing

Previews publish as `<next-version>-preview.<run>` from `main`. Bump `Version` in `Directory.Build.props` to the next release right after a stable release, so previews keep sorting above it.

## License

MIT. See [LICENSE](LICENSE).
