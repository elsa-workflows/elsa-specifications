using System.Reflection;
using Elsa.PackageManifest.Generator.Core.AssemblyInspection;
using Elsa.PackageManifest.Generator.Core.SchemaGeneration;

namespace Elsa.PackageManifest.Generator.Core.Generation;

public sealed class SettingDiscoveryService(
    FeatureMetadataReader metadataReader,
    NullableMetadataReader nullableMetadataReader,
    ValidationAnnotationMapper validationAnnotationMapper,
    SettingDefaultValueResolver defaultValueResolver,
    SettingSchemaGenerator schemaGenerator)
{
    public IReadOnlyList<DiscoveredSetting> Discover(Type featureType, string featureId, string featureName)
    {
        return featureType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(IsConfigurableProperty)
            .Select(property => CreateSetting(property, featureId, featureName))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsConfigurableProperty(PropertyInfo property) =>
        property.GetIndexParameters().Length == 0 &&
        property.GetSetMethod() is { IsPublic: true } &&
        !FeatureTypeMatcher.HasIgnoreAttribute(property);

    private DiscoveredSetting CreateSetting(PropertyInfo property, string featureId, string featureName)
    {
        var hint = metadataReader.ReadSettingMetadata(property);
        var nullable = nullableMetadataReader.IsNullable(property);
        var validation = validationAnnotationMapper.Map(property);
        var required = hint.Required || validation.ContainsKey("required") || (!nullable && defaultValueResolver.Resolve(property, hint.DefaultValue) is null);
        var defaultValue = defaultValueResolver.Resolve(property, hint.DefaultValue);
        var schema = schemaGenerator.Generate(property.PropertyType, nullable, validation);
        var enumValues = property.PropertyType.IsEnum
            ? Enum.GetNames(property.PropertyType).Order(StringComparer.Ordinal).ToArray()
            : [];
        var displayName = hint.DisplayName ?? ToDisplayName(property.Name);

        return new DiscoveredSetting(
            featureId,
            property.Name,
            property.Name,
            property.PropertyType.FullName ?? property.PropertyType.Name,
            schema.JsonType,
            $"{featureName}:{property.Name}",
            required,
            nullable,
            defaultValue,
            displayName,
            hint.Description,
            hint.Category,
            hint.Group,
            validation,
            enumValues,
            hint.Secret,
            hint.Sensitive,
            hint.RestartRequired,
            hint.UiHint,
            hint.Advanced,
            hint.Experimental,
            hint.Extensions);
    }

    private static string ToDisplayName(string value)
    {
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
