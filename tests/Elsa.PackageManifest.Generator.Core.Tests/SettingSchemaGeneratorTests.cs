using Elsa.PackageManifest.Generator.Core.SchemaGeneration;
using Elsa.PackageManifest.Generator.Core.Validation;
using FluentAssertions;

namespace Elsa.PackageManifest.Generator.Core.Tests;

public sealed class SettingSchemaGeneratorTests
{
    public static TheoryData<Type, string> CommonClrTypes => new()
    {
        { typeof(string), "string" },
        { typeof(bool), "boolean" },
        { typeof(int), "integer" },
        { typeof(decimal), "number" },
        { typeof(Uri), "string" }
    };

    [Theory]
    [MemberData(nameof(CommonClrTypes))]
    public void Generate_maps_common_clr_types(Type type, string expectedJsonType)
    {
        var schema = new SettingSchemaGenerator().Generate(type, true, new Dictionary<string, object?>());

        schema.JsonType.Should().Be(expectedJsonType);
        schema.Diagnostics.Should().BeEmpty();
    }
}
