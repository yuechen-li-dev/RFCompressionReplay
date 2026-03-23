namespace RfCompressionReplay.Core.Config;

public static class M5B3ExplorationConfigJson
{
    public static M5B3ExplorationConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        var config = System.Text.Json.JsonSerializer.Deserialize<M5B3ExplorationConfig>(stream, ExperimentConfigJson.SerializerOptions);
        return config ?? throw new InvalidOperationException($"Could not deserialize M5b3 exploration config '{path}'.");
    }
}
