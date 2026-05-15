using Elsa.PackageManifest.Generator.Core.Overrides;

namespace Elsa.PackageManifest.Generator.Core.Generation;

public sealed class ManifestMetadataMerger
{
    public IReadOnlyList<DiscoveredFeature> ApplyOverrides(IReadOnlyList<DiscoveredFeature> features, ManifestOverride? manifestOverride)
    {
        if (manifestOverride is null)
            return features;

        return features.Select(feature =>
        {
            var featureOverride = ManifestOverrideReferenceResolver.ResolveFeatureOverride(feature, manifestOverride);
            if (featureOverride is null)
                return feature;

            var settings = feature.Settings.Select(setting =>
            {
                var settingOverride = featureOverride.Settings?.FirstOrDefault(x => string.Equals(x.Name, setting.Name, StringComparison.OrdinalIgnoreCase));
                return settingOverride is null
                    ? setting
                    : setting with
                    {
                        DisplayName = settingOverride.DisplayName ?? setting.DisplayName,
                        Description = settingOverride.Description ?? setting.Description,
                        Category = settingOverride.Category ?? setting.Category,
                        Group = settingOverride.Group ?? setting.Group,
                        Required = settingOverride.Required ?? setting.Required,
                        Nullable = settingOverride.Nullable ?? setting.Nullable,
                        DefaultValue = settingOverride.DefaultValue ?? setting.DefaultValue,
                        Secret = settingOverride.Secret ?? setting.Secret,
                        Sensitive = settingOverride.Sensitive ?? setting.Sensitive,
                        RestartRequired = settingOverride.RestartRequired ?? setting.RestartRequired,
                        UiHint = settingOverride.UiHint ?? setting.UiHint,
                        Advanced = settingOverride.Advanced ?? setting.Advanced,
                        Experimental = settingOverride.Experimental ?? setting.Experimental,
                        ExtensionMetadata = Merge(setting.ExtensionMetadata, settingOverride.Extensions)
                    };
            }).ToArray();

            return feature with
            {
                DisplayName = featureOverride.DisplayName ?? feature.DisplayName,
                Description = featureOverride.Description ?? feature.Description,
                Category = featureOverride.Category ?? feature.Category,
                Advanced = featureOverride.Advanced ?? feature.Advanced,
                Experimental = featureOverride.Experimental ?? feature.Experimental,
                Dependencies = featureOverride.Dependencies?.Select(ToDependencyReference).ToArray() ?? feature.Dependencies,
                Conflicts = featureOverride.Conflicts?.Select(ToConflictReference).ToArray() ?? feature.Conflicts,
                RequiredCapabilities = featureOverride.RequiredCapabilities ?? feature.RequiredCapabilities,
                ExtensionMetadata = Merge(feature.ExtensionMetadata, featureOverride.Extensions),
                Settings = settings
            };
        }).ToArray();
    }

    private static IReadOnlyDictionary<string, object?> Merge(IReadOnlyDictionary<string, object?> first, IReadOnlyDictionary<string, object?>? second)
    {
        var result = new Dictionary<string, object?>(first, StringComparer.OrdinalIgnoreCase);
        if (second is not null)
        {
            foreach (var item in second)
                result[item.Key] = item.Value;
        }

        return result;
    }

    private static ManifestDependencyReference ToDependencyReference(DependencyOverride dependency) =>
        new(
            string.IsNullOrWhiteSpace(dependency.PackageId) ? null : dependency.PackageId,
            dependency.VersionRange,
            dependency.FeatureId);

    private static ManifestConflictReference ToConflictReference(ConflictOverride conflict) =>
        new(
            string.IsNullOrWhiteSpace(conflict.PackageId) ? null : conflict.PackageId,
            conflict.VersionRange,
            conflict.FeatureId,
            conflict.Reason);
}
