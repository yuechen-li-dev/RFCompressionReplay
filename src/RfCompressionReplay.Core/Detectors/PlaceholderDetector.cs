using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Detectors;

public sealed class PlaceholderDetector : IDetector
{
    public string Name => "placeholder-detector";

    public DetectorResult Evaluate(DetectorInput input, DetectorConfig config)
    {
        var allSamples = input.Windows.SelectMany(window => window.Samples).ToArray();
        var mean = allSamples.Length == 0 ? 0d : allSamples.Average();
        var peak = allSamples.Length == 0 ? 0d : allSamples.Max();
        var score = Math.Round(mean + (peak * 0.05d) + (input.TrialIndex * 0.01d), 6, MidpointRounding.AwayFromZero);
        var metrics = new Dictionary<string, double>
        {
            ["mean"] = Math.Round(mean, 6, MidpointRounding.AwayFromZero),
            ["peak"] = Math.Round(peak, 6, MidpointRounding.AwayFromZero),
            ["windowCount"] = input.Windows.Count,
        };

        return new DetectorResult(config.Name, score, score >= config.Threshold, metrics);
    }
}
