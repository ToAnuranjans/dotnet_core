using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiddleMan.Serialization;

public sealed class JsonPropertyPathConverter<T> : JsonConverter<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var fallbackSource = JsonPathResolver.GetSingleRootObject(document.RootElement);
        return (T?)JsonPropertyPathMapper.Map(typeof(T), document.RootElement, fallbackSource, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, JsonPropertyPathSerializerOptions.RemoveThisFactory(options));
    }
}
