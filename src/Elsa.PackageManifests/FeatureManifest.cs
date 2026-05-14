using Elsa.PackageManifests.Compatibility;

namespace Elsa.PackageManifests;

public sealed class FeatureManifest : ExtensibleManifestObject
{
    public string Id { get; init; } = "";
    public string TypeName { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? Description { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<FeatureSettingManifest> Settings { get; init; } = [];
    public IReadOnlyList<DependencyManifest> Dependencies { get; init; } = [];
    public IReadOnlyList<ConflictManifest> Conflicts { get; init; } = [];
    public IReadOnlyList<string> RequiredCapabilities { get; init; } = [];
    public bool Advanced { get; init; }
    public bool Experimental { get; init; }
    public Dictionary<string, object?> Extensions { get; init; } = new();
}
