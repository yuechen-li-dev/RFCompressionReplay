using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;

namespace RfCompressionReplay.Tests;

internal static class TestConfigFactory
{
    public static ExperimentConfig CreateSyntheticBenchmarkConfig(
        string experimentId,
        string detectorName,
        string detectorMode,
        double threshold = 0d,
        IReadOnlyList<SyntheticCaseConfig>? cases = null)
    {
        return new ExperimentConfig(
            ExperimentId: experimentId,
            ExperimentName: "M2 Synthetic Test",
            Seed: 7,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 64),
            TrialCount: 4,
            Detector: new DetectorConfig(detectorName, threshold, detectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 2048,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: cases ??
                [
                    new SyntheticCaseConfig(
                        Name: "noise-only-baseline",
                        TargetLabel: "noise-only",
                        SourceType: ExperimentConfigValidator.NoiseOnlySourceType,
                        SnrDb: null,
                        GaussianEmitter: null,
                        OfdmLike: null)
                ]),
            Evaluation: null,
            ManifestMetadata: new ManifestMetadataConfig("note", "m2", new Dictionary<string, string> { ["suite"] = "tests" }));
    }

    public static ExperimentConfig CreateSyntheticEvaluationConfig(
        string experimentId,
        IReadOnlyList<BenchmarkTaskConfig>? tasks = null,
        IReadOnlyList<DetectorConfig>? detectors = null,
        IReadOnlyList<double>? snrDbValues = null,
        IReadOnlyList<int>? windowLengths = null,
        int trialCountPerCondition = 3)
    {
        return new ExperimentConfig(
            ExperimentId: experimentId,
            ExperimentName: "M3 Synthetic Evaluation Test",
            Seed: 7,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 64),
            TrialCount: 4,
            Detector: new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 2048,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: Array.Empty<SyntheticCaseConfig>()),
            Evaluation: new EvaluationConfig(
                Tasks: tasks ?? [CreateOfdmTask()],
                Detectors: detectors ?? [new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode)],
                SnrDbValues: snrDbValues ?? [-6d, 0d],
                WindowLengths: windowLengths ?? [64, 128],
                TrialCountPerCondition: trialCountPerCondition),
            ManifestMetadata: new ManifestMetadataConfig("note", "m3", new Dictionary<string, string> { ["suite"] = "tests" }));
    }

    public static IReadOnlyList<DetectorConfig> CreateM4CompressionDetectors()
    {
        return
        [
            new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedLengthDetectorName, 64d, DetectorCatalog.LzmsaCompressedLengthDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, 0.25d, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode),
        ];
    }

    public static IReadOnlyList<DetectorConfig> CreateM5A1CompressionDetectors()
    {
        return
        [
            new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedLengthDetectorName, 64d, DetectorCatalog.LzmsaCompressedLengthDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, 0.25d, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, 128d, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode),
        ];
    }

    public static BenchmarkTaskConfig CreateOfdmTask()
    {
        return new BenchmarkTaskConfig(
            Name: BenchmarkTaskCatalog.OfdmSignalPresentVsNoiseOnly,
            Description: "Positive class is an OFDM-like synthetic signal mixed into Gaussian background noise. Negative class is Gaussian noise only.",
            PositiveCase: CreateOfdmLikeCase(-3d),
            NegativeCase: CreateNoiseOnlyCase());
    }

    public static BenchmarkTaskConfig CreateGaussianEmitterTask()
    {
        return new BenchmarkTaskConfig(
            Name: BenchmarkTaskCatalog.GaussianEmitterVsNoiseOnly,
            Description: "Positive class is an independent Gaussian emitter mixed into Gaussian background noise. Negative class is Gaussian noise only.",
            PositiveCase: CreateGaussianEmitterCase(0d),
            NegativeCase: CreateNoiseOnlyCase());
    }

    public static SyntheticCaseConfig CreateNoiseOnlyCase()
    {
        return new SyntheticCaseConfig(
            Name: "noise-only-baseline",
            TargetLabel: "noise-only",
            SourceType: ExperimentConfigValidator.NoiseOnlySourceType,
            SnrDb: null,
            GaussianEmitter: null,
            OfdmLike: null);
    }

    public static SyntheticCaseConfig CreateGaussianEmitterCase(double snrDb = 0d)
    {
        return new SyntheticCaseConfig(
            Name: "gaussian-emitter-control",
            TargetLabel: "signal-present",
            SourceType: ExperimentConfigValidator.GaussianEmitterSourceType,
            SnrDb: snrDb,
            GaussianEmitter: new GaussianEmitterConfig(0d, 1d),
            OfdmLike: null);
    }

    public static SyntheticCaseConfig CreateOfdmLikeCase(double snrDb = -3d, int symbolSeed = 77)
    {
        return new SyntheticCaseConfig(
            Name: "ofdm-like-signal",
            TargetLabel: "signal-present",
            SourceType: ExperimentConfigValidator.OfdmLikeSourceType,
            SnrDb: snrDb,
            GaussianEmitter: null,
            OfdmLike: new OfdmLikeSignalConfig(8, 32, symbolSeed, 0.75d, 1d));
    }
}
