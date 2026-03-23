namespace RfCompressionReplay.Core.Models;

public sealed record M7BBoundaryComparisonRow(
    string TaskFamilyId,
    int Seed,
    double SnrDb,
    int WindowLength,
    string DetectorId,
    double OnsetHitRate,
    double? OffsetHitRate,
    double? MedianOnsetLocalizationError,
    double? MedianOffsetLocalizationError,
    double MedianFalsePositiveCount,
    double? MedianOnsetDetectionDelay,
    double? MedianOffsetDetectionDelay,
    int StreamCount);

public sealed record M7BTaskSummaryRow(
    string TaskFamilyId,
    string DetectorId,
    double MedianOnsetHitRate,
    double? MedianOffsetHitRate,
    double? MedianOnsetLocalizationError,
    double? MedianOffsetLocalizationError,
    double MedianFalsePositiveCount,
    int BestOrTiedBestConditionCount,
    int DistinctHitVsBestBaselineConditionCount,
    string ComparisonNote);

public sealed record M7BChangePointManifest(
    string ExperimentId,
    string ExperimentName,
    DateTimeOffset UtcTimestamp,
    IReadOnlyList<int> SeedPanel,
    string GitCommit,
    EnvironmentSummary Environment,
    string ConfigFilePath,
    string ScenarioName,
    int StreamCountPerCondition,
    IReadOnlyList<string> ArtifactPaths,
    IReadOnlyList<string> Warnings,
    ManifestMetadata Metadata,
    EvaluationManifest Evaluation,
    ArtifactRetentionManifest Retention);

public sealed record M7BChangePointArtifacts(
    IReadOnlyList<M7BBoundaryComparisonRow> ComparisonRows,
    IReadOnlyList<M7BTaskSummaryRow> TaskSummaryRows,
    string FindingsMarkdown);

public sealed record M7BStreamBoundaryMetrics(
    bool OnsetHit,
    bool? OffsetHit,
    double? OnsetLocalizationError,
    double? OffsetLocalizationError,
    int FalsePositiveCount,
    double? OnsetDetectionDelay,
    double? OffsetDetectionDelay,
    IReadOnlyList<int> ProposedBoundaries);
