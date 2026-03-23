namespace RfCompressionReplay.Core.Models;

public sealed record M6A2AucComparisonRow(
    string TaskFamilyId,
    int Seed,
    double SnrDb,
    int WindowLength,
    string DetectorId,
    double Auc);

public sealed record M6A2BundleConditionRow(
    string TaskFamilyId,
    int Seed,
    double SnrDb,
    int WindowLength,
    string BundleId,
    double Auc);

public sealed record M6A2BundleSummaryRow(
    string TaskFamilyId,
    string BundleId,
    double MedianAuc,
    double MaxAuc,
    int BestOrTiedBestConditionCount,
    int ConditionCount,
    int ConditionWinsOverBundleA,
    double MedianImprovementVsBundleA,
    double MaxImprovementVsBundleA,
    string ComparisonNote);

public sealed record M6A2ComplementaryValueManifest(
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

public sealed record M6A2ComplementaryValueArtifacts(
    IReadOnlyList<M6A2AucComparisonRow> ComparisonRows,
    IReadOnlyList<M6A2BundleConditionRow> BundleConditionRows,
    IReadOnlyList<M6A2BundleSummaryRow> BundleSummaryRows,
    string FindingsMarkdown);
