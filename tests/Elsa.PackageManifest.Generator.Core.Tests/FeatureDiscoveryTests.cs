using System.Globalization;
using System.Text.Json;
using Elsa.PackageManifest.Generator.Core.Generation;
using Elsa.PackageManifest.Generator.Core.Validation;
using Elsa.PackageManifest.Generator.Testing;
using FluentAssertions;

namespace Elsa.PackageManifest.Generator.Core.Tests;

public sealed class FeatureDiscoveryTests
{
    [Fact]
    public async Task Generate_discovers_cshells_feature_and_configurable_settings_without_runtime_execution()
    {
        await using var project = new SampleProjectBuilder()
            .WithSource("""
#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CShells.Features;
using Elsa.PackageManifest.Generator.Hints;

namespace Sample.Features;

/// <summary>Adds Entity Framework Core persistence support.</summary>
[ShellFeature("EntityFrameworkCore", DisplayName = "Entity Framework Core Persistence", Description = "Adds EF Core persistence.")]
public sealed class EntityFrameworkCoreFeature : IShellFeature
{
    /// <summary>The database provider name.</summary>
    [ManifestSetting(DisplayName = "Provider", Category = "Persistence", DefaultValue = "Sqlite")]
    public string? Provider { get; set; }

    [Range(1, 100)]
    public int BatchSize { get; set; }

    public string RequiredName { get; set; } = "";

    [StringLength(100, MinimumLength = 5)]
    public string? Code { get; set; }

    public List<string> SupportedItems { get; set; } = [];

    public Dictionary<string, int> SupportedMap { get; set; } = [];

    [ManifestSetting(DefaultValue = "3.14")]
    public decimal Ratio { get; set; }

    [ManifestSetting(DefaultValue = "42")]
    public int? OptionalBatchSize { get; set; }

    [ManifestIgnore]
    public string Ignored { get; set; } = "";

    public static string StaticSetting { get; set; } = "";

    public string ReadOnlySetting => "computed";
}
""");

        var build = await project.BuildAsync();
        build.ExitCode.Should().Be(0, build.CombinedOutput);

        var result = Generate(project);
        result.diagnostics.Items.Where(x => x.Severity == GenerationDiagnosticSeverity.Error).Should().BeEmpty();

        using var document = JsonDocument.Parse(result.artifact.ManifestJson);
        var feature = document.RootElement.GetProperty("features")[0];
        feature.GetProperty("id").GetString().Should().Be("Sample.Elsa.Package.EntityFrameworkCore");
        feature.GetProperty("displayName").GetString().Should().Be("Entity Framework Core Persistence");
        feature.GetProperty("description").GetString().Should().Be("Adds EF Core persistence.");

        var settings = feature.GetProperty("settings").EnumerateArray().ToDictionary(x => x.GetProperty("name").GetString()!);
        settings.Keys.Should().BeEquivalentTo("BatchSize", "Code", "OptionalBatchSize", "Provider", "Ratio", "RequiredName", "SupportedItems", "SupportedMap");
        settings["Provider"].GetProperty("jsonType").GetString().Should().Be("string");
        settings["Provider"].GetProperty("defaultValue").GetString().Should().Be("Sqlite");
        settings["BatchSize"].GetProperty("validation").GetProperty("minimum").GetDecimal().Should().Be(1);
        settings["BatchSize"].GetProperty("validation").GetProperty("maximum").GetDecimal().Should().Be(100);
        settings["Code"].GetProperty("validation").GetProperty("minLength").GetInt32().Should().Be(5);
        settings["Code"].GetProperty("validation").GetProperty("maxLength").GetInt32().Should().Be(100);
        settings["RequiredName"].GetProperty("required").GetBoolean().Should().BeTrue();
        settings["SupportedItems"].GetProperty("jsonType").GetString().Should().Be("array");
        settings["SupportedMap"].GetProperty("jsonType").GetString().Should().Be("object");
        settings["Ratio"].GetProperty("defaultValue").GetDecimal().Should().Be(3.14m);
        settings["OptionalBatchSize"].GetProperty("jsonType").GetString().Should().Be("integer");
        settings["OptionalBatchSize"].GetProperty("extensions").GetProperty("nullable").GetBoolean().Should().BeTrue();
        settings["OptionalBatchSize"].GetProperty("defaultValue").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task Generate_omits_unsupported_complex_setting_types()
    {
        await using var project = new SampleProjectBuilder()
            .WithSource("""
#nullable enable
using CShells.Features;

namespace Sample.Features;

[ShellFeature("Complex", DisplayName = "Complex Feature", Description = "Complex feature.")]
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

        var result = Generate(project);

        result.diagnostics.Items.Should().Contain(x => x.Code == "EPMGEN_SETTING_TYPE_UNSUPPORTED" && x.Severity == GenerationDiagnosticSeverity.Info);
        result.diagnostics.Items.Should().NotContain(x => x.Severity == GenerationDiagnosticSeverity.Warning || x.Severity == GenerationDiagnosticSeverity.Error);

        using var document = JsonDocument.Parse(result.artifact.ManifestJson);
        var settings = document.RootElement.GetProperty("features")[0].GetProperty("settings").EnumerateArray();
        settings.Should().BeEmpty();
    }

    [Fact]
    public async Task Generate_applies_infrastructure_requirements_from_override_file()
    {
        await using var project = new SampleProjectBuilder()
            .WithSource("""
using CShells.Features;

namespace Sample.Features;

[ShellFeature("RabbitMq", DisplayName = "RabbitMQ Messaging")]
public sealed class RabbitMqFeature : IShellFeature
{
    public string ConnectionString { get; set; } = "";
}
""");

        var build = await project.BuildAsync();
        build.ExitCode.Should().Be(0, build.CombinedOutput);
        var overridePath = Path.Combine(project.ProjectDirectory, "elsa-package.overrides.json");
        await File.WriteAllTextAsync(overridePath, """
{
  "features": [
    {
      "id": "Sample.Elsa.Package.RabbitMq",
      "infrastructure": [
        {
          "id": "message-broker",
          "kind": "message-broker",
          "reason": "RabbitMQ transport requires a broker.",
          "providers": ["rabbitmq", "azure-service-bus"],
          "configurationKeys": ["RabbitMq:ConnectionString"]
        }
      ]
    }
  ]
}
""");

        var result = Generate(project, overridePath);
        result.diagnostics.Items.Where(x => x.Severity == GenerationDiagnosticSeverity.Error).Should().BeEmpty();

        using var document = JsonDocument.Parse(result.artifact.ManifestJson);
        var requirement = document.RootElement
            .GetProperty("features")[0]
            .GetProperty("infrastructure")[0];

        requirement.GetProperty("id").GetString().Should().Be("message-broker");
        requirement.GetProperty("kind").GetString().Should().Be("message-broker");
        requirement.GetProperty("providers").EnumerateArray().Select(x => x.GetString()).Should().BeEquivalentTo("rabbitmq", "azure-service-bus");
        requirement.GetProperty("configurationKeys").EnumerateArray().Select(x => x.GetString()).Should().Contain("RabbitMq:ConnectionString");
    }

    private static (GeneratedManifestArtifact artifact, GenerationDiagnostics diagnostics) Generate(SampleProjectBuilder project, string? overridePath = null)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");
        var diagnostics = new GenerationDiagnostics();
        var generator = new ManifestGenerator();
        try
        {
            var artifact = generator.Generate(
                new GeneratorOptions(true, Path.Combine(project.ProjectDirectory, "obj", "elsa-package.json"), true, "elsa-package.json", overridePath, "Error", false, false, false, "concise", []),
                ProjectPackageMetadataMapper.Map("Sample.Elsa.Package", "1.2.3", "Sample", "Sample package.", "Elsa", null, null, "elsa", null, null, "net10.0", null),
                new AssemblyInspectionInput(project.AssemblyPath, project.XmlDocumentationPath, "net10.0", [], true),
                diagnostics);

            return (artifact, diagnostics);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
