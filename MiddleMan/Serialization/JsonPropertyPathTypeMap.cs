using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;

namespace MiddleMan.Serialization;

internal sealed class JsonPropertyPathTypeMap
{
    private static readonly ConcurrentDictionary<Type, JsonPropertyPathTypeMap> Cache = new();

    private JsonPropertyPathTypeMap(Type type)
    {
        Properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanWrite)
            .Select(JsonPropertyMap.Create)
            .ToArray();
    }

    public IReadOnlyList<JsonPropertyMap> Properties { get; }

    public static JsonPropertyPathTypeMap Get(Type type) => Cache.GetOrAdd(type, static type => new JsonPropertyPathTypeMap(type));

    public static bool HasJsonPropertyPathMetadata(Type type)
    {
        if (JsonTypeHelpers.IsSimpleType(type) || JsonTypeHelpers.TryGetDictionaryTypes(type, out _, out _))
        {
            return false;
        }

        return Get(type).Properties.Any(property => property.Attribute is not null);
    }
}

internal sealed class JsonPropertyMap
{
    private JsonPropertyMap(
        PropertyInfo property,
        string jsonName,
        JsonPropertyPathAttribute? attribute,
        Type? collectionItemType,
        Type? dictionaryKeyType,
        Type? dictionaryValueType)
    {
        Property = property;
        JsonName = jsonName;
        Attribute = attribute;
        CollectionItemType = collectionItemType;
        DictionaryKeyType = dictionaryKeyType;
        DictionaryValueType = dictionaryValueType;
    }

    public PropertyInfo Property { get; }

    public string JsonName { get; }

    public JsonPropertyPathAttribute? Attribute { get; }

    public Type? CollectionItemType { get; }

    public Type? DictionaryKeyType { get; }

    public Type? DictionaryValueType { get; }

    public static JsonPropertyMap Create(PropertyInfo property)
    {
        JsonTypeHelpers.TryGetCollectionItemType(property.PropertyType, out var collectionItemType);
        JsonTypeHelpers.TryGetDictionaryTypes(property.PropertyType, out var dictionaryKeyType, out var dictionaryValueType);

        return new JsonPropertyMap(
            property,
            property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? JsonNameHelpers.ToCamelCase(property.Name),
            property.GetCustomAttribute<JsonPropertyPathAttribute>(),
            collectionItemType,
            dictionaryKeyType,
            dictionaryValueType);
    }
}
