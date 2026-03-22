using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;

namespace RfCompressionReplay.Tests;

public sealed class ExperimentConfigTests
{
    [Fact]
    public void DeserializesM2SampleConfig()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs/m2.gaussian-emitter.ed.json"));
        var config = ExperimentConfigJson.Load(path);

        Assert.Equal("m2-gaussian-emitter-ed", config.ExperimentId);
        Assert.Equal(12345, config.Seed);
        Assert.Equal(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, config.Scenario.Name);
        Assert.Equal(6, config.TrialCount);
        Assert.Equal(DetectorCatalog.EnergyDetectorName, config.Detector.Name);
        Assert.Equal(DetectorCatalog.EnergyDetectorMode, config.Detector.Mode);
        Assert.NotNull(config.Benchmark);
        Assert.Single(config.Benchmark!.Cases);
    }

    [Fact]
    public void ValidatorReturnsClearMessagesForInvalidSyntheticBenchmarkConfig()
    {
        var config = new ExperimentConfig(
            ExperimentId: "",
            ExperimentName: "",
            Seed: -1,
            OutputDirectory: "",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 0, 0),
            TrialCount: 0,
            Detector: new DetectorConfig("", 0.5, ""),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(0, new GaussianNoiseConfig(0d, 0d), Array.Empty<SyntheticCaseConfig>()),
            ManifestMetadata: ManifestMetadataConfig.Empty);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("ExperimentId is required.", errors);
        Assert.Contains("Seed must be zero or greater.", errors);
        Assert.Contains("TrialCount must be greater than zero.", errors);
        Assert.Contains("Scenario.SampleWindowCount must be greater than zero.", errors);
        Assert.Contains("Detector.Mode is required.", errors);
        Assert.Contains("Benchmark.BaseStreamLength must be greater than zero.", errors);
        Assert.Contains("Benchmark.Noise.StandardDeviation must be greater than zero.", errors);
        Assert.Contains("Benchmark.Cases must contain at least one synthetic case.", errors);
    }

    [Fact]
    public void ValidatorRejectsUnsupportedDetectorAndSourceIdentifiers()
    {
        var config = new ExperimentConfig(
            ExperimentId: "bad-identifiers",
            ExperimentName: "Bad identifiers",
            Seed: 1,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 1, 8),
            TrialCount: 1,
            Detector: new DetectorConfig("bogus-detector", 0.5, "bogus-mode"),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                64,
                new GaussianNoiseConfig(0d, 1d),
                [new SyntheticCaseConfig("bad-case", "signal-present", "bogus-source", 0d, null, null)]),
            ManifestMetadata: ManifestMetadataConfig.Empty);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("Detector.Name 'bogus-detector' is not supported in M2. Supported detectors: ed, cav, lzmsa-paper.", errors);
        Assert.Contains("Benchmark.Cases[0].SourceType 'bogus-source' is not supported in M2. Supported source types: noise-only, gaussian-emitter, ofdm-like.", errors);
    }

    [Fact]
    public void ValidatorRejectsUnsupportedDetectorModes()
    {
        var config = TestConfigFactory.CreateSyntheticBenchmarkConfig(
            experimentId: "bad-mode",
            detectorName: DetectorCatalog.LzmsaPaperDetectorName,
            detectorMode: "compressed-length");

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("Detector.Mode 'compressed-length' is not supported for detector 'lzmsa-paper' in M2. Supported modes: paper-byte-sum.", errors);
    }
}
