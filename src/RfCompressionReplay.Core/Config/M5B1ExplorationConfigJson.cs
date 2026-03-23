namespace RfCompressionReplay.Core.Config;

public static class M5B1ExplorationConfigJson
{
    public static M5B1ExplorationConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        var config = System.Text.Json.JsonSerializer.Deserialize<M5B1ExplorationConfig>(stream, ExperimentConfigJson.SerializerOptions);
        return config ?? throw new InvalidOperationException($"Could not deserialize M5b1 exploration config from '{path}'.");
    }
}
