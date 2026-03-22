using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Tests;

public sealed class ExperimentConfigTests
{
    [Fact]
    public void DeserializesSampleConfig()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs/m0.dummy.json"));
        var config = ExperimentConfigJson.Load(path);

        Assert.Equal("m0-dummy", config.ExperimentId);
        Assert.Equal(12345, config.Seed);
        Assert.Equal("dummy", config.Scenario.Name);
        Assert.Equal(4, config.TrialCount);
        Assert.Equal("placeholder-detector", config.Detector.Name);
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

        Assert.Contains("Detector.Name 'bogus-detector' is not supported in M0. Supported detectors: placeholder-detector.", errors);
        Assert.Contains("Detector.Mode 'bogus-mode' is not supported in M0. Supported modes: placeholder.", errors);
        Assert.Contains("Signal.Name 'bogus-signal' is not supported in M0. Supported signals: dummy-signal.", errors);
    }
}
