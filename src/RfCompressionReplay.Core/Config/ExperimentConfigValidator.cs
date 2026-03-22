using RfCompressionReplay.Core.Detectors;

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
            errors.Add($"Scenario.Name '{config.Scenario.Name}' is not supported in M2. Supported scenarios: {DummyScenarioName}, {SyntheticBenchmarkScenarioName}.");
        }

        if (config.Scenario.SampleWindowCount <= 0)
        {
            errors.Add("Scenario.SampleWindowCount must be greater than zero.");
        }

        if (config.Scenario.SamplesPerWindow <= 0)
        {
            errors.Add("Scenario.SamplesPerWindow must be greater than zero.");
        }

        ValidateDetector(config, errors);

        if (string.Equals(config.Scenario.Name, DummyScenarioName, StringComparison.OrdinalIgnoreCase))
        {
            ValidateDummySignal(config, errors);
        }
        else if (string.Equals(config.Scenario.Name, SyntheticBenchmarkScenarioName, StringComparison.OrdinalIgnoreCase))
        {
            ValidateBenchmark(config, errors);
        }

        if (config.ManifestMetadata is null)
        {
            errors.Add("ManifestMetadata is required.");
        }

        return errors;
    }

    private static void ValidateDetector(ExperimentConfig config, List<string> errors)
    {
        if (config.Detector is null)
        {
            errors.Add("Detector is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(config.Detector.Name))
        {
            errors.Add("Detector.Name is required.");
        }
        else if (!DetectorCatalog.IsSupportedDetector(config.Detector.Name))
        {
            errors.Add($"Detector.Name '{config.Detector.Name}' is not supported in M2. Supported detectors: {DetectorCatalog.SupportedDetectorsDisplay}.");
        }

        if (string.IsNullOrWhiteSpace(config.Detector.Mode))
        {
            errors.Add("Detector.Mode is required.");
        }
        else if (!string.IsNullOrWhiteSpace(config.Detector.Name) && DetectorCatalog.IsSupportedDetector(config.Detector.Name) && !DetectorCatalog.IsSupportedMode(config.Detector.Name, config.Detector.Mode))
        {
            errors.Add($"Detector.Mode '{config.Detector.Mode}' is not supported for detector '{config.Detector.Name}' in M2. Supported modes: {DetectorCatalog.SupportedModesDisplay(config.Detector.Name)}.");
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
            errors.Add($"Signal.Name '{config.Signal.Name}' is not supported in M2 for the dummy scenario. Supported signals: {DummySignalName}.");
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

        var samplesPerTrial = checked(config.Scenario.SampleWindowCount * config.Scenario.SamplesPerWindow);
        if (config.Benchmark.BaseStreamLength < samplesPerTrial)
        {
            errors.Add("Benchmark.BaseStreamLength must be at least Scenario.SampleWindowCount * Scenario.SamplesPerWindow.");
        }

        if (config.Benchmark.Noise is null)
        {
            errors.Add("Benchmark.Noise is required.");
        }
        else if (config.Benchmark.Noise.StandardDeviation <= 0d)
        {
            errors.Add("Benchmark.Noise.StandardDeviation must be greater than zero.");
        }

        if (config.Benchmark.Cases is null || config.Benchmark.Cases.Count == 0)
        {
            errors.Add("Benchmark.Cases must contain at least one synthetic case.");
            return;
        }

        for (var caseIndex = 0; caseIndex < config.Benchmark.Cases.Count; caseIndex++)
        {
            var @case = config.Benchmark.Cases[caseIndex];
            var prefix = $"Benchmark.Cases[{caseIndex}]";

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
                continue;
            }

            if (!IsSupportedSourceType(@case.SourceType))
            {
                errors.Add($"{prefix}.SourceType '{@case.SourceType}' is not supported in M2. Supported source types: {NoiseOnlySourceType}, {GaussianEmitterSourceType}, {OfdmLikeSourceType}.");
                continue;
            }

            if (string.Equals(@case.SourceType, NoiseOnlySourceType, StringComparison.OrdinalIgnoreCase))
            {
                if (@case.SnrDb is not null)
                {
                    errors.Add($"{prefix}.SnrDb must be null for the noise-only source type.");
                }
            }
            else
            {
                if (@case.SnrDb is null)
                {
                    errors.Add($"{prefix}.SnrDb is required for source type '{@case.SourceType}'.");
                }
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
    }

    private static bool IsSupportedSourceType(string sourceType)
    {
        return string.Equals(sourceType, NoiseOnlySourceType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceType, GaussianEmitterSourceType, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceType, OfdmLikeSourceType, StringComparison.OrdinalIgnoreCase);
    }
}
