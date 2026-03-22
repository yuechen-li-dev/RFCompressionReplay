using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Experiments;
using RfCompressionReplay.Core.Randomness;
using RfCompressionReplay.Core.Signals;

namespace RfCompressionReplay.Tests;

public sealed class DummyScenarioTests
{
    [Fact]
    public void LegacyDummyScenarioRemainsDeterministic()
    {
        var config = new ExperimentConfig(
            ExperimentId: "determinism",
            ExperimentName: "Deterministic dummy",
            Seed: 42,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.DummyScenarioName, 2, 3),
            TrialCount: 3,
            Detector: new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1.0, DetectorCatalog.EnergyDetectorMode),
            Signal: new SignalConfig(ExperimentConfigValidator.DummySignalName, 1.25, 0.2),
            Benchmark: null,
            Evaluation: null,
            ManifestMetadata: ManifestMetadataConfig.Empty);

        var scenario = new DummyScenario(new DummySignalProvider(), new EnergyDetector());

        var resultA = scenario.Execute(config, new SeededRandom(config.Seed));
        var resultB = scenario.Execute(config, new SeededRandom(config.Seed));

        Assert.Equal(resultA.Trials.Count, resultB.Trials.Count);
        Assert.Equal(resultA.Trials.Select(trial => trial.Score), resultB.Trials.Select(trial => trial.Score));
        Assert.Equal(resultA.Summary.Groups.Select(group => group.MeanScore), resultB.Summary.Groups.Select(group => group.MeanScore));
        Assert.Equal(3, resultA.Trials.Count);
        Assert.Single(resultA.Summary.Groups);
    }
}
