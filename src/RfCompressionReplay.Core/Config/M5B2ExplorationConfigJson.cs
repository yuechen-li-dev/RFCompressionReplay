namespace RfCompressionReplay.Core.Config;

public static class M5B2ExplorationConfigJson
{
    public static M5B2ExplorationConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        var config = System.Text.Json.JsonSerializer.Deserialize<M5B2ExplorationConfig>(stream, ExperimentConfigJson.SerializerOptions);
        return config ?? throw new InvalidOperationException($"Could not deserialize M5b2 exploration config '{path}'.");
    }
}
