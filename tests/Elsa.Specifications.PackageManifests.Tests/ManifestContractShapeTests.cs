using System.Reflection;
using Elsa.Specifications.PackageManifests;
using Elsa.Specifications.PackageManifests.Compatibility;
using Elsa.Specifications.PackageManifests.Documentation;
using Elsa.Specifications.PackageManifests.Infrastructure;
using Elsa.Specifications.PackageManifests.Licensing;
using Elsa.Specifications.PackageManifests.Validation;

namespace Elsa.Specifications.PackageManifests.Tests;

public sealed class ManifestContractShapeTests
{
    [Fact]
    public void Public_contract_types_are_available()
    {
        var types = new[]
        {
            typeof(ElsaPackageManifest),
            typeof(FeatureManifest),
            typeof(FeatureSettingManifest),
            typeof(CompatibilityManifest),
            typeof(DependencyManifest),
            typeof(ConflictManifest),
            typeof(InfrastructureRequirementManifest),
            typeof(LicenseManifest),
            typeof(DocumentationManifest),
            typeof(ManifestValidationResult)
        };

        Assert.All(types, type => Assert.True(type.IsPublic || type.GetTypeInfo().IsNestedPublic));
    }
}
