using System;

#nullable enable

namespace Elsa.PackageManifest.Generator.Hints;

/// <summary>
/// Supplies manifest-only infrastructure requirement metadata for a CShells feature.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal sealed class ManifestInfrastructureAttribute(string id, string kind) : Attribute
{
    public string Id { get; } = id;
    public string Kind { get; } = kind;
    public bool Optional { get; set; }
    public string? Reason { get; set; }
    public string[] Capabilities { get; set; } = [];
    public string[] Providers { get; set; } = [];
    public string[] ConfigurationKeys { get; set; } = [];
    public string[] Extensions { get; set; } = [];
}
