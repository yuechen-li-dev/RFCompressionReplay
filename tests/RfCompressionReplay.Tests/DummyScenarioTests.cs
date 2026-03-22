using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Experiments;
using RfCompressionReplay.Core.Randomness;
using RfCompressionReplay.Core.Signals;

namespace RfCompressionReplay.Tests;

public sealed class DummyScenarioTests
{
    [Fact]
    public void ProducesDeterministicResultsForFixedSeed()
    {
        var config = new ExperimentConfig(
            ExperimentId: "determinism",
            ExperimentName: "Deterministic dummy",
            Seed: 42,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig("dummy", 2, 3),
            TrialCount: 3,
            Detector: new DetectorConfig("placeholder-detector", 1.0, "placeholder"),
            Signal: new SignalConfig("dummy-signal", 1.25, 0.2),
            ManifestMetadata: ManifestMetadataConfig.Empty);

        var scenario = new DummyScenario(new DummySignalProvider(), new PlaceholderDetector());

        var resultA = scenario.Execute(config, new SeededRandom(config.Seed));
        var resultB = scenario.Execute(config, new SeededRandom(config.Seed));

        Assert.Equal(resultA.Trials, resultB.Trials);
        Assert.Equal(resultA.Summary, resultB.Summary);
        Assert.Equal(3, resultA.Trials.Count);
        Assert.Equal(3, resultA.Summary.AboveThresholdCount);
    }
}
