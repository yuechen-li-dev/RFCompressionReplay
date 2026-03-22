using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Detectors;

[Obsolete("M0 placeholder detector retained only for historical reference; M1 uses DetectorFactory-backed real detectors.")]
public sealed class PlaceholderDetector : IDetector
{
    public string Name => "placeholder-detector";

    public DetectorResult Evaluate(DetectorInput input, DetectorConfig config)
    {
        var allSamples = input.Windows.SelectMany(window => window.Samples).ToArray();
        var mean = allSamples.Length == 0 ? 0d : allSamples.Average();
        var peak = allSamples.Length == 0 ? 0d : allSamples.Max();
        var score = DetectorMath.RoundScore(mean + (peak * 0.05d) + (input.TrialIndex * 0.01d));
        var metrics = new Dictionary<string, double>
        {
            ["mean"] = DetectorMath.RoundScore(mean),
            ["peak"] = DetectorMath.RoundScore(peak),
            ["windowCount"] = input.Windows.Count,
        };

        return new DetectorResult(config.Name, config.Mode, score, score >= config.Threshold, metrics);
    }
}
