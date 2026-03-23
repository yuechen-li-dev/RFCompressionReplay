namespace RfCompressionReplay.Core.Config;

public sealed record M5B2ExplorationConfig(
    string ExperimentId,
    string ExperimentName,
    IReadOnlyList<int> SeedPanel,
    IReadOnlyList<M5B2PerturbationConfig> Perturbations,
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
    public ExperimentConfig ToSeededExperimentConfig(int seed, M5B2PerturbationConfig perturbation)
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

public sealed record M5B2PerturbationConfig(
    string Id,
    string AxisTag,
    string Description,
    RepresentationConfig Representation);

public static class M5B2PerturbationAxes
{
    public const string Baseline = "baseline";
    public const string Scale = "scale";
    public const string Packing = "packing";
    public const string Combined = "combined";

    public static IReadOnlyList<string> SupportedAxes { get; } =
    [
        Baseline,
        Scale,
        Packing,
        Combined,
    ];

    public static bool IsSupported(string axisTag)
    {
        return SupportedAxes.Contains(axisTag, StringComparer.OrdinalIgnoreCase);
    }

    public static string SupportedAxesDisplay => string.Join(", ", SupportedAxes);
}
