using System.Text.Json;
using System.Text.Json.Serialization;

namespace RfCompressionReplay.Core.Config;

public static class ExperimentConfigJson
{
    public static JsonSerializerOptions SerializerOptions { get; } = CreateOptions();

    public static ExperimentConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        var config = JsonSerializer.Deserialize<ExperimentConfig>(stream, SerializerOptions);
        return config ?? throw new InvalidOperationException($"Could not deserialize experiment config from '{path}'.");
    }

    public static void Save<T>(string path, T value)
    {
        using var stream = File.Create(path);
        JsonSerializer.Serialize(stream, value, SerializerOptions);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}
