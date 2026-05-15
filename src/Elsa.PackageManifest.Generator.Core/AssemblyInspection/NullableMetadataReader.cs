using System.Reflection;

namespace Elsa.PackageManifest.Generator.Core.AssemblyInspection;

public sealed class NullableMetadataReader
{
    public bool IsNullable(PropertyInfo property)
    {
        var type = property.PropertyType;
        if (!type.IsValueType)
            return HasNullableAttribute(property);

        return Nullable.GetUnderlyingType(type) is not null;
    }

    private static bool HasNullableAttribute(PropertyInfo property)
    {
        var attributes = property.GetCustomAttributesData()
            .Concat(property.DeclaringType?.GetCustomAttributesData() ?? []);

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeType.FullName != "System.Runtime.CompilerServices.NullableAttribute")
                continue;

            if (attribute.ConstructorArguments.Count == 0)
                continue;

            var value = attribute.ConstructorArguments[0].Value;
            if (value is byte b)
                return b == 2;
        }

        return true;
    }
}
