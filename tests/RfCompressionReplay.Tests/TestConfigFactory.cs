using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;

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
            ManifestMetadata: new ManifestMetadataConfig("note", "m2", new Dictionary<string, string> { ["suite"] = "tests" }));
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
