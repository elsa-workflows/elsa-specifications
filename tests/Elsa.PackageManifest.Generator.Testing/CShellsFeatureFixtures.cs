namespace Elsa.PackageManifest.Generator.Testing;

public static class CShellsFeatureFixtures
{
    public const string AbstractionsSource = """
#nullable enable
using System;

namespace CShells.Features;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ShellFeatureAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public Type[] DependsOn { get; set; } = [];
    public object? Metadata { get; set; }
}

public interface IShellFeature;
""";

    public const string DelegateHooksFeatureSource = """
#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CShells.Features;

namespace Sample.Features;

[ShellFeature("DelegateHooks", DisplayName = "Delegate Hooks")]
public sealed class DelegateHooksFeature : IShellFeature
{
    public string? Endpoint { get; set; }

    public Action<DelegateHookOptions>? Configure { get; set; }

    public Func<IServiceProvider, object>? ServiceFactory { get; set; }

    public Action<IServiceProvider, HttpClient>? ConfigureHttpClient { get; set; }

    public IDictionary<string, Func<IServiceProvider, ValueTask<object>>> Factories { get; set; } =
        new Dictionary<string, Func<IServiceProvider, ValueTask<object>>>();
}

public sealed class DelegateHookOptions
{
    public string Value { get; set; } = "";
}
""";

    public const string ManifestHintsSource = """
#nullable enable
using System;

namespace Elsa.PackageManifest.Generator.Hints;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ManifestSettingAttribute : Attribute
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Group { get; set; }
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
    public string? UiHint { get; set; }
    public bool Secret { get; set; }
    public bool Sensitive { get; set; }
    public bool RestartRequired { get; set; }
    public bool Advanced { get; set; }
    public bool Experimental { get; set; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public sealed class ManifestIgnoreAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ManifestExtensionAttribute(string key, string value) : Attribute
{
    public string Key { get; } = key;
    public string Value { get; } = value;
}
""";
}
