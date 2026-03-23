using System.Text.Json;

namespace RfCompressionReplay.Core.Config;

public static class M6A2ComplementaryValueConfigJson
{
    public static M6A2ComplementaryValueConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        var config = JsonSerializer.Deserialize<M6A2ComplementaryValueConfig>(stream, ExperimentConfigJson.SerializerOptions);
        return config ?? throw new InvalidOperationException($"Could not deserialize M6a2 complementary-value config from '{path}'.");
    }
}
