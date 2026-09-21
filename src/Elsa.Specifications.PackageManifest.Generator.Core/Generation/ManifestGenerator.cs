using System.Text;
using Elsa.Specifications.PackageManifest.Generator.Core.AssemblyInspection;
using Elsa.Specifications.PackageManifest.Generator.Core.Documentation;
using Elsa.Specifications.PackageManifest.Generator.Core.Overrides;
using Elsa.Specifications.PackageManifest.Generator.Core.Validation;
using Elsa.Specifications.PackageManifests;
using Elsa.Specifications.PackageManifests.Compatibility;
using Elsa.Specifications.PackageManifests.Documentation;
using Elsa.Specifications.PackageManifests.Infrastructure;
using Elsa.Specifications.PackageManifests.Licensing;

namespace Elsa.Specifications.PackageManifest.Generator.Core.Generation;

public sealed class ManifestGenerator
{
    private static readonly Encoding ManifestEncoding = new UTF8Encoding(false);

    public GeneratedManifestArtifact Generate(
        GeneratorOptions options,
        ProjectPackageMetadata packageMetadata,
        AssemblyInspectionInput assemblyInput,
        GenerationDiagnostics? diagnostics = null)
    {
        diagnostics ??= new GenerationDiagnostics();

        var metadataReader = new FeatureMetadataReader();
        var nullableReader = new NullableMetadataReader();
        var validationMapper = new ValidationAnnotationMapper();
        var defaultValueResolver = new SettingDefaultValueResolver();
        var schemaGenerator = new SchemaGeneration.SettingSchemaGenerator();
        var verboseDiagnostics = string.Equals(options.DiagnosticsVerbosity, "verbose", StringComparison.OrdinalIgnoreCase);
        var settingDiscovery = new SettingDiscoveryService(metadataReader, nullableReader, validationMapper, defaultValueResolver, schemaGenerator, diagnostics, verboseDiagnostics);
        var featureMatcher = new FeatureTypeMatcher(options.AdditionalFeatureInterfaceTypes);
        var featureDiscovery = new FeatureDiscoveryService(featureMatcher, metadataReader, settingDiscovery);
        var assemblyReader = new AssemblyMetadataReader();
        var xmlReader = new XmlDocumentationReader();
        var xmlEnricher = new XmlDocumentationEnricher();
        var overrideReader = new ManifestOverrideReader();
        var overrideValidator = new ManifestOverrideValidator();
        var referenceResolver = new ManifestOverrideReferenceResolver();
        var merger = new ManifestMetadataMerger();

        var xmlEntries = xmlReader.Read(assemblyInput.XmlDocumentationPath);
        var discovered = assemblyReader.Read(assemblyInput.AssemblyPath, assemblyInput.ReferenceAssemblyPaths, assembly => featureDiscovery.Discover(assembly, packageMetadata));
        var features = discovered.Features;
        features = xmlEnricher.Enrich(features, xmlEntries);

        ManifestOverride? manifestOverride = null;
        try
        {
            manifestOverride = overrideReader.Read(options.OverrideFile);
            overrideValidator.Validate(manifestOverride, packageMetadata, diagnostics);
            referenceResolver.ValidateReferences(manifestOverride, features, diagnostics);
            features = merger.ApplyOverrides(features, manifestOverride);
        }
        catch (Exception ex)
        {
            diagnostics.Fatal("EPMGEN_OVERRIDE_INVALID", ex.Message, options.OverrideFile, category: GenerationDiagnosticCategory.InvalidInput);
        }

        var recommendedValidator = new RecommendedMetadataValidator();
        recommendedValidator.Validate(features, options.Strict, diagnostics);

        var manifest = BuildManifest(packageMetadata, discovered.PackageCompatibility, discovered.PackageExtensions, features, manifestOverride, diagnostics);
        var manifestJson = DeterministicJsonSerializer.Serialize(manifest);

        var sizeValidator = new GeneratedManifestSizeValidator();
        sizeValidator.Validate(manifestJson, diagnostics);

        var validator = new GeneratedManifestValidator();
        var validationResult = validator.Validate(manifestJson, packageMetadata.PackageId, packageMetadata.Version);
        foreach (var error in validationResult.Errors)
            diagnostics.Error(
                "EPMGEN_MANIFEST_INVALID",
                error.Message,
                error.Path,
                error.Path,
                error.RuleId,
                GenerationDiagnosticCategory.ManifestValidation,
                canMapValidationSeverity: true);
        foreach (var warning in validationResult.Warnings)
            diagnostics.Warning(
                "EPMGEN_MANIFEST_WARNING",
                warning.Message,
                warning.Path,
                warning.Path,
                warning.RuleId,
                GenerationDiagnosticCategory.ManifestValidation,
                canMapValidationSeverity: true);

        var outputDirectory = Path.GetDirectoryName(options.OutputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(options.OutputPath, manifestJson, ManifestEncoding);
        diagnostics.Info("EPMGEN_MANIFEST_GENERATED", $"Generated Elsa package manifest at '{options.OutputPath}'.", options.OutputPath);
        diagnostics.Info("EPMGEN_FEATURES_DISCOVERED", $"Discovered {features.Count} CShells feature(s).", assemblyInput.AssemblyPath);

        return new GeneratedManifestArtifact(
            options.OutputPath,
            options.PackagePath,
            manifestJson,
            ManifestEncoding.GetByteCount(manifestJson),
            validationResult,
            options.IncludeInPackage);
    }

    private static ElsaPackageManifest BuildManifest(ProjectPackageMetadata metadata, CompatibilityOverride? packageCompatibility, IReadOnlyDictionary<string, object?> assemblyExtensions, IReadOnlyList<DiscoveredFeature> features, ManifestOverride? manifestOverride, GenerationDiagnostics diagnostics)
    {
        var packageOverride = manifestOverride?.Package;
        var builtInExtensions = new Dictionary<string, object?>
        {
            ["authors"] = metadata.Authors,
            ["repositoryUrl"] = metadata.RepositoryUrl,
            ["readmeFile"] = metadata.PackageReadmeFile,
            ["targetFrameworks"] = metadata.TargetFrameworks
        };
        var reservedAssemblyExtensions = RejectReservedAssemblyExtensions(assemblyExtensions, builtInExtensions.Keys, diagnostics);

        return new ElsaPackageManifest
        {
            Package = new PackageIdentityManifest
            {
                Id = metadata.PackageId,
                Version = metadata.Version
            },
            DisplayName = packageOverride?.DisplayName ?? DefaultPackageDisplayName(metadata),
            Description = packageOverride?.Description ?? metadata.Description,
            Tags = packageOverride?.Tags ?? metadata.PackageTags,
            Features = features.Select(ToFeatureManifest).ToArray(),
            Compatibility = ToCompatibility(MergeCompatibility(packageCompatibility, packageOverride?.Compatibility)),
            Dependencies = ToDependencies(packageOverride?.Dependencies),
            Conflicts = ToConflicts(packageOverride?.Conflicts),
            License = ToLicense(packageOverride?.License, metadata.PackageLicenseExpression),
            Documentation = ToDocumentation(packageOverride?.Documentation, metadata.PackageProjectUrl),
            // Precedence, lowest to highest: assembly-level [ManifestExtension] (built-in key names rejected
            // above regardless of the built-in's own value), then the override file, then the built-in package
            // keys themselves, which the override file can still supply when the project defines no value.
            Extensions = MergeExtensions(MergeExtensions(reservedAssemblyExtensions, packageOverride?.Extensions), builtInExtensions)
        };
    }

    /// <summary>
    /// Assembly-level [ManifestExtension] keys that name a built-in package key (compared ordinal
    /// ignore-case) never reach the package-level extensions, regardless of whether the built-in
    /// itself has a value. This keeps a project with no repository URL or readme file from having
    /// an attribute silently supply one.
    /// </summary>
    private static IReadOnlyDictionary<string, object?> RejectReservedAssemblyExtensions(IReadOnlyDictionary<string, object?> assemblyExtensions, IEnumerable<string> reservedKeys, GenerationDiagnostics diagnostics)
    {
        var reserved = new HashSet<string>(reservedKeys, StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in assemblyExtensions)
        {
            if (reserved.Contains(item.Key))
            {
                diagnostics.Warning(
                    "EPMGEN_EXTENSION_RESERVED_KEY",
                    $"Assembly-level [ManifestExtension(\"{item.Key}\", ...)] was ignored because '{item.Key}' is a built-in package key.",
                    item.Key,
                    category: GenerationDiagnosticCategory.InvalidInput);
                continue;
            }

            result[item.Key] = item.Value;
        }

        return result;
    }

    private static string DefaultPackageDisplayName(ProjectPackageMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.Title) &&
            !string.Equals(metadata.Title, metadata.PackageId, StringComparison.OrdinalIgnoreCase))
            return metadata.Title;

        return NamingHelpers.ToPackageDisplayName(metadata.PackageId);
    }

    private static FeatureManifest ToFeatureManifest(DiscoveredFeature feature) => new()
    {
        Id = feature.FeatureId,
        TypeName = feature.ClrTypeName,
        DisplayName = feature.DisplayName,
        Description = feature.Description,
        Category = feature.Categories.FirstOrDefault(),
        Categories = feature.Categories,
        Compatibility = ToCompatibility(feature.Compatibility),
        Settings = feature.Settings.Select(ToSettingManifest).ToArray(),
        Dependencies = feature.Dependencies.Select(x => new DependencyManifest { PackageId = x.PackageId, VersionRange = x.VersionRange, FeatureId = x.FeatureId }).ToArray(),
        Conflicts = feature.Conflicts.Select(x => new ConflictManifest { PackageId = x.PackageId, VersionRange = x.VersionRange, FeatureId = x.FeatureId, Reason = x.Reason }).ToArray(),
        RequiredCapabilities = feature.RequiredCapabilities,
        Infrastructure = feature.Infrastructure.Select(ToInfrastructureRequirementManifest).ToArray(),
        Advanced = feature.Advanced,
        Experimental = feature.Experimental,
        Extensions = MergeExtensions(feature.ExtensionMetadata, new Dictionary<string, object?>
        {
            ["cshellsFeatureName"] = feature.CShellsFeatureName
        })
    };

    private static InfrastructureRequirementManifest ToInfrastructureRequirementManifest(ManifestInfrastructureRequirementReference requirement) => new()
    {
        Id = requirement.Id,
        Kind = requirement.Kind,
        Optional = requirement.Optional,
        Reason = requirement.Reason,
        Capabilities = requirement.Capabilities,
        Providers = requirement.Providers,
        ConfigurationKeys = requirement.ConfigurationKeys,
        Extensions = new Dictionary<string, object?>(requirement.Extensions, StringComparer.OrdinalIgnoreCase)
    };

    private static FeatureSettingManifest ToSettingManifest(DiscoveredSetting setting) => new()
    {
        Name = setting.Name,
        ClrType = setting.ClrType,
        JsonType = setting.JsonType,
        Required = setting.Required,
        DefaultValue = setting.DefaultValue,
        DisplayName = setting.DisplayName,
        Description = setting.Description,
        Category = setting.Category,
        Validation = new Dictionary<string, object?>(setting.ValidationConstraints, StringComparer.OrdinalIgnoreCase),
        Secret = setting.Secret,
        RestartRequired = setting.RestartRequired,
        UI = BuildSettingUI(setting),
        Extensions = MergeExtensions(setting.ExtensionMetadata, new Dictionary<string, object?>
        {
            ["configurationPath"] = setting.ConfigurationPath,
            ["nullable"] = setting.Nullable,
            ["sensitive"] = setting.Sensitive
        })
    };

    private static Dictionary<string, object?> BuildSettingUI(DiscoveredSetting setting)
    {
        var values = MergeExtensions(null, new Dictionary<string, object?>
        {
            ["hint"] = setting.UIHint,
            ["group"] = setting.Group,
            ["advanced"] = setting.Advanced,
            ["experimental"] = setting.Experimental
        });

        var options = BuildSettingUIOptions(setting);
        if (options.Count > 0)
            values["options"] = options;

        return values;
    }

    private static Dictionary<string, object?> BuildSettingUIOptions(DiscoveredSetting setting)
    {
        if (!SupportsUIOptions(setting.UIHint))
            return [];

        if (setting.UIOptionsProvider is not null)
        {
            return MergeExtensions(null, new Dictionary<string, object?>
            {
                ["source"] = "provider",
                ["provider"] = setting.UIOptionsProvider.Provider,
                ["dependsOn"] = setting.UIOptionsProvider.DependsOn.Count > 0 ? setting.UIOptionsProvider.DependsOn : null,
                ["parameters"] = setting.UIOptionsProvider.Parameters.Count > 0 ? setting.UIOptionsProvider.Parameters : null
            });
        }

        if (setting.UIOptions.Count == 0)
            return [];

        return MergeExtensions(null, new Dictionary<string, object?>
        {
            ["source"] = "static",
            ["items"] = setting.UIOptions.Select(ToUIOption).ToArray()
        });
    }

    private static bool SupportsUIOptions(string? uiHint) =>
        string.IsNullOrWhiteSpace(uiHint) ||
        string.Equals(uiHint, "select-list", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(uiHint, "multi-select-list", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(uiHint, "radio-list", StringComparison.OrdinalIgnoreCase);

    private static Dictionary<string, object?> ToUIOption(ManifestUIOptionReference option) =>
        MergeExtensions(null, new Dictionary<string, object?>
        {
            ["value"] = option.Value,
            ["label"] = option.Label,
            ["description"] = option.Description
        });

    private static DocumentationManifest? ToDocumentation(DocumentationOverride? documentation, string? projectUrl)
    {
        if (documentation is null && string.IsNullOrWhiteSpace(projectUrl))
            return null;

        return new DocumentationManifest
        {
            ProjectUrl = documentation?.Url ?? projectUrl,
            ReadmeUrl = documentation?.Readme,
            Extensions = MergeExtensions(null, new Dictionary<string, object?>
            {
                ["remarks"] = documentation?.Remarks,
                ["examples"] = documentation?.Examples
            })
        };
    }

    private static LicenseManifest? ToLicense(LicenseOverride? license, string? licenseExpression)
    {
        if (license is null && string.IsNullOrWhiteSpace(licenseExpression))
            return null;

        return new LicenseManifest
        {
            Expression = license?.Expression ?? licenseExpression,
            Url = license?.Url
        };
    }

    private static CompatibilityManifest? ToCompatibility(CompatibilityOverride? compatibility)
    {
        if (compatibility is null)
            return null;

        return new CompatibilityManifest
        {
            ElsaVersionRange = compatibility.ElsaVersionRange,
            DockerImageVersionRange = compatibility.DockerImageVersionRange,
            RuntimeKinds = compatibility.RuntimeKinds ?? [],
            RuntimeCapabilities = compatibility.RuntimeCapabilities ?? [],
            Extensions = compatibility.Extensions ?? []
        };
    }

    private static CompatibilityOverride? MergeCompatibility(CompatibilityOverride? discovered, CompatibilityOverride? overrideCompatibility)
    {
        if (discovered is null)
            return overrideCompatibility;

        if (overrideCompatibility is null)
            return discovered;

        return new CompatibilityOverride
        {
            RuntimeKinds = overrideCompatibility.RuntimeKinds ?? discovered.RuntimeKinds,
            ElsaVersionRange = overrideCompatibility.ElsaVersionRange ?? discovered.ElsaVersionRange,
            DockerImageVersionRange = overrideCompatibility.DockerImageVersionRange ?? discovered.DockerImageVersionRange,
            RuntimeCapabilities = overrideCompatibility.RuntimeCapabilities ?? discovered.RuntimeCapabilities,
            Extensions = MergeExtensions(discovered.Extensions, overrideCompatibility.Extensions)
        };
    }

    private static IReadOnlyList<DependencyManifest> ToDependencies(IReadOnlyList<DependencyOverride>? dependencies) =>
        dependencies?.Select(x => new DependencyManifest { PackageId = x.PackageId, VersionRange = x.VersionRange, FeatureId = x.FeatureId }).ToArray() ?? [];

    private static IReadOnlyList<ConflictManifest> ToConflicts(IReadOnlyList<ConflictOverride>? conflicts) =>
        conflicts?.Select(x => new ConflictManifest { PackageId = x.PackageId, VersionRange = x.VersionRange, FeatureId = x.FeatureId, Reason = x.Reason }).ToArray() ?? [];

    private static Dictionary<string, object?> MergeExtensions(IReadOnlyDictionary<string, object?>? first, IReadOnlyDictionary<string, object?>? second)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in first ?? new Dictionary<string, object?>())
            if (item.Value is not null)
                result[item.Key] = item.Value;
        foreach (var item in second ?? new Dictionary<string, object?>())
            if (item.Value is not null)
                result[item.Key] = item.Value;
        return result;
    }

}
