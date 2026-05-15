using System.Reflection;

namespace Elsa.PackageManifest.Generator.Core.AssemblyInspection;

public sealed class FeatureMetadataReader
{
    public FeatureMetadata ReadFeatureMetadata(Type type)
    {
        var shellFeature = FeatureTypeMatcher.GetShellFeatureAttribute(type);
        var extensions = ReadExtensions(type);
        return new FeatureMetadata(
            FeatureTypeMatcher.ResolveFeatureName(type),
            FeatureTypeMatcher.ReadNamedString(shellFeature, "DisplayName"),
            FeatureTypeMatcher.ReadNamedString(shellFeature, "Description"),
            FeatureTypeMatcher.ReadDependsOn(shellFeature),
            extensions);
    }

    public SettingHintMetadata ReadSettingMetadata(PropertyInfo property)
    {
        var hint = property.GetCustomAttributesData()
            .FirstOrDefault(x => x.AttributeType.FullName == "Elsa.PackageManifest.Generator.Hints.ManifestSettingAttribute");

        return new SettingHintMetadata(
            FeatureTypeMatcher.ReadNamedString(hint, "DisplayName"),
            FeatureTypeMatcher.ReadNamedString(hint, "Description"),
            FeatureTypeMatcher.ReadNamedString(hint, "Category"),
            FeatureTypeMatcher.ReadNamedString(hint, "Group"),
            FeatureTypeMatcher.ReadNamedBool(hint, "Required"),
            FeatureTypeMatcher.ReadNamedString(hint, "DefaultValue"),
            FeatureTypeMatcher.ReadNamedString(hint, "UiHint"),
            FeatureTypeMatcher.ReadNamedBool(hint, "Secret"),
            FeatureTypeMatcher.ReadNamedBool(hint, "Sensitive"),
            FeatureTypeMatcher.ReadNamedBool(hint, "RestartRequired"),
            FeatureTypeMatcher.ReadNamedBool(hint, "Advanced"),
            FeatureTypeMatcher.ReadNamedBool(hint, "Experimental"),
            ReadExtensions(property));
    }

    private static IReadOnlyDictionary<string, object?> ReadExtensions(MemberInfo member)
    {
        return member.GetCustomAttributesData()
            .Where(x => x.AttributeType.FullName == "Elsa.PackageManifest.Generator.Hints.ManifestExtensionAttribute")
            .Select(x => new
            {
                Key = x.ConstructorArguments.Count > 0 ? x.ConstructorArguments[0].Value as string : null,
                Value = x.ConstructorArguments.Count > 1 ? x.ConstructorArguments[1].Value as string : null
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Key))
            .GroupBy(x => x.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => (object?)x.Last().Value, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed record FeatureMetadata(
    string FeatureName,
    string? DisplayName,
    string? Description,
    IReadOnlyList<string> Dependencies,
    IReadOnlyDictionary<string, object?> Extensions);

public sealed record SettingHintMetadata(
    string? DisplayName,
    string? Description,
    string? Category,
    string? Group,
    bool Required,
    string? DefaultValue,
    string? UiHint,
    bool Secret,
    bool Sensitive,
    bool RestartRequired,
    bool Advanced,
    bool Experimental,
    IReadOnlyDictionary<string, object?> Extensions);
