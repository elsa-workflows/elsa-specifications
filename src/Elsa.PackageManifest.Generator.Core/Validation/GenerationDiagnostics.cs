namespace Elsa.PackageManifest.Generator.Core.Validation;

public enum GenerationDiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record GenerationDiagnostic(
    string Code,
    GenerationDiagnosticSeverity Severity,
    string Message,
    string? Target = null,
    string? ManifestPath = null,
    string? RuleId = null);

public sealed class GenerationDiagnostics
{
    private readonly List<GenerationDiagnostic> _items = [];

    public IReadOnlyList<GenerationDiagnostic> Items => _items;
    public bool HasErrors => _items.Any(x => x.Severity == GenerationDiagnosticSeverity.Error);

    public void Info(string code, string message, string? target = null, string? manifestPath = null, string? ruleId = null) =>
        Add(new GenerationDiagnostic(code, GenerationDiagnosticSeverity.Info, message, target, manifestPath, ruleId));

    public void Warning(string code, string message, string? target = null, string? manifestPath = null, string? ruleId = null) =>
        Add(new GenerationDiagnostic(code, GenerationDiagnosticSeverity.Warning, message, target, manifestPath, ruleId));

    public void Error(string code, string message, string? target = null, string? manifestPath = null, string? ruleId = null) =>
        Add(new GenerationDiagnostic(code, GenerationDiagnosticSeverity.Error, message, target, manifestPath, ruleId));

    public void Add(GenerationDiagnostic diagnostic) => _items.Add(diagnostic);
}
