using System.Reflection;

namespace Elsa.PackageManifest.Generator.Core.Generation;

public sealed class SettingDefaultValueResolver
{
    public object? Resolve(PropertyInfo property, string? hintDefaultValue)
    {
        if (!string.IsNullOrWhiteSpace(hintDefaultValue))
            return ConvertString(hintDefaultValue, property.PropertyType);

        var defaultValueAttribute = property.GetCustomAttributesData()
            .FirstOrDefault(x => x.AttributeType.FullName == "System.ComponentModel.DefaultValueAttribute");

        if (defaultValueAttribute?.ConstructorArguments.Count > 0)
            return defaultValueAttribute.ConstructorArguments[0].Value;

        return null;
    }

    private static object? ConvertString(string value, Type targetType)
    {
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (type == typeof(string))
            return value;
        if (type == typeof(bool) && bool.TryParse(value, out var b))
            return b;
        if (type == typeof(int) && int.TryParse(value, out var i))
            return i;
        if (type == typeof(long) && long.TryParse(value, out var l))
            return l;
        if (type == typeof(double) && double.TryParse(value, out var d))
            return d;
        if (type == typeof(decimal) && decimal.TryParse(value, out var m))
            return m;

        return value;
    }
}
