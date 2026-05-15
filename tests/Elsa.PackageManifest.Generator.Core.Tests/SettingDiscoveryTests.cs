using System.Text.Json;
using Elsa.PackageManifest.Generator.Core.Generation;
using Elsa.PackageManifest.Generator.Core.Validation;
using Elsa.PackageManifest.Generator.Testing;
using FluentAssertions;

namespace Elsa.PackageManifest.Generator.Core.Tests;

public sealed class SettingDiscoveryTests
{
    [Fact]
    public async Task Generate_excludes_direct_action_and_service_factory_hooks()
    {
        var result = await GenerateAsync(CShellsFeatureFixtures.DelegateHooksFeatureSource);

        result.Settings.Should().Contain("Endpoint");
        result.Settings.Should().NotContain("Configure");
        result.Settings.Should().NotContain("ServiceFactory");
    }

    [Fact]
    public async Task Generate_excludes_http_client_configuration_hooks()
    {
        var result = await GenerateAsync(CShellsFeatureFixtures.DelegateHooksFeatureSource);

        result.Settings.Should().NotContain("ConfigureHttpClient");
    }

    [Fact]
    public async Task Generate_excludes_delegate_valued_dictionary_and_list_hooks()
    {
        var result = await GenerateAsync("""
#nullable enable
using System;
using System.Collections.Generic;
using CShells.Features;

namespace Sample.Features;

[ShellFeature("DelegateCollections")]
public sealed class DelegateCollectionsFeature : IShellFeature
{
    public string Name { get; set; } = "";
    public List<Func<IServiceProvider, object>> Factories { get; set; } = [];
    public Dictionary<string, Func<IServiceProvider, object>> FactoryMap { get; set; } = [];
}
""");

        result.Settings.Should().Equal("Name");
    }

    private static async Task<GeneratedSettingsResult> GenerateAsync(string source, string diagnosticsVerbosity = "concise")
    {
        await using var project = new SampleProjectBuilder().WithSource(source);
        var build = await project.BuildAsync();
        build.ExitCode.Should().Be(0, build.CombinedOutput);

        var diagnostics = new GenerationDiagnostics();
        var artifact = new ManifestGenerator().Generate(
            new GeneratorOptions(true, Path.Combine(project.ProjectDirectory, "obj", "elsa-package.json"), true, "elsa-package.json", null, "Error", false, false, false, diagnosticsVerbosity, []),
            ProjectPackageMetadataMapper.Map("Sample.Elsa.Package", "1.2.3", "Sample", "Sample package.", null, null, null, null, null, null, "net10.0", null),
            new AssemblyInspectionInput(project.AssemblyPath, project.XmlDocumentationPath, "net10.0", [], true),
            diagnostics);

        using var document = JsonDocument.Parse(artifact.ManifestJson);
        var settings = document.RootElement.GetProperty("features")[0]
            .GetProperty("settings")
            .EnumerateArray()
            .Select(x => x.GetProperty("name").GetString()!)
            .ToArray();

        return new GeneratedSettingsResult(settings, diagnostics);
    }

    private sealed record GeneratedSettingsResult(IReadOnlyList<string> Settings, GenerationDiagnostics Diagnostics);
}
