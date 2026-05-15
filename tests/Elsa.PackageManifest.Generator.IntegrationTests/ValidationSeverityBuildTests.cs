using Elsa.PackageManifest.Generator.Core.Generation;
using Elsa.PackageManifest.Generator.Core.Validation;
using Elsa.PackageManifest.Generator.Testing;
using FluentAssertions;

namespace Elsa.PackageManifest.Generator.IntegrationTests;

public sealed class ValidationSeverityBuildTests
{
    [Fact]
    public async Task Default_policy_fails_for_non_delegate_unsupported_setting()
    {
        await using var project = new SampleProjectBuilder().WithSource("""
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
        var build = await project.BuildAsync();
        build.ExitCode.Should().Be(0, build.CombinedOutput);

        var diagnostics = new GenerationDiagnostics();
        new ManifestGenerator().Generate(
            new GeneratorOptions(true, Path.Combine(project.ProjectDirectory, "obj", "elsa-package.json"), true, "elsa-package.json", null, "Error", false, false, false, "concise", []),
            ProjectPackageMetadataMapper.Map("Sample.Elsa.Package", "1.2.3", "Sample", "Sample package.", null, null, null, null, null, null, "net10.0", null),
            new AssemblyInspectionInput(project.AssemblyPath, project.XmlDocumentationPath, "net10.0", [], true),
            diagnostics);

        diagnostics.Items.Should().Contain(x => x.Code == "EPMGEN_SETTING_TYPE_UNSUPPORTED");
        new ValidationSeverityPolicy("Error", false).ShouldFail(diagnostics).Should().BeTrue();
    }
}
