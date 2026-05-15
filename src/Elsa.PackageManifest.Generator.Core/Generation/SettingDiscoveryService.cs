using System.Reflection;
using Elsa.PackageManifest.Generator.Core.AssemblyInspection;
using Elsa.PackageManifest.Generator.Core.SchemaGeneration;
using Elsa.PackageManifest.Generator.Core.Validation;

namespace Elsa.PackageManifest.Generator.Core.Generation;

public sealed class SettingDiscoveryService(
    FeatureMetadataReader metadataReader,
    NullableMetadataReader nullableMetadataReader,
    ValidationAnnotationMapper validationAnnotationMapper,
    SettingDefaultValueResolver defaultValueResolver,
    SettingSchemaGenerator schemaGenerator,
    GenerationDiagnostics? diagnostics = null,
    bool verboseDiagnostics = false)
{
    public IReadOnlyList<DiscoveredSetting> Discover(Type featureType, string featureId, string featureName)
    {
        var settings = new List<DiscoveredSetting>();
        foreach (var property in featureType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(IsConfigurableProperty))
        {
            if (IsIgnoredCodeHook(property, featureId))
                continue;

            var setting = TryCreateSetting(property, featureId, featureName);
            if (setting is not null)
                settings.Add(setting);
        }

        return settings
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsConfigurableProperty(PropertyInfo property) =>
        property.GetIndexParameters().Length == 0 &&
        property.GetSetMethod() is { IsPublic: true } &&
        !FeatureTypeMatcher.HasIgnoreAttribute(property);

    private bool IsIgnoredCodeHook(PropertyInfo property, string featureId)
    {
        if (!TypeMetadataHelpers.IsDelegateOrContainsDelegate(property.PropertyType))
            return false;

        if (verboseDiagnostics)
        {
            diagnostics?.Verbose(
                "EPMGEN_SETTING_CODE_HOOK_IGNORED",
                $"Setting candidate '{featureId}.{property.Name}' was ignored because it is a code configuration hook.",
                property.PropertyType.FullName ?? property.PropertyType.Name);
        }

        return true;
    }

    private DiscoveredSetting? TryCreateSetting(PropertyInfo property, string featureId, string featureName)
    {
        var hint = metadataReader.ReadSettingMetadata(property);
        var nullable = nullableMetadataReader.IsNullable(property);
        var validation = validationAnnotationMapper.Map(property);
        var schema = schemaGenerator.Generate(property.PropertyType, nullable, validation);
        if (IsUnsupportedSchema(schema))
        {
            ReportUnsupportedSetting(property, featureId);
            return null;
        }

        var defaultValue = defaultValueResolver.Resolve(property, hint.DefaultValue);
        var required = hint.Required || validation.ContainsKey("required") || (!nullable && defaultValue is null);
        var enumValues = property.PropertyType.IsEnum
            ? Enum.GetNames(property.PropertyType).Order(StringComparer.Ordinal).ToArray()
            : [];
        var displayName = hint.DisplayName ?? NamingHelpers.ToDisplayName(property.Name);

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

    private void ReportUnsupportedSetting(PropertyInfo property, string featureId)
    {
        var clrType = property.PropertyType.FullName ?? property.PropertyType.Name;
        diagnostics?.Add(UnsupportedTypeDiagnosticFactory.Create(featureId, property.Name, clrType));
    }

    private static bool IsUnsupportedSchema(SettingSchemaResult schema) =>
        string.Equals(schema.JsonType, "unsupported", StringComparison.OrdinalIgnoreCase);
}
