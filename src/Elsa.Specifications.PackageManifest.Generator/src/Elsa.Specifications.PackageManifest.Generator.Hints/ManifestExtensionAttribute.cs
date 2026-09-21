using System;

#nullable enable

namespace Elsa.Specifications.PackageManifest.Generator.Hints;

/// <summary>
/// Supplies a small string extension metadata value for manifest generation. On a class or property it
/// contributes to that feature's or setting's <c>extensions</c>; on an assembly it contributes to the
/// package-level <c>extensions</c>. A key declared more than once at one level becomes a sorted list of
/// its distinct values. Built-in package-level keys (such as <c>authors</c> or <c>repositoryUrl</c>) are
/// reserved and cannot be replaced from an assembly-level attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
internal sealed class ManifestExtensionAttribute(string key, string value) : Attribute
{
    public string Key { get; } = key;
    public string Value { get; } = value;
}
