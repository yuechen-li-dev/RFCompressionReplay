using System.Text.Json;

namespace RfCompressionReplay.Core.Config;

public static class M5A3StabilityConfigJson
{
    public static M5A3StabilityConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        var config = JsonSerializer.Deserialize<M5A3StabilityConfig>(stream, ExperimentConfigJson.SerializerOptions);
        return config ?? throw new InvalidOperationException($"Could not deserialize M5a3 stability config from '{path}'.");
    }
}
