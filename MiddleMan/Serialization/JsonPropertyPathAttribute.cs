namespace MiddleMan.Serialization;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class JsonPropertyPathAttribute(string path, params string[] compositeProperties) : Attribute
{
    public string Path { get; } = string.IsNullOrWhiteSpace(path)
        ? throw new ArgumentException("JSON property path cannot be empty.", nameof(path))
        : path;

    public IReadOnlyList<string> CompositeProperties { get; } = compositeProperties;

    public string Separator { get; init; } = ", ";

    public bool Required { get; init; }
}
