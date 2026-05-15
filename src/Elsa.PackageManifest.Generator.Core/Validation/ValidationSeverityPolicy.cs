namespace Elsa.PackageManifest.Generator.Core.Validation;

public sealed class ValidationSeverityPolicy(string validationSeverity, bool failOnWarnings)
{
    public bool ShouldFail(GenerationDiagnostics diagnostics)
    {
        if (diagnostics.HasErrors)
            return !string.Equals(validationSeverity, "None", StringComparison.OrdinalIgnoreCase);

        return failOnWarnings && diagnostics.Items.Any(x => x.Severity == GenerationDiagnosticSeverity.Warning);
    }

    public GenerationDiagnostic MapLoggedSeverity(GenerationDiagnostic diagnostic)
    {
        if (diagnostic.Severity != GenerationDiagnosticSeverity.Error)
            return diagnostic;

        if (string.Equals(validationSeverity, "None", StringComparison.OrdinalIgnoreCase))
            return diagnostic with { Severity = GenerationDiagnosticSeverity.Info };

        if (string.Equals(validationSeverity, "Warning", StringComparison.OrdinalIgnoreCase))
            return diagnostic with { Severity = GenerationDiagnosticSeverity.Warning };

        return diagnostic;
    }
}
