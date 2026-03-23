namespace RfCompressionReplay.Core.Models;

public sealed record M6A1AucComparisonRow(
    string TaskFamilyId,
    int Seed,
    double SnrDb,
    int WindowLength,
    string DetectorId,
    double Auc);

public sealed record M6A1TaskSummaryRow(
    string TaskFamilyId,
    string DetectorId,
    double MedianAuc,
    double MaxAuc,
    int BestOrTiedBestConditionCount,
    int ConditionCount,
    double MedianGapToBestBaselineAuc,
    double MaxGapToBestBaselineAuc,
    double? MedianGapToLzmsaPaperAuc,
    double? MedianGapToRmsNormalizedMeanAuc,
    string ComparisonNote);

public sealed record M6A1SeedRecord(
    int Seed);

public sealed record M6A1UsefulnessManifest(
    string ExperimentId,
    string ExperimentName,
    DateTimeOffset UtcTimestamp,
    IReadOnlyList<int> SeedPanel,
    string GitCommit,
    EnvironmentSummary Environment,
    string ConfigFilePath,
    string ScenarioName,
    int TrialCountPerCondition,
    IReadOnlyList<string> ArtifactPaths,
    IReadOnlyList<string> Warnings,
    ManifestMetadata Metadata,
    EvaluationManifest Evaluation,
    ArtifactRetentionManifest Retention);

public sealed record M6A1UsefulnessArtifacts(
    IReadOnlyList<M6A1AucComparisonRow> ComparisonRows,
    IReadOnlyList<M6A1TaskSummaryRow> TaskSummaryRows,
    string FindingsMarkdown);
