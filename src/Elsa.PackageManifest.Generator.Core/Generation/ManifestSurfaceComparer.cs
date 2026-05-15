using System.Text.Json;

namespace Elsa.PackageManifest.Generator.Core.Generation;

public sealed class ManifestSurfaceComparer
{
    public string Normalize(string manifestJson)
    {
        using var document = JsonDocument.Parse(manifestJson);
        var features = document.RootElement.TryGetProperty("features", out var featureElement)
            ? featureElement.EnumerateArray()
                .Select(feature => new
                {
                    id = feature.GetProperty("id").GetString(),
                    settings = feature.TryGetProperty("settings", out var settings)
                        ? settings.EnumerateArray().Select(setting => setting.GetProperty("name").GetString()).Order().ToArray()
                        : []
                })
                .OrderBy(x => x.id)
                .ToArray()
            : [];

        return JsonSerializer.Serialize(features);
    }
}
