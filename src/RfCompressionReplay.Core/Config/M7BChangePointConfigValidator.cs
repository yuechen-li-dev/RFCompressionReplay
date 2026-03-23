using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;

namespace RfCompressionReplay.Core.Config;

public static class M7BChangePointConfigValidator
{
    private static readonly string[] RequiredTaskPanel =
    [
        BenchmarkTaskCatalog.QuietToStructuredRegime,
        BenchmarkTaskCatalog.CorrelatedNuisanceToEngineeredStructure,
        BenchmarkTaskCatalog.StructureToStructureRegimeShift,
    ];

    private static readonly string[] RequiredDetectorPanel =
    [
        DetectorCatalog.EnergyDetectorName,
        DetectorCatalog.CovarianceAbsoluteValueDetectorName,
        DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
    ];

    public static IReadOnlyList<string> Validate(M7BChangePointConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.ExperimentId))
        {
            errors.Add("ExperimentId is required.");
        }

        if (string.IsNullOrWhiteSpace(config.ExperimentName))
        {
            errors.Add("ExperimentName is required.");
        }

        if (string.IsNullOrWhiteSpace(config.OutputDirectory))
        {
            errors.Add("OutputDirectory is required.");
        }

        if (!ArtifactRetentionModes.IsSupported(config.ArtifactRetentionMode))
        {
            errors.Add($"ArtifactRetentionMode '{config.ArtifactRetentionMode}' is not supported. Supported modes: {ArtifactRetentionModes.SupportedModesDisplay}.");
        }

        if (config.SeedPanel is null || config.SeedPanel.Count == 0)
        {
            errors.Add("SeedPanel must contain at least one seed.");
            return errors;
        }

        if (config.SeedPanel.Count < 3)
        {
            errors.Add("SeedPanel must contain at least three explicit seeds for M7b change-point usefulness mapping.");
        }

        if (config.SeedPanel.Any(seed => seed < 0))
        {
            errors.Add("SeedPanel seeds must be zero or greater.");
        }

        if (config.SeedPanel.Distinct().Count() != config.SeedPanel.Count)
        {
            errors.Add("SeedPanel seeds must be distinct.");
        }

        if (!string.Equals(config.Scenario.Name, ExperimentConfigValidator.SyntheticBenchmarkScenarioName, StringComparison.Ordinal))
        {
            errors.Add($"Scenario.Name must be '{ExperimentConfigValidator.SyntheticBenchmarkScenarioName}' for M7b.");
        }

        if (config.Benchmark.Tasks is null || config.Benchmark.Tasks.Count == 0)
        {
            errors.Add("Benchmark.Tasks must contain the three-task M7b stream suite.");
            return errors;
        }

        var taskNames = config.Benchmark.Tasks.Select(task => task.Name).ToArray();
        if (!taskNames.SequenceEqual(RequiredTaskPanel, StringComparer.Ordinal))
        {
            errors.Add($"M7b change-point usefulness mapping requires the exact three-task stream suite in order: {string.Join(", ", RequiredTaskPanel)}.");
        }

        var detectorNames = config.Evaluation.Detectors.Select(detector => detector.Name).ToArray();
        if (!detectorNames.SequenceEqual(RequiredDetectorPanel, StringComparer.Ordinal))
        {
            errors.Add($"M7b change-point usefulness mapping requires the focused detector panel exactly: {string.Join(", ", RequiredDetectorPanel)}.");
        }

        if (!config.Evaluation.SnrDbValues.SequenceEqual([-9d, -3d, 0d]))
        {
            errors.Add("M7b change-point usefulness mapping expects the readable default SNR panel exactly: -9, -3, 0 dB.");
        }

        if (!config.Evaluation.WindowLengths.SequenceEqual([64, 128]))
        {
            errors.Add("M7b change-point usefulness mapping expects the readable default window-length panel exactly: 64, 128.");
        }

        if (config.Evaluation.StreamCountPerCondition <= 0)
        {
            errors.Add("Evaluation.StreamCountPerCondition must be greater than zero.");
        }

        if (config.Evaluation.MaxBoundaryProposals <= 0)
        {
            errors.Add("Evaluation.MaxBoundaryProposals must be greater than zero.");
        }

        if (config.Evaluation.WindowStrideFraction <= 0d || config.Evaluation.WindowStrideFraction > 1d)
        {
            errors.Add("Evaluation.WindowStrideFraction must be greater than zero and at most 1.0.");
        }

        if (config.Evaluation.BoundaryToleranceWindowMultiple <= 0d)
        {
            errors.Add("Evaluation.BoundaryToleranceWindowMultiple must be greater than zero.");
        }

        if (config.Evaluation.MinPeakSpacingWindowMultiple <= 0d)
        {
            errors.Add("Evaluation.MinPeakSpacingWindowMultiple must be greater than zero.");
        }

        if (config.Evaluation.PeakThresholdMadMultiplier < 0d)
        {
            errors.Add("Evaluation.PeakThresholdMadMultiplier must be zero or greater.");
        }

        if (config.Benchmark.Noise.StandardDeviation <= 0d)
        {
            errors.Add("Benchmark.Noise.StandardDeviation must be greater than zero.");
        }

        foreach (var task in config.Benchmark.Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Description))
            {
                errors.Add($"Task '{task.Name}' must provide a non-empty Description.");
            }

            if (task.Regimes is null || task.Regimes.Count < 2)
            {
                errors.Add($"Task '{task.Name}' must contain at least two regimes.");
                continue;
            }

            foreach (var regime in task.Regimes)
            {
                if (regime.LengthSamples <= 0)
                {
                    errors.Add($"Task '{task.Name}' regime '{regime.Id}' must have LengthSamples > 0.");
                }

                ValidateSyntheticCase(errors, task.Name, regime);
            }
        }

        return errors;
    }

    private static void ValidateSyntheticCase(List<string> errors, string taskName, M7BRegimeConfig regime)
    {
        var syntheticCase = regime.SyntheticCase;
        if (!IsSupportedSourceType(syntheticCase.SourceType))
        {
            errors.Add($"Task '{taskName}' regime '{regime.Id}' uses unsupported source type '{syntheticCase.SourceType}'. Supported source types: {ExperimentConfigValidator.NoiseOnlySourceType}, {ExperimentConfigValidator.GaussianEmitterSourceType}, {ExperimentConfigValidator.OfdmLikeSourceType}, {ExperimentConfigValidator.BurstOfdmLikeSourceType}, {ExperimentConfigValidator.CorrelatedGaussianSourceType}.");
            return;
        }

        if (string.Equals(syntheticCase.SourceType, ExperimentConfigValidator.NoiseOnlySourceType, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!regime.ApplyConditionSnr && syntheticCase.SnrDb is null)
        {
            errors.Add($"Task '{taskName}' regime '{regime.Id}' must provide an intrinsic SnrDb when ApplyConditionSnr is false.");
        }
    }

    private static bool IsSupportedSourceType(string sourceType)
    {
        return string.Equals(sourceType, ExperimentConfigValidator.NoiseOnlySourceType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceType, ExperimentConfigValidator.GaussianEmitterSourceType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceType, ExperimentConfigValidator.OfdmLikeSourceType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceType, ExperimentConfigValidator.BurstOfdmLikeSourceType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceType, ExperimentConfigValidator.CorrelatedGaussianSourceType, StringComparison.OrdinalIgnoreCase);
    }
}
