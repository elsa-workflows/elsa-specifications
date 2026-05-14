using Elsa.PackageManifests.Validation;
using FluentAssertions;

namespace Elsa.PackageManifests.Tests;

public sealed class ManifestSchemaValidationTests
{
    private readonly ManifestValidator _validator = new();

    [Fact]
    public void Validate_accepts_minimal_supported_manifest()
    {
        var result = _validator.Validate("""
        {
          "schemaVersion": "1.0",
          "package": { "id": "Elsa.Email", "version": "1.0.0" },
          "displayName": "Email"
        }
        """);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_rejects_unsupported_schema()
    {
        var result = _validator.Validate("""
        {
          "schemaVersion": "99.0",
          "package": { "id": "Elsa.Email", "version": "1.0.0" },
          "displayName": "Email"
        }
        """);

        result.Status.Should().Be(ManifestValidationStatus.UnsupportedSchema);
    }

    [Fact]
    public void Validate_rejects_oversized_manifest()
    {
        var json = new string(' ', ManifestValidator.MaxManifestBytes + 1);

        var result = _validator.Validate(json);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.RuleId == "manifest.size");
    }
}
