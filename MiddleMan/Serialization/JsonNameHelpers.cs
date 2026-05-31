namespace MiddleMan.Serialization;

internal static class JsonNameHelpers
{
    public static string ToCamelCase(string value) => string.IsNullOrEmpty(value)
        ? value
        : char.ToLowerInvariant(value[0]) + value[1..];
}
