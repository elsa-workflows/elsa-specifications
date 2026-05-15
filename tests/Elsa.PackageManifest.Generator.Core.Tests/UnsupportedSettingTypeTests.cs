using Elsa.PackageManifest.Generator.Core.Generation;
using Elsa.PackageManifest.Generator.Core.Validation;
using Elsa.PackageManifest.Generator.Testing;
using FluentAssertions;

namespace Elsa.PackageManifest.Generator.Core.Tests;

public sealed class UnsupportedSettingTypeTests
{
    [Fact]
    public async Task Generate_does_not_warn_for_ignored_delegate_hooks_by_default()
    {
        var diagnostics = await GenerateAsync(CShellsFeatureFixtures.DelegateHooksFeatureSource);

        diagnostics.Items.Should().NotContain(x => x.Severity == GenerationDiagnosticSeverity.Warning && x.Code == "EPMGEN_SETTING_CODE_HOOK_IGNORED");
        diagnostics.Items.Any(x => x.Code == "EPMGEN_SETTING_TYPE_UNSUPPORTED" && x.Target is not null && x.Target.Contains("Action", StringComparison.OrdinalIgnoreCase)).Should().BeFalse();
    }

    [Fact]
    public async Task Generate_reports_ignored_delegate_hooks_with_verbose_diagnostics()
    {
        var diagnostics = await GenerateAsync(CShellsFeatureFixtures.DelegateHooksFeatureSource, "verbose");

        diagnostics.Items.Should().Contain(x => x.Code == "EPMGEN_SETTING_CODE_HOOK_IGNORED" && x.Severity == GenerationDiagnosticSeverity.Info);
    }

    [Fact]
    public async Task Generate_still_reports_non_delegate_complex_object_settings()
    {
        var diagnostics = await GenerateAsync("""
#nullable enable
using CShells.Features;

namespace Sample.Features;

[ShellFeature("Complex")]
public sealed class ComplexFeature : IShellFeature
{
    public ComplexOptions Options { get; set; } = new();
}

public sealed class ComplexOptions
{
    public string Value { get; set; } = "";
}
""");

        diagnostics.Items.Should().Contain(x => x.Code == "EPMGEN_SETTING_TYPE_UNSUPPORTED");
    }

    private static async Task<GenerationDiagnostics> GenerateAsync(string source, string diagnosticsVerbosity = "concise")
    {
        await using var project = new SampleProjectBuilder().WithSource(source);
        var build = await project.BuildAsync();
        build.ExitCode.Should().Be(0, build.CombinedOutput);

        var diagnostics = new GenerationDiagnostics();
        new ManifestGenerator().Generate(
            new GeneratorOptions(true, Path.Combine(project.ProjectDirectory, "obj", "elsa-package.json"), true, "elsa-package.json", null, "Error", false, false, false, diagnosticsVerbosity, []),
            ProjectPackageMetadataMapper.Map("Sample.Elsa.Package", "1.2.3", "Sample", "Sample package.", null, null, null, null, null, null, "net10.0", null),
            new AssemblyInspectionInput(project.AssemblyPath, project.XmlDocumentationPath, "net10.0", [], true),
            diagnostics);

        return diagnostics;
    }
}
