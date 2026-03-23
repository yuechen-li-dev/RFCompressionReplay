namespace RfCompressionReplay.Core.Config;

public sealed record M7BChangePointConfig(
    string ExperimentId,
    string ExperimentName,
    IReadOnlyList<int> SeedPanel,
    string OutputDirectory,
    ScenarioConfig Scenario,
    M7BStreamBenchmarkConfig Benchmark,
    M7BChangePointEvaluationConfig Evaluation,
    ManifestMetadataConfig ManifestMetadata,
    string ArtifactRetentionMode = ArtifactRetentionModes.Milestone);

public sealed record M7BStreamBenchmarkConfig(
    GaussianNoiseConfig Noise,
    IReadOnlyList<M7BStreamTaskConfig> Tasks);

public sealed record M7BStreamTaskConfig(
    string Name,
    string Description,
    IReadOnlyList<M7BRegimeConfig> Regimes);

public sealed record M7BRegimeConfig(
    string Id,
    int LengthSamples,
    SyntheticCaseConfig SyntheticCase,
    bool ApplyConditionSnr = true);

public sealed record M7BChangePointEvaluationConfig(
    IReadOnlyList<DetectorConfig> Detectors,
    IReadOnlyList<double> SnrDbValues,
    IReadOnlyList<int> WindowLengths,
    int StreamCountPerCondition,
    int MaxBoundaryProposals,
    double WindowStrideFraction,
    double BoundaryToleranceWindowMultiple,
    double MinPeakSpacingWindowMultiple,
    double PeakThresholdMadMultiplier);
