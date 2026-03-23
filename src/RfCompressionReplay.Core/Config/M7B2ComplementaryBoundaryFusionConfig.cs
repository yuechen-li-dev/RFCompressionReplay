namespace RfCompressionReplay.Core.Config;

public sealed record M7B2ComplementaryBoundaryFusionConfig(
    string ExperimentId,
    string ExperimentName,
    IReadOnlyList<int> SeedPanel,
    string OutputDirectory,
    ScenarioConfig Scenario,
    M7BStreamBenchmarkConfig Benchmark,
    M7B2BoundaryFusionEvaluationConfig Evaluation,
    ManifestMetadataConfig ManifestMetadata,
    string ArtifactRetentionMode = ArtifactRetentionModes.Milestone);

public sealed record M7B2BoundaryFusionEvaluationConfig(
    IReadOnlyList<DetectorConfig> Detectors,
    IReadOnlyList<M7B2FusionConfig> Fusions,
    IReadOnlyList<double> SnrDbValues,
    IReadOnlyList<int> WindowLengths,
    int StreamCountPerCondition,
    int MaxBoundaryProposals,
    double WindowStrideFraction,
    double BoundaryToleranceWindowMultiple,
    double MinPeakSpacingWindowMultiple,
    double PeakThresholdMadMultiplier);

public sealed record M7B2FusionConfig(
    string SignalId,
    string Description,
    string Rule,
    IReadOnlyList<string> SourceDetectorIds);
