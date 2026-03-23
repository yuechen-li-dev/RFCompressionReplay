using System.Text.Json;

namespace RfCompressionReplay.Core.Config;

public static class M7B2ComplementaryBoundaryFusionConfigJson
{
    public static M7B2ComplementaryBoundaryFusionConfig Load(string path)
    {
        using var stream = File.OpenRead(path);
        var config = JsonSerializer.Deserialize<M7B2ComplementaryBoundaryFusionConfig>(stream, ExperimentConfigJson.SerializerOptions);
        return config ?? throw new InvalidOperationException($"Could not deserialize M7b2 complementary boundary fusion config from '{path}'.");
    }
}
