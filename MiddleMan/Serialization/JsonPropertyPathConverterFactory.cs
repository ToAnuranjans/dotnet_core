using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiddleMan.Serialization;

public sealed class JsonPropertyPathConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, bool> CanConvertCache = new();

    public override bool CanConvert(Type typeToConvert)
    {
        return CanConvertCache.GetOrAdd(typeToConvert, JsonPropertyPathTypeMap.HasJsonPropertyPathMetadata);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(JsonPropertyPathConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
