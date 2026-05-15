namespace Elsa.PackageManifest.Generator.Core.Validation;

public sealed class ValidationSeverityPolicy(string validationSeverity, bool failOnWarnings)
{
    public bool ShouldFail(GenerationDiagnostics diagnostics)
    {
        if (diagnostics.HasErrors)
            return !string.Equals(validationSeverity, "None", StringComparison.OrdinalIgnoreCase);

        return failOnWarnings && diagnostics.Items.Any(x => x.Severity == GenerationDiagnosticSeverity.Warning);
    }
}
