using System.Text.Json;
using Elsa.PackageManifests;

namespace Elsa.PackageManifest.Generator.Core.Overrides;

public sealed class ManifestOverrideReader
{
    public const long MaxOverrideBytes = 262_144;

    public ManifestOverride? Read(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        var file = new FileInfo(path);
        if (file.Length > MaxOverrideBytes)
            throw new InvalidOperationException($"Override file '{path}' exceeds the 256 KB limit.");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ManifestOverride>(json, ManifestJsonSerializerOptions.Default);
    }
}
