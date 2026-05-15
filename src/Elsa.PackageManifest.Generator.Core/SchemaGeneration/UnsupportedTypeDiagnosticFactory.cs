using Elsa.PackageManifest.Generator.Core.Validation;

namespace Elsa.PackageManifest.Generator.Core.SchemaGeneration;

public static class UnsupportedTypeDiagnosticFactory
{
    public static GenerationDiagnostic Create(string featureId, string settingName, string clrType) =>
        new(
            "EPMGEN_SETTING_TYPE_UNSUPPORTED",
            GenerationDiagnosticSeverity.Error,
            $"Setting '{settingName}' on feature '{featureId}' uses unsupported type '{clrType}'.",
            $"{featureId}.{settingName}",
            "$.features[*].settings[*]",
            "setting.type.unsupported");
}
