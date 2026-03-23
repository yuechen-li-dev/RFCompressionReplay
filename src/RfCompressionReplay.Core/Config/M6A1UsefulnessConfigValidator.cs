using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;

namespace RfCompressionReplay.Core.Config;

public static class M6A1UsefulnessConfigValidator
{
    private static readonly string[] RequiredTaskPanel =
    [
        BenchmarkTaskCatalog.StructuredBurstVsNoiseOnly,
        BenchmarkTaskCatalog.ColoredNuisanceVsWhiteNoise,
        BenchmarkTaskCatalog.EqualEnergyStructuredVsUnstructured,
    ];

    private static readonly string[] RequiredDetectorPanel =
    [
        DetectorCatalog.EnergyDetectorName,
        DetectorCatalog.CovarianceAbsoluteValueDetectorName,
        DetectorCatalog.LzmsaPaperDetectorName,
        DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
    ];

    public static IReadOnlyList<string> Validate(M6A1UsefulnessConfig config)
    {
        var errors = new List<string>();

        if (config.SeedPanel is null || config.SeedPanel.Count == 0)
        {
            errors.Add("SeedPanel must contain at least one seed.");
            return errors;
        }

        if (config.SeedPanel.Count < 3)
        {
            errors.Add("SeedPanel must contain at least three explicit seeds for M6a1 usefulness mapping.");
        }

        if (config.SeedPanel.Any(seed => seed < 0))
        {
            errors.Add("SeedPanel seeds must be zero or greater.");
        }

        if (config.SeedPanel.Distinct().Count() != config.SeedPanel.Count)
        {
            errors.Add("SeedPanel seeds must be distinct.");
        }

        var baseValidationErrors = ExperimentConfigValidator.Validate(config.ToSeededExperimentConfig(config.SeedPanel[0]));
        errors.AddRange(baseValidationErrors);

        var taskNames = config.Evaluation.Tasks.Select(task => task.Name).ToArray();
        if (!taskNames.SequenceEqual(RequiredTaskPanel, StringComparer.Ordinal))
        {
            errors.Add($"M6a1 usefulness mapping requires the exact three-task suite in order: {string.Join(", ", RequiredTaskPanel)}.");
        }

        var detectorNames = config.Evaluation.Detectors.Select(detector => detector.Name).ToArray();
        if (!detectorNames.SequenceEqual(RequiredDetectorPanel, StringComparer.Ordinal))
        {
            errors.Add($"M6a1 usefulness mapping requires the focused detector panel exactly: {string.Join(", ", RequiredDetectorPanel)}.");
        }

        if (!config.Evaluation.SnrDbValues.SequenceEqual([-9d, -3d, 0d]))
        {
            errors.Add("M6a1 usefulness mapping expects the readable default SNR panel exactly: -9, -3, 0 dB.");
        }

        if (!config.Evaluation.WindowLengths.SequenceEqual([64, 128]))
        {
            errors.Add("M6a1 usefulness mapping expects the readable default window-length panel exactly: 64, 128.");
        }

        return errors;
    }
}
