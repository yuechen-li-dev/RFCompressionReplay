using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;

namespace RfCompressionReplay.Tests;

public sealed class ExperimentConfigTests
{
    [Fact]
    public void DeserializesSampleConfig()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs/m1.ed.json"));
        var config = ExperimentConfigJson.Load(path);

        Assert.Equal("m1-ed", config.ExperimentId);
        Assert.Equal(12345, config.Seed);
        Assert.Equal("dummy", config.Scenario.Name);
        Assert.Equal(4, config.TrialCount);
        Assert.Equal(DetectorCatalog.EnergyDetectorName, config.Detector.Name);
        Assert.Equal(DetectorCatalog.EnergyDetectorMode, config.Detector.Mode);
    }

    [Fact]
    public void ValidatorReturnsClearMessagesForInvalidConfig()
    {
        var config = new ExperimentConfig(
            ExperimentId: "",
            ExperimentName: "",
            Seed: -1,
            OutputDirectory: "",
            Scenario: new ScenarioConfig("", 0, 0),
            TrialCount: 0,
            Detector: new DetectorConfig("", 0.5, ""),
            Signal: new SignalConfig("", 1.0, 0.2),
            ManifestMetadata: ManifestMetadataConfig.Empty);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("ExperimentId is required.", errors);
        Assert.Contains("Seed must be zero or greater.", errors);
        Assert.Contains("TrialCount must be greater than zero.", errors);
        Assert.Contains("Scenario.SampleWindowCount must be greater than zero.", errors);
        Assert.Contains("Detector.Mode is required.", errors);
    }

    [Fact]
    public void ValidatorRejectsUnsupportedDetectorAndSignalIdentifiers()
    {
        var config = new ExperimentConfig(
            ExperimentId: "bad-identifiers",
            ExperimentName: "Bad identifiers",
            Seed: 1,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig("dummy", 1, 1),
            TrialCount: 1,
            Detector: new DetectorConfig("bogus-detector", 0.5, "bogus-mode"),
            Signal: new SignalConfig("bogus-signal", 1.0, 0.2),
            ManifestMetadata: ManifestMetadataConfig.Empty);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("Detector.Name 'bogus-detector' is not supported in M1. Supported detectors: ed, cav, lzmsa-paper.", errors);
        Assert.Contains("Signal.Name 'bogus-signal' is not supported in M1. Supported signals: dummy-signal.", errors);
    }

    [Fact]
    public void ValidatorRejectsUnsupportedDetectorModes()
    {
        var config = new ExperimentConfig(
            ExperimentId: "bad-mode",
            ExperimentName: "Bad mode",
            Seed: 1,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig("dummy", 1, 4),
            TrialCount: 1,
            Detector: new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 0.5, "compressed-length"),
            Signal: new SignalConfig("dummy-signal", 1.0, 0.2),
            ManifestMetadata: ManifestMetadataConfig.Empty);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("Detector.Mode 'compressed-length' is not supported for detector 'lzmsa-paper' in M1. Supported modes: paper-byte-sum.", errors);
    }
}
