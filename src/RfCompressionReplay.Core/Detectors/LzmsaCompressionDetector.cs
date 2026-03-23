using RfCompressionReplay.Core.Compression;
using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Detectors;

public class LzmsaCompressionDetector : IDetector
{
    private readonly string _detectorName;
    private readonly LzmsaWindowSerializer _serializer;
    private readonly ICompressionCodec _compressionCodec;
    private readonly Func<LzmsaCompressionAnalysis, double> _scoreSelector;

    public LzmsaCompressionDetector(
        string detectorName,
        LzmsaWindowSerializer serializer,
        ICompressionCodec compressionCodec,
        Func<LzmsaCompressionAnalysis, double> scoreSelector)
    {
        _detectorName = detectorName;
        _serializer = serializer;
        _compressionCodec = compressionCodec;
        _scoreSelector = scoreSelector;
    }

    public string Name => _detectorName;

    public DetectorResult Evaluate(DetectorInput input, DetectorConfig config)
    {
        var analysis = Analyze(input);
        var score = DetectorMath.RoundScore(_scoreSelector(analysis));

        var metrics = new Dictionary<string, double>
        {
            ["serializedByteCount"] = analysis.SerializedByteCount,
            ["inputByteCount"] = analysis.InputByteCount,
            ["compressedByteCount"] = analysis.CompressedByteCount,
            ["compressedByteSum"] = DetectorMath.RoundScore(analysis.CompressedByteSum),
        };

        return new DetectorResult(config.Name, config.Mode, score, DetectorCatalog.IsPositiveAtThreshold(config.Name, score, config.Threshold), metrics);
    }

    private LzmsaCompressionAnalysis Analyze(DetectorInput input)
    {
        var serialized = _serializer.Serialize(input.Windows);
        var compressed = _compressionCodec.Compress(serialized);
        var byteSum = compressed.Sum(static value => (double)value);

        return new LzmsaCompressionAnalysis(
            SerializedByteCount: serialized.Length,
            InputByteCount: serialized.Length,
            CompressedByteCount: compressed.Length,
            CompressedByteSum: byteSum,
            CompressedBytes: compressed);
    }
}
