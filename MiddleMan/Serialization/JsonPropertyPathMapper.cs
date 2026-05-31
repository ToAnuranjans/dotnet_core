using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiddleMan.Serialization;

internal static class JsonPropertyPathMapper
{
    public static object? Map(Type targetType, JsonElement primarySource, JsonElement fallbackSource, JsonSerializerOptions options)
    {
        if (JsonTypeHelpers.IsSimpleType(targetType))
        {
            return DeserializeElement(primarySource, targetType, options);
        }

        if (JsonTypeHelpers.TryGetDictionaryTypes(targetType, out _, out _))
        {
            return DeserializeElement(primarySource, targetType, options);
        }

        var target = Activator.CreateInstance(targetType)
            ?? throw new JsonException($"Could not create '{targetType.Name}'.");

        foreach (var propertyMap in JsonPropertyPathTypeMap.Get(targetType).Properties)
        {
            if (!propertyMap.Property.CanWrite)
            {
                continue;
            }

            var value = propertyMap.Attribute is null
                ? ReadConventionProperty(propertyMap, primarySource, options)
                : ReadAttributedProperty(propertyMap, primarySource, fallbackSource, options);

            if (value is not null || JsonTypeHelpers.IsNullable(propertyMap.Property.PropertyType))
            {
                propertyMap.Property.SetValue(target, value);
            }
        }

        return target;
    }

    private static object? ReadAttributedProperty(
        JsonPropertyMap propertyMap,
        JsonElement primarySource,
        JsonElement fallbackSource,
        JsonSerializerOptions options)
    {
        var attribute = propertyMap.Attribute!;
        if (propertyMap.CollectionItemType is not null)
        {
            return ReadCollectionProperty(propertyMap, attribute, primarySource, fallbackSource, options);
        }

        if (propertyMap.DictionaryKeyType is not null)
        {
            if (!JsonPathResolver.TryResolve(primarySource, fallbackSource, attribute.Path, out var dictionaryElement))
            {
                return MissingValue(propertyMap, attribute);
            }

            return DeserializeElement(dictionaryElement, propertyMap.Property.PropertyType, options);
        }

        if (attribute.CompositeProperties.Count > 0)
        {
            return ReadCompositeValue(propertyMap, attribute, primarySource, fallbackSource);
        }

        if (!JsonPathResolver.TryResolve(primarySource, fallbackSource, attribute.Path, out var valueElement))
        {
            return MissingValue(propertyMap, attribute);
        }

        return MapElementToType(propertyMap.Property.PropertyType, valueElement, valueElement, options);
    }

    private static object? ReadCollectionProperty(
        JsonPropertyMap propertyMap,
        JsonPropertyPathAttribute attribute,
        JsonElement primarySource,
        JsonElement fallbackSource,
        JsonSerializerOptions options)
    {
        if (!JsonPathResolver.IsArrayWildcardPath(attribute.Path))
        {
            throw new JsonException($"JSON property path '{attribute.Path}' must contain '[n]' for collection property '{propertyMap.Property.Name}'.");
        }

        var arrayPath = JsonPathResolver.GetArraySourcePath(attribute.Path);
        if (!JsonPathResolver.TryResolve(primarySource, fallbackSource, arrayPath, out var arrayElement))
        {
            return MissingValue(propertyMap, attribute);
        }

        if (arrayElement.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"JSON property path '{arrayPath}' must be an array.");
        }

        var items = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(propertyMap.CollectionItemType!))!;
        var itemPath = JsonPathResolver.GetArrayItemPath(attribute.Path);
        foreach (var itemElement in arrayElement.EnumerateArray())
        {
            var itemSource = itemElement;
            if (!string.IsNullOrEmpty(itemPath) && !JsonPathResolver.TryResolve(itemElement, itemPath, out itemSource))
            {
                if (attribute.Required)
                {
                    throw new JsonException($"JSON property path '{attribute.Path}' is missing.");
                }

                continue;
            }

            items.Add(MapElementToType(propertyMap.CollectionItemType!, itemSource, itemSource, options));
        }

        if (propertyMap.Property.PropertyType.IsArray)
        {
            var array = Array.CreateInstance(propertyMap.CollectionItemType!, items.Count);
            items.CopyTo(array, 0);
            return array;
        }

        if (propertyMap.Property.PropertyType.IsAssignableFrom(items.GetType()))
        {
            return items;
        }

        return DeserializeElement(JsonSerializer.SerializeToElement(items, JsonPropertyPathSerializerOptions.RemoveThisFactory(options)), propertyMap.Property.PropertyType, options);
    }

    private static object? ReadCompositeValue(
        JsonPropertyMap propertyMap,
        JsonPropertyPathAttribute attribute,
        JsonElement primarySource,
        JsonElement fallbackSource)
    {
        var paths = new[] { attribute.Path }.Concat(attribute.CompositeProperties);
        var values = paths
            .Select(path => JsonPathResolver.TryResolve(primarySource, fallbackSource, path, out var valueElement)
                ? GetElementText(valueElement)
                : null)
            .Where(value => !string.IsNullOrWhiteSpace(value));

        var joinedValue = string.Join(attribute.Separator, values);
        if (string.IsNullOrEmpty(joinedValue) && attribute.Required)
        {
            throw new JsonException($"JSON property path '{attribute.Path}' is missing.");
        }

        return ConvertString(joinedValue, propertyMap.Property.PropertyType);
    }

    private static object? ReadConventionProperty(JsonPropertyMap propertyMap, JsonElement source, JsonSerializerOptions options)
    {
        if (source.ValueKind != JsonValueKind.Object || !source.TryGetProperty(propertyMap.JsonName, out var valueElement))
        {
            return null;
        }

        return MapElementToType(propertyMap.Property.PropertyType, valueElement, valueElement, options);
    }

    private static object? MapElementToType(Type targetType, JsonElement primarySource, JsonElement fallbackSource, JsonSerializerOptions options)
    {
        if (primarySource.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (JsonTypeHelpers.IsSimpleType(targetType) || JsonTypeHelpers.TryGetDictionaryTypes(targetType, out _, out _))
        {
            return DeserializeElement(primarySource, targetType, options);
        }

        if (JsonTypeHelpers.TryGetCollectionItemType(targetType, out _))
        {
            return DeserializeElement(primarySource, targetType, options);
        }

        if (!JsonPropertyPathTypeMap.HasJsonPropertyPathMetadata(targetType))
        {
            return DeserializeElement(primarySource, targetType, options);
        }

        return Map(targetType, primarySource, fallbackSource, options);
    }

    private static object? MissingValue(JsonPropertyMap propertyMap, JsonPropertyPathAttribute attribute)
    {
        if (attribute.Required)
        {
            throw new JsonException($"JSON property path '{attribute.Path}' is missing.");
        }

        return JsonTypeHelpers.IsNullable(propertyMap.Property.PropertyType)
            ? null
            : Activator.CreateInstance(propertyMap.Property.PropertyType);
    }

    private static object? DeserializeElement(JsonElement element, Type targetType, JsonSerializerOptions options)
    {
        return element.Deserialize(targetType, JsonPropertyPathSerializerOptions.RemoveThisFactory(options));
    }

    private static string? GetElementText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.ToString()
        };
    }

    private static object? ConvertString(string value, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType);
        var concreteType = nullableType ?? targetType;

        if (concreteType == typeof(string))
        {
            return value;
        }

        if (string.IsNullOrWhiteSpace(value) && nullableType is not null)
        {
            return null;
        }

        if (concreteType.IsEnum)
        {
            return Enum.Parse(concreteType, value, ignoreCase: true);
        }

        return Convert.ChangeType(value, concreteType, CultureInfo.InvariantCulture);
    }
}
