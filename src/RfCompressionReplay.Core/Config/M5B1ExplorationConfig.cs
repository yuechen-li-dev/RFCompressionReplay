namespace RfCompressionReplay.Core.Config;

public sealed record M5B1ExplorationConfig(
    string ExperimentId,
    string ExperimentName,
    IReadOnlyList<int> SeedPanel,
    IReadOnlyList<M5B1PerturbationConfig> Perturbations,
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
    public ExperimentConfig ToSeededExperimentConfig(int seed, M5B1PerturbationConfig perturbation)
    {
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
            perturbation.Representation);
    }
}

public sealed record M5B1PerturbationConfig(
    string Id,
    string Description,
    RepresentationConfig Representation);
