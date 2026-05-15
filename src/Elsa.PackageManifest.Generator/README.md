# Elsa.PackageManifest.Generator

`Elsa.PackageManifest.Generator` is a build-time package for Elsa professional runtime packages. Add it to a class library with `PrivateAssets="all"` and it generates an `elsa-package.json` manifest during build/pack.

```xml
<PackageReference Include="Elsa.PackageManifest.Generator" Version="x.y.z" PrivateAssets="all" />
```

The generator inspects the compiled assembly with metadata-only reflection. It discovers public CShells feature classes implementing `CShells.Features.IShellFeature`, reads `ShellFeatureAttribute` metadata, discovers public settable feature properties as settings, applies XML documentation and optional manifest hint attributes, validates the result with `Elsa.PackageManifests`, and includes one root `elsa-package.json` in the produced NuGet package.

Optional source-only hints are available under `Elsa.PackageManifest.Generator.Hints`:

- `ManifestSettingAttribute`
- `ManifestIgnoreAttribute`
- `ManifestExtensionAttribute`

For metadata that cannot be inferred, add `elsa-package.overrides.json` beside the project file or set `ElsaPackageManifestOverrideFile`.

Common MSBuild properties:

- `GenerateElsaPackageManifest`
- `ElsaPackageManifestOutputPath`
- `ElsaPackageManifestIncludeInPackage`
- `ElsaPackageManifestValidationSeverity`
- `ElsaPackageManifestStrict`
- `ElsaPackageManifestFailOnWarnings`
- `ElsaPackageManifestAllowTargetFrameworkDifferences`
