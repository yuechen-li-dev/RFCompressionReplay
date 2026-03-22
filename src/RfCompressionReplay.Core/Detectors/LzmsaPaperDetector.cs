using RfCompressionReplay.Core.Compression;
using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Detectors;

public sealed class LzmsaPaperDetector : IDetector
{
    private readonly LzmsaWindowSerializer _serializer;
    private readonly ICompressionCodec _compressionCodec;

    public LzmsaPaperDetector(LzmsaWindowSerializer serializer, ICompressionCodec compressionCodec)
    {
        _serializer = serializer;
        _compressionCodec = compressionCodec;
    }

    public string Name => DetectorCatalog.LzmsaPaperDetectorName;

    public DetectorResult Evaluate(DetectorInput input, DetectorConfig config)
    {
        var serialized = _serializer.Serialize(input.Windows);
        var compressed = _compressionCodec.Compress(serialized);
        var byteSum = compressed.Sum(static value => (double)value);
        var score = DetectorMath.RoundScore(byteSum);

        var metrics = new Dictionary<string, double>
        {
            ["serializedByteCount"] = serialized.Length,
            ["compressedByteCount"] = compressed.Length,
            ["compressedByteSum"] = score,
        };

        return new DetectorResult(config.Name, config.Mode, score, score >= config.Threshold, metrics);
    }
}
