using System.Text;
using System.Text.Json;

namespace MiddleMan.Serialization;

internal static class JsonPathResolver
{
    public static bool TryResolve(JsonElement primarySource, JsonElement fallbackSource, string path, out JsonElement value)
    {
        if (TryResolve(primarySource, path, out value))
        {
            return true;
        }

        return TryResolve(fallbackSource, path, out value);
    }

    public static bool TryResolve(JsonElement source, string path, out JsonElement value)
    {
        value = source;
        foreach (var token in Tokenize(path))
        {
            if (!TryApplyToken(value, token, out value))
            {
                return false;
            }
        }

        return true;
    }

    public static JsonElement GetSingleRootObject(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1)
        {
            return root;
        }

        return root.EnumerateObject().First().Value;
    }

    public static bool IsArrayWildcardPath(string path) => path.Contains("[n]", StringComparison.Ordinal);

    public static string GetArraySourcePath(string path)
    {
        var wildcardIndex = path.IndexOf("[n]", StringComparison.Ordinal);
        return wildcardIndex < 0 ? path : path[..wildcardIndex].TrimEnd('.');
    }

    public static string GetArrayItemPath(string path)
    {
        var wildcardIndex = path.IndexOf("[n]", StringComparison.Ordinal);
        if (wildcardIndex < 0)
        {
            return string.Empty;
        }

        var itemPathStart = wildcardIndex + "[n]".Length;
        return itemPathStart >= path.Length ? string.Empty : path[itemPathStart..].TrimStart('.');
    }

    private static bool TryApplyToken(JsonElement source, JsonPathToken token, out JsonElement value)
    {
        value = source;
        return token.Kind switch
        {
            JsonPathTokenKind.Property => source.ValueKind == JsonValueKind.Object
                && source.TryGetProperty(token.Value, out value),
            JsonPathTokenKind.ArrayIndex => source.ValueKind == JsonValueKind.Array
                && TryGetArrayIndex(source, int.Parse(token.Value), out value),
            JsonPathTokenKind.DictionaryKey => source.ValueKind == JsonValueKind.Object
                && source.TryGetProperty(token.Value, out value),
            JsonPathTokenKind.ArrayWildcard => false,
            _ => false
        };
    }

    private static bool TryGetArrayIndex(JsonElement source, int index, out JsonElement value)
    {
        value = default;
        if (index < 0 || index >= source.GetArrayLength())
        {
            return false;
        }

        value = source.EnumerateArray().ElementAt(index);
        return true;
    }

    private static IEnumerable<JsonPathToken> Tokenize(string path)
    {
        var segment = new StringBuilder();
        for (var i = 0; i < path.Length; i++)
        {
            var current = path[i];
            if (current == '.')
            {
                if (TryTakeProperty(segment, out var propertyToken))
                {
                    yield return propertyToken;
                }

                continue;
            }

            if (current != '[')
            {
                segment.Append(current);
                continue;
            }

            if (TryTakeProperty(segment, out var propertyTokenBeforeIndexer))
            {
                yield return propertyTokenBeforeIndexer;
            }

            var endIndex = path.IndexOf(']', i);
            if (endIndex < 0)
            {
                throw new JsonException($"JSON property path '{path}' has an unterminated indexer.");
            }

            var indexer = path[(i + 1)..endIndex].Trim();
            if (string.Equals(indexer, "n", StringComparison.OrdinalIgnoreCase))
            {
                yield return new JsonPathToken(JsonPathTokenKind.ArrayWildcard, indexer);
            }
            else if (int.TryParse(indexer, out _))
            {
                yield return new JsonPathToken(JsonPathTokenKind.ArrayIndex, indexer);
            }
            else
            {
                yield return new JsonPathToken(JsonPathTokenKind.DictionaryKey, indexer.Trim('"', '\''));
            }

            i = endIndex;
        }

        if (TryTakeProperty(segment, out var finalPropertyToken))
        {
            yield return finalPropertyToken;
        }
    }

    private static bool TryTakeProperty(StringBuilder builder, out JsonPathToken token)
    {
        token = default;
        if (builder.Length == 0)
        {
            return false;
        }

        var propertyName = builder.ToString().Trim();
        builder.Clear();
        if (string.IsNullOrEmpty(propertyName))
        {
            return false;
        }

        token = new JsonPathToken(JsonPathTokenKind.Property, propertyName);
        return true;
    }

    private readonly record struct JsonPathToken(JsonPathTokenKind Kind, string Value);

    private enum JsonPathTokenKind
    {
        Property,
        ArrayWildcard,
        ArrayIndex,
        DictionaryKey
    }
}
