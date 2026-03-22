using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Detectors;

public sealed class CovarianceAbsoluteValueDetector : IDetector
{
    public string Name => DetectorCatalog.CovarianceAbsoluteValueDetectorName;

    public DetectorResult Evaluate(DetectorInput input, DetectorConfig config)
    {
        var allSamples = input.Windows.SelectMany(window => window.Samples).ToArray();
        if (allSamples.Length < 2)
        {
            return new DetectorResult(config.Name, config.Mode, 0d, DetectorCatalog.IsPositiveAtThreshold(config.Name, 0d, config.Threshold), new Dictionary<string, double>
            {
                ["lag1AbsoluteAutocovariance"] = 0d,
            });
        }

        var mean = allSamples.Average();
        var lag1AbsoluteAutocovariance = Math.Abs(
            Enumerable.Range(1, allSamples.Length - 1)
                .Average(index => (allSamples[index] - mean) * (allSamples[index - 1] - mean)));

        var score = DetectorMath.RoundScore(lag1AbsoluteAutocovariance);
        var metrics = new Dictionary<string, double>
        {
            ["lag1AbsoluteAutocovariance"] = score,
        };

        return new DetectorResult(config.Name, config.Mode, score, DetectorCatalog.IsPositiveAtThreshold(config.Name, score, config.Threshold), metrics);
    }
}
