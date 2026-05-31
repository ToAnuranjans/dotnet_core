using System.Text.Json;

namespace MiddleMan.Serialization;

internal static class JsonPropertyPathSerializerOptions
{
    public static JsonSerializerOptions RemoveThisFactory(JsonSerializerOptions options)
    {
        var safeOptions = new JsonSerializerOptions(options);
        for (var i = safeOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (safeOptions.Converters[i] is JsonPropertyPathConverterFactory)
            {
                safeOptions.Converters.RemoveAt(i);
            }
        }

        return safeOptions;
    }
}
