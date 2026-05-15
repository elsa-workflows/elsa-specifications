using System.Diagnostics;

namespace Elsa.PackageManifest.Generator.Testing;

public sealed class SampleProjectBuilder : IAsyncDisposable
{
    private readonly List<string> _sources = [];
    private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TargetFramework"] = "net10.0",
        ["PackageId"] = "Sample.Elsa.Package",
        ["Version"] = "1.2.3",
        ["Description"] = "Sample package for generator tests.",
        ["GenerateDocumentationFile"] = "true"
    };

    public SampleProjectBuilder()
    {
        ProjectDirectory = Path.Combine(Path.GetTempPath(), "elsa-manifest-generator-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ProjectDirectory);
    }

    public string ProjectDirectory { get; }

    public string ProjectFile => Path.Combine(ProjectDirectory, "Sample.Elsa.Package.csproj");

    public string AssemblyPath => Path.Combine(ProjectDirectory, "bin", "Debug", _properties["TargetFramework"], "Sample.Elsa.Package.dll");

    public string XmlDocumentationPath => Path.ChangeExtension(AssemblyPath, ".xml");

    public SampleProjectBuilder WithProperty(string name, string value)
    {
        _properties[name] = value;
        return this;
    }

    public SampleProjectBuilder WithSource(string source)
    {
        _sources.Add(source);
        return this;
    }

    public async Task<CommandResult> BuildAsync(CancellationToken cancellationToken = default)
    {
        WriteProject();
        return await RunDotNetAsync(["build", ProjectFile, "--nologo"], cancellationToken);
    }

    public async Task<CommandResult> PackAsync(CancellationToken cancellationToken = default)
    {
        WriteProject();
        return await RunDotNetAsync(["pack", ProjectFile, "--nologo", "--no-build"], cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(ProjectDirectory))
            Directory.Delete(ProjectDirectory, true);
        return ValueTask.CompletedTask;
    }

    private void WriteProject()
    {
        File.WriteAllText(ProjectFile, RenderProject());
        File.WriteAllText(Path.Combine(ProjectDirectory, "CShells.cs"), CShellsFeatureFixtures.AbstractionsSource);
        File.WriteAllText(Path.Combine(ProjectDirectory, "ManifestHints.cs"), CShellsFeatureFixtures.ManifestHintsSource);

        for (var i = 0; i < _sources.Count; i++)
            File.WriteAllText(Path.Combine(ProjectDirectory, $"Feature{i}.cs"), _sources[i]);
    }

    private string RenderProject()
    {
        var properties = string.Join(Environment.NewLine, _properties.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"    <{x.Key}>{x.Value}</{x.Key}>"));
        return $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
{properties}
  </PropertyGroup>
</Project>
""";
    }

    private static async Task<CommandResult> RunDotNetAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet.");
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        var output = await outputTask;
        var error = await errorTask;
        return new CommandResult(process.ExitCode, output, error);
    }
}

public sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public string CombinedOutput => StandardOutput + StandardError;
}
