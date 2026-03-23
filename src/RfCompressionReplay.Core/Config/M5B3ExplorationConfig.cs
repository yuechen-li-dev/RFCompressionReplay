namespace RfCompressionReplay.Core.Config;

public sealed record M5B3ExplorationConfig(
    string ExperimentId,
    string ExperimentName,
    IReadOnlyList<int> SeedPanel,
    IReadOnlyList<double> ScaleValues,
    IReadOnlyList<M5B3RepresentationFamilyConfig> RepresentationFamilies,
    string OutputDirectory,
    ScenarioConfig Scenario,
    int TrialCount,
    DetectorConfig Detector,
    SignalConfig? Signal,
    SyntheticBenchmarkConfig Benchmark,
    EvaluationConfig Evaluation,
    ManifestMetadataConfig ManifestMetadata,
    string ArtifactRetentionMode = ArtifactRetentionModes.Milestone)
{
    public ExperimentConfig ToSeededExperimentConfig(int seed, M5B3RepresentationFamilyConfig family, double scaleValue)
    {
        var representation = family.Representation with { SampleScale = scaleValue };
        return new ExperimentConfig(
            ExperimentId,
            ExperimentName,
            seed,
            OutputDirectory,
            Scenario,
            TrialCount,
            Detector,
            Signal,
            Benchmark,
            Evaluation,
            ManifestMetadata,
            ArtifactRetentionMode,
            representation);
    }
}

public sealed record M5B3RepresentationFamilyConfig(
    string Id,
    string Description,
    RepresentationConfig Representation);
