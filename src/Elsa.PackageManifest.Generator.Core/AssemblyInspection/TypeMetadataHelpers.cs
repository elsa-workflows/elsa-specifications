namespace Elsa.PackageManifest.Generator.Core.AssemblyInspection;

internal static class TypeMetadataHelpers
{
    public static Type GetNonNullableType(Type type) =>
        IsNullableValueType(type) ? type.GetGenericArguments()[0] : type;

    public static bool IsNullableValueType(Type type) =>
        type is { IsValueType: true, IsGenericType: true } &&
        string.Equals(type.GetGenericTypeDefinition().FullName, "System.Nullable`1", StringComparison.Ordinal);
}
