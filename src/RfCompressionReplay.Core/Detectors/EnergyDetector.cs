using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Detectors;

public sealed class EnergyDetector : IDetector
{
    public string Name => DetectorCatalog.EnergyDetectorName;

    public DetectorResult Evaluate(DetectorInput input, DetectorConfig config)
    {
        var allSamples = input.Windows.SelectMany(window => window.Samples).ToArray();
        var averageEnergy = allSamples.Length == 0
            ? 0d
            : allSamples.Select(sample => sample * sample).Average();

        var score = DetectorMath.RoundScore(averageEnergy);
        var metrics = new Dictionary<string, double>
        {
            ["averageEnergy"] = score,
        };

        return new DetectorResult(config.Name, config.Mode, score, DetectorCatalog.IsPositiveAtThreshold(config.Name, score, config.Threshold), metrics);
    }
}
