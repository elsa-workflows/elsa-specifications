using System.IO.Compression;
using Elsa.PackageManifest.Generator.Testing;
using FluentAssertions;

namespace Elsa.PackageManifest.Generator.IntegrationTests;

public sealed class MultiTargetingPackageInspectionTests
{
    [Fact]
    public void NuGetPackageInspector_finds_exactly_one_root_manifest_entry()
    {
        using var package = CreatePackage(("elsa-package.json", "{}"), ("lib/net10.0/Sample.dll", ""));

        NuGetPackageInspector.AssertSingleEntry(package.PackagePath, "elsa-package.json");
        NuGetPackageInspector.FindEntries(package.PackagePath, "elsa-package.json").Should().ContainSingle();
    }

    [Fact]
    public void NuGetPackageInspector_detects_duplicate_root_manifest_entries()
    {
        using var package = CreatePackage(("elsa-package.json", "{}"), ("elsa-package.json", "{}"));

        var action = () => NuGetPackageInspector.AssertSingleEntry(package.PackagePath, "elsa-package.json");

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NuGetPackageInspector_honors_custom_manifest_package_path()
    {
        using var package = CreatePackage(("metadata/elsa-package.json", "{}"));

        NuGetPackageInspector.AssertSingleEntry(package.PackagePath, "metadata/elsa-package.json");
    }

    private static TempPackage CreatePackage(params (string EntryName, string Content)[] entries)
    {
        var path = Path.Combine(Path.GetTempPath(), "elsa-manifest-generator-tests", $"{Guid.NewGuid():N}.nupkg");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            foreach (var (entryName, content) in entries)
            {
                var entry = archive.CreateEntry(entryName);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }

        return new TempPackage(path);
    }

    private sealed class TempPackage(string packagePath) : IDisposable
    {
        public string PackagePath { get; } = packagePath;

        public void Dispose()
        {
            if (File.Exists(PackagePath))
                File.Delete(PackagePath);
        }
    }
}
