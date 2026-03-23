using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;

namespace RfCompressionReplay.Core.Config;

public static class ExperimentConfigValidator
{
    public const string DummyScenarioName = "dummy";
    public const string SyntheticBenchmarkScenarioName = "synthetic-benchmark";

    public const string DummySignalName = "dummy-signal";
    public const string NoiseOnlySourceType = "noise-only";
    public const string GaussianEmitterSourceType = "gaussian-emitter";
    public const string OfdmLikeSourceType = "ofdm-like";

    public static IReadOnlyList<string> Validate(ExperimentConfig config)
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

        if (config.Seed < 0)
        {
            errors.Add("Seed must be zero or greater.");
        }

        if (string.IsNullOrWhiteSpace(config.OutputDirectory))
        {
            errors.Add("OutputDirectory is required.");
        }

        if (!ArtifactRetentionModes.IsSupported(config.ArtifactRetentionMode))
        {
            errors.Add($"ArtifactRetentionMode '{config.ArtifactRetentionMode}' is not supported. Supported modes: {ArtifactRetentionModes.SupportedModesDisplay}.");
        }

        if (config.TrialCount <= 0)
        {
            errors.Add("TrialCount must be greater than zero.");
        }

        if (config.Scenario is null)
        {
            errors.Add("Scenario is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(config.Scenario.Name))
        {
            errors.Add("Scenario.Name is required.");
        }
        else if (!string.Equals(config.Scenario.Name, DummyScenarioName, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(config.Scenario.Name, SyntheticBenchmarkScenarioName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Scenario.Name '{config.Scenario.Name}' is not supported in M3. Supported scenarios: {DummyScenarioName}, {SyntheticBenchmarkScenarioName}.");
        }

        if (config.Scenario.SampleWindowCount <= 0)
        {
            errors.Add("Scenario.SampleWindowCount must be greater than zero.");
        }

        if (config.Scenario.SamplesPerWindow <= 0)
        {
            errors.Add("Scenario.SamplesPerWindow must be greater than zero.");
        }

        ValidateDetector(config.Detector, "Detector", errors);

        if (string.Equals(config.Scenario.Name, DummyScenarioName, StringComparison.OrdinalIgnoreCase))
        {
            ValidateDummySignal(config, errors);
        }
        else if (string.Equals(config.Scenario.Name, SyntheticBenchmarkScenarioName, StringComparison.OrdinalIgnoreCase))
        {
            ValidateBenchmark(config, errors);
            ValidateEvaluation(config, errors);
        }

        if (config.ManifestMetadata is null)
        {
            errors.Add("ManifestMetadata is required.");
        }

        ValidateRepresentation(config.Representation, "Representation", errors);

        return errors;
    }

    private static void ValidateDetector(DetectorConfig? detector, string prefix, List<string> errors)
    {
        if (detector is null)
        {
            errors.Add($"{prefix} is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(detector.Name))
        {
            errors.Add($"{prefix}.Name is required.");
        }
        else if (!DetectorCatalog.IsSupportedDetector(detector.Name))
        {
            errors.Add($"{prefix}.Name '{detector.Name}' is not supported in M3. Supported detectors: {DetectorCatalog.SupportedDetectorsDisplay}.");
        }

        if (string.IsNullOrWhiteSpace(detector.Mode))
        {
            errors.Add($"{prefix}.Mode is required.");
        }
        else if (!string.IsNullOrWhiteSpace(detector.Name)
            && DetectorCatalog.IsSupportedDetector(detector.Name)
            && !DetectorCatalog.IsSupportedMode(detector.Name, detector.Mode))
        {
            errors.Add($"{prefix}.Mode '{detector.Mode}' is not supported for detector '{detector.Name}' in M3. Supported modes: {DetectorCatalog.SupportedModesDisplay(detector.Name)}.");
        }

        if (double.IsNaN(detector.Threshold) || double.IsInfinity(detector.Threshold))
        {
            errors.Add($"{prefix}.Threshold must be a finite number.");
        }

        if (string.Equals(detector.Name, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase)
            && (detector.Threshold < 0d || detector.Threshold > byte.MaxValue))
        {
            errors.Add($"{prefix}.Threshold for detector '{detector.Name}' must be between 0 and 255 inclusive because the mean compressed byte value is bounded to that range.");
        }
    }

    private static void ValidateRepresentation(RepresentationConfig? representation, string prefix, List<string> errors)
    {
        if (representation is null)
        {
            return;
        }

        if (double.IsNaN(representation.SampleScale) || double.IsInfinity(representation.SampleScale))
        {
            errors.Add($"{prefix}.SampleScale must be a finite number.");
        }
        else if (representation.SampleScale <= 0d)
        {
            errors.Add($"{prefix}.SampleScale must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(representation.NumericFormat))
        {
            errors.Add($"{prefix}.NumericFormat is required.");
        }
        else if (!RepresentationFormats.IsSupported(representation.NumericFormat))
        {
            errors.Add($"{prefix}.NumericFormat '{representation.NumericFormat}' is not supported. Supported formats: {RepresentationFormats.SupportedFormatsDisplay}.");
        }
    }

    private static void ValidateDummySignal(ExperimentConfig config, List<string> errors)
    {
        if (config.Signal is null)
        {
            errors.Add("Signal is required for the dummy scenario.");
            return;
        }

        if (string.IsNullOrWhiteSpace(config.Signal.Name))
        {
            errors.Add("Signal.Name is required.");
        }
        else if (!string.Equals(config.Signal.Name, DummySignalName, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Signal.Name '{config.Signal.Name}' is not supported in M3 for the dummy scenario. Supported signals: {DummySignalName}.");
        }
    }

    private static void ValidateBenchmark(ExperimentConfig config, List<string> errors)
    {
        if (config.Benchmark is null)
        {
            errors.Add("Benchmark is required for the synthetic-benchmark scenario.");
            return;
        }

        if (config.Benchmark.BaseStreamLength <= 0)
        {
            errors.Add("Benchmark.BaseStreamLength must be greater than zero.");
        }

        var minimumSamplesPerWindow = GetMinimumSamplesPerWindow(config);
        var samplesPerTrial = checked(config.Scenario.SampleWindowCount * minimumSamplesPerWindow);
        if (config.Benchmark.BaseStreamLength < samplesPerTrial)
        {
            errors.Add("Benchmark.BaseStreamLength must be at least Scenario.SampleWindowCount * min(windowLength).");
        }

        if (config.Benchmark.Noise is null)
        {
            errors.Add("Benchmark.Noise is required.");
        }
        else if (config.Benchmark.Noise.StandardDeviation <= 0d)
        {
            errors.Add("Benchmark.Noise.StandardDeviation must be greater than zero.");
        }

        if (config.Evaluation is null)
        {
            if (config.Benchmark.Cases is null || config.Benchmark.Cases.Count == 0)
            {
                errors.Add("Benchmark.Cases must contain at least one synthetic case.");
                return;
            }

            for (var caseIndex = 0; caseIndex < config.Benchmark.Cases.Count; caseIndex++)
            {
                ValidateSyntheticCase(config.Benchmark.Cases[caseIndex], $"Benchmark.Cases[{caseIndex}]", errors);
            }

            return;
        }

        foreach (var task in config.Evaluation.Tasks.Select((value, index) => (value, index)))
        {
            ValidateSyntheticCase(task.value.PositiveCase, $"Evaluation.Tasks[{task.index}].PositiveCase", errors);
            ValidateSyntheticCase(task.value.NegativeCase, $"Evaluation.Tasks[{task.index}].NegativeCase", errors);
        }
    }

    private static void ValidateEvaluation(ExperimentConfig config, List<string> errors)
    {
        if (config.Evaluation is null)
        {
            return;
        }

        if (config.Evaluation.Tasks is null || config.Evaluation.Tasks.Count == 0)
        {
            errors.Add("Evaluation.Tasks must contain at least one benchmark task.");
        }
        else
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var taskIndex = 0; taskIndex < config.Evaluation.Tasks.Count; taskIndex++)
            {
                var task = config.Evaluation.Tasks[taskIndex];
                var prefix = $"Evaluation.Tasks[{taskIndex}]";

                if (string.IsNullOrWhiteSpace(task.Name))
                {
                    errors.Add($"{prefix}.Name is required.");
                }
                else if (!BenchmarkTaskCatalog.IsSupportedTask(task.Name))
                {
                    errors.Add($"{prefix}.Name '{task.Name}' is not supported in M3. Supported tasks: {BenchmarkTaskCatalog.SupportedTasksDisplay}.");
                }
                else if (!names.Add(task.Name))
                {
                    errors.Add($"{prefix}.Name '{task.Name}' is duplicated.");
                }

                if (string.IsNullOrWhiteSpace(task.Description))
                {
                    errors.Add($"{prefix}.Description is required.");
                }
            }
        }

        if (config.Evaluation.Detectors is null || config.Evaluation.Detectors.Count == 0)
        {
            errors.Add("Evaluation.Detectors must contain at least one detector.");
        }
        else
        {
            for (var detectorIndex = 0; detectorIndex < config.Evaluation.Detectors.Count; detectorIndex++)
            {
                ValidateDetector(config.Evaluation.Detectors[detectorIndex], $"Evaluation.Detectors[{detectorIndex}]", errors);
            }
        }

        if (config.Evaluation.SnrDbValues is null || config.Evaluation.SnrDbValues.Count == 0)
        {
            errors.Add("Evaluation.SnrDbValues must contain at least one SNR value.");
        }

        if (config.Evaluation.WindowLengths is null || config.Evaluation.WindowLengths.Count == 0)
        {
            errors.Add("Evaluation.WindowLengths must contain at least one window length.");
        }
        else
        {
            for (var index = 0; index < config.Evaluation.WindowLengths.Count; index++)
            {
                if (config.Evaluation.WindowLengths[index] <= 0)
                {
                    errors.Add($"Evaluation.WindowLengths[{index}] must be greater than zero.");
                }
            }
        }

        if (config.Evaluation.TrialCountPerCondition <= 0)
        {
            errors.Add("Evaluation.TrialCountPerCondition must be greater than zero.");
        }
    }

    private static int GetMinimumSamplesPerWindow(ExperimentConfig config)
    {
        if (config.Evaluation?.WindowLengths is { Count: > 0 })
        {
            return config.Evaluation.WindowLengths.Min();
        }

        return config.Scenario.SamplesPerWindow;
    }

    private static void ValidateSyntheticCase(SyntheticCaseConfig @case, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(@case.Name))
        {
            errors.Add($"{prefix}.Name is required.");
        }

        if (string.IsNullOrWhiteSpace(@case.TargetLabel))
        {
            errors.Add($"{prefix}.TargetLabel is required.");
        }

        if (string.IsNullOrWhiteSpace(@case.SourceType))
        {
            errors.Add($"{prefix}.SourceType is required.");
            return;
        }

        if (!IsSupportedSourceType(@case.SourceType))
        {
            errors.Add($"{prefix}.SourceType '{@case.SourceType}' is not supported in M3. Supported source types: {NoiseOnlySourceType}, {GaussianEmitterSourceType}, {OfdmLikeSourceType}.");
            return;
        }

        if (string.Equals(@case.SourceType, NoiseOnlySourceType, StringComparison.OrdinalIgnoreCase))
        {
            if (@case.SnrDb is not null)
            {
                errors.Add($"{prefix}.SnrDb must be null for the noise-only source type.");
            }
        }
        else if (@case.SnrDb is null)
        {
            errors.Add($"{prefix}.SnrDb is required for source type '{@case.SourceType}'.");
        }

        if (string.Equals(@case.SourceType, GaussianEmitterSourceType, StringComparison.OrdinalIgnoreCase))
        {
            if (@case.GaussianEmitter is null)
            {
                errors.Add($"{prefix}.GaussianEmitter is required for source type '{GaussianEmitterSourceType}'.");
            }
            else if (@case.GaussianEmitter.StandardDeviation <= 0d)
            {
                errors.Add($"{prefix}.GaussianEmitter.StandardDeviation must be greater than zero.");
            }
        }

        if (string.Equals(@case.SourceType, OfdmLikeSourceType, StringComparison.OrdinalIgnoreCase))
        {
            if (@case.OfdmLike is null)
            {
                errors.Add($"{prefix}.OfdmLike is required for source type '{OfdmLikeSourceType}'.");
            }
            else
            {
                if (@case.OfdmLike.SubcarrierCount <= 0)
                {
                    errors.Add($"{prefix}.OfdmLike.SubcarrierCount must be greater than zero.");
                }

                if (@case.OfdmLike.SamplesPerSymbol <= 1)
                {
                    errors.Add($"{prefix}.OfdmLike.SamplesPerSymbol must be greater than one.");
                }

                if (@case.OfdmLike.CarrierSpacing <= 0d)
                {
                    errors.Add($"{prefix}.OfdmLike.CarrierSpacing must be greater than zero.");
                }

                if (@case.OfdmLike.Amplitude <= 0d)
                {
                    errors.Add($"{prefix}.OfdmLike.Amplitude must be greater than zero.");
                }
            }
        }
    }

    private static bool IsSupportedSourceType(string sourceType)
    {
        return string.Equals(sourceType, NoiseOnlySourceType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceType, GaussianEmitterSourceType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceType, OfdmLikeSourceType, StringComparison.OrdinalIgnoreCase);
    }
}
