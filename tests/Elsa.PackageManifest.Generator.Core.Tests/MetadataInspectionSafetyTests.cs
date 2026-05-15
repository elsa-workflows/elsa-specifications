using System.Text.Json;
using Elsa.PackageManifest.Generator.Core.Generation;
using Elsa.PackageManifest.Generator.Core.Validation;
using Elsa.PackageManifest.Generator.Testing;
using FluentAssertions;

namespace Elsa.PackageManifest.Generator.Core.Tests;

public sealed class MetadataInspectionSafetyTests
{
    [Fact]
    public async Task Generate_does_not_invoke_feature_constructors_or_property_getters()
    {
        await using var project = new SampleProjectBuilder().WithSource(TripwireFeatureFixtures.Source);
        var build = await project.BuildAsync();
        build.ExitCode.Should().Be(0, build.CombinedOutput);

        var diagnostics = new GenerationDiagnostics();
        var artifact = new ManifestGenerator().Generate(
            new GeneratorOptions(true, Path.Combine(project.ProjectDirectory, "obj", "elsa-package.json"), true, "elsa-package.json", null, "Error", false, false, false, "concise", []),
            ProjectPackageMetadataMapper.Map("Sample.Elsa.Package", "1.2.3", "Sample", "Sample package.", null, null, null, null, null, null, "net10.0", null),
            new AssemblyInspectionInput(project.AssemblyPath, project.XmlDocumentationPath, "net10.0", [], true),
            diagnostics);

        diagnostics.Items.Where(x => x.Severity == GenerationDiagnosticSeverity.Error).Should().BeEmpty();
        using var document = JsonDocument.Parse(artifact.ManifestJson);
        var settings = document.RootElement.GetProperty("features")[0].GetProperty("settings").EnumerateArray().Select(x => x.GetProperty("name").GetString());
        settings.Should().Equal("SafeSetting");
    }
}
