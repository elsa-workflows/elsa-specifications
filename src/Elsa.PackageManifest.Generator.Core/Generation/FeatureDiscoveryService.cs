using System.Reflection;
using Elsa.PackageManifest.Generator.Core.AssemblyInspection;

namespace Elsa.PackageManifest.Generator.Core.Generation;

public sealed class FeatureDiscoveryService(
    FeatureTypeMatcher featureTypeMatcher,
    FeatureMetadataReader metadataReader,
    SettingDiscoveryService settingDiscoveryService)
{
    public IReadOnlyList<DiscoveredFeature> Discover(Assembly assembly, ProjectPackageMetadata packageMetadata)
    {
        return assembly.GetTypes()
            .Where(featureTypeMatcher.IsFeature)
            .Select(type => CreateFeature(type, packageMetadata))
            .OrderBy(x => x.FeatureId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private DiscoveredFeature CreateFeature(Type type, ProjectPackageMetadata packageMetadata)
    {
        var metadata = metadataReader.ReadFeatureMetadata(type);
        var featureId = $"{packageMetadata.PackageId}.{metadata.FeatureName}";
        var settings = settingDiscoveryService.Discover(type, featureId, metadata.FeatureName);

        return new DiscoveredFeature(
            featureId,
            metadata.FeatureName,
            type.FullName ?? type.Name,
            metadata.DisplayName ?? ToDisplayName(metadata.FeatureName),
            metadata.Description,
            null,
            type.GetInterfaces().Any(x => x.FullName == FeatureTypeMatcher.ShellFeatureInterfaceName && x.DeclaringType is null)
                ? FeatureDiscoverySource.IShellFeature
                : FeatureDiscoverySource.InheritedIShellFeature,
            type.IsPublic,
            type.IsAbstract,
            type.IsGenericTypeDefinition,
            false,
            false,
            metadata.Dependencies,
            [],
            [],
            metadata.Extensions,
            settings);
    }

    private static string ToDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var chars = new List<char>(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]) && !char.IsWhiteSpace(value[i - 1]))
                chars.Add(' ');
            chars.Add(value[i]);
        }

        return new string(chars.ToArray());
    }
}
