using System.Text.Json;

namespace RfCompressionReplay.Core.Config;

public static class M7BChangePointConfigJson
{
    public static M7BChangePointConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        var config = JsonSerializer.Deserialize<M7BChangePointConfig>(stream, ExperimentConfigJson.SerializerOptions);
        return config ?? throw new InvalidOperationException($"Could not deserialize M7b change-point config from '{path}'.");
    }
}
