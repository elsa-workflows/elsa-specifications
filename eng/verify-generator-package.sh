#!/usr/bin/env bash
# Verifies that the packed Elsa.Specifications.PackageManifest.Generator .nupkg in the given
# folder contains its MSBuild task assembly under tasks/, and that a consumer project restoring
# it from that folder as a local feed can produce an elsa-package.json manifest.
#
# Usage: eng/verify-generator-package.sh <packages-dir>
set -euo pipefail

packages_dir=$(cd "${1:?usage: $0 <packages-dir>}" && pwd)
nupkg=$(find "$packages_dir" -maxdepth 1 -iname 'Elsa.Specifications.PackageManifest.Generator.*.nupkg' | sort | tail -n1)
[ -n "$nupkg" ] || { echo "No Elsa.Specifications.PackageManifest.Generator package found in $packages_dir" >&2; exit 1; }

# Captured into a variable rather than piped into `grep -q` directly: under `pipefail`, `grep -q`
# can close its end of the pipe as soon as it finds a match, which sends unzip a SIGPIPE and turns
# that into a spurious pipeline failure.
listing=$(unzip -l "$nupkg")
if ! grep -q 'tasks/Elsa.Specifications.PackageManifest.Generator.MSBuild.dll' <<< "$listing"; then
  echo "$(basename "$nupkg") is missing tasks/Elsa.Specifications.PackageManifest.Generator.MSBuild.dll" >&2
  exit 1
fi
echo "OK: $(basename "$nupkg") contains its MSBuild task assembly."

# Consuming-project smoke test: restore the package from a local folder feed and build a tiny
# project with one feature class, then assert elsa-package.json was produced.
version=$(unzip -p "$nupkg" '*.nuspec' | grep -o '<version>[^<]*</version>' | sed -E 's/<\/?version>//g')
work_dir=$(mktemp -d)
trap 'rm -rf "$work_dir"' EXIT

cat > "$work_dir/nuget.config" <<CONFIG
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$packages_dir" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
CONFIG

cat > "$work_dir/Consumer.csproj" <<CSPROJ
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>Elsa.Specifications.Verify.Consumer</PackageId>
    <Version>1.0.0</Version>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Elsa.Specifications.PackageManifest.Generator" Version="$version" PrivateAssets="all" />
  </ItemGroup>
</Project>
CSPROJ

cat > "$work_dir/Feature.cs" <<'CS'
namespace CShells.Features
{
    public sealed class ShellFeatureAttribute(string? name = null) : System.Attribute
    {
        public string? DisplayName { get; set; }
    }

    public interface IShellFeature;
}

[CShells.Features.ShellFeature("Smoke", DisplayName = "Smoke Feature")]
public sealed class SmokeFeature : CShells.Features.IShellFeature;
CS

if ! dotnet build "$work_dir/Consumer.csproj" -c Release --configfile "$work_dir/nuget.config" > "$work_dir/build.log" 2>&1; then
  cat "$work_dir/build.log" >&2
  echo "Consumer smoke build failed." >&2
  exit 1
fi

manifest="$work_dir/obj/Release/net10.0/elsa-package.json"
[ -f "$manifest" ] || { echo "Consumer smoke build did not produce $manifest" >&2; exit 1; }
echo "OK: consumer smoke build produced elsa-package.json."
