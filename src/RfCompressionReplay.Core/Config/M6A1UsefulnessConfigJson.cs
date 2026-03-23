using System.Text.Json;

namespace RfCompressionReplay.Core.Config;

public static class M6A1UsefulnessConfigJson
{
    public static M6A1UsefulnessConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        var config = JsonSerializer.Deserialize<M6A1UsefulnessConfig>(stream, ExperimentConfigJson.SerializerOptions);
        return config ?? throw new InvalidOperationException($"Could not deserialize M6a1 usefulness config from '{path}'.");
    }
}
