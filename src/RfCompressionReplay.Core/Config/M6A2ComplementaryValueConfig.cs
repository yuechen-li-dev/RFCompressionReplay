namespace RfCompressionReplay.Core.Config;

public sealed record M6A2ComplementaryValueConfig(
    string ExperimentId,
    string ExperimentName,
    IReadOnlyList<int> SeedPanel,
    string OutputDirectory,
    ScenarioConfig Scenario,
    int TrialCount,
    DetectorConfig Detector,
    SignalConfig? Signal,
    SyntheticBenchmarkConfig Benchmark,
    EvaluationConfig Evaluation,
    IReadOnlyList<FeatureBundleConfig> Bundles,
    ManifestMetadataConfig ManifestMetadata,
    string ArtifactRetentionMode = ArtifactRetentionModes.Milestone)
{
    public ExperimentConfig ToSeededExperimentConfig(int seed)
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
            ArtifactRetentionMode);
    }
}

public sealed record FeatureBundleConfig(
    string Id,
    string Description,
    IReadOnlyList<string> FeatureDetectors);
