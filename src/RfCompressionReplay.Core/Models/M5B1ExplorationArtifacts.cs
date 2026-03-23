namespace RfCompressionReplay.Core.Models;

public sealed record M5B1AucComparisonRow(
    string PerturbationId,
    int Seed,
    string TaskName,
    double ConditionSnrDb,
    int WindowLength,
    double PaperAuc,
    double MeanCompressedByteValueAuc,
    double Bucket64To127ProportionAuc,
    double SuffixThirdMeanCompressedByteValueAuc,
    double AbsoluteDeltaFromPaperMeanCompressedByteValue,
    double AbsoluteDeltaFromPaperBucket64To127Proportion,
    double AbsoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue);

public sealed record M5B1DeltaSummaryRow(
    string PerturbationId,
    string AlternativeDetectorName,
    string FeatureFamily,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper);

public sealed record M5B1StabilitySummaryRow(
    string GroupingScope,
    string? PerturbationId,
    string AlternativeDetectorName,
    string FeatureFamily,
    int ClosestNeighborCount,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper,
    double MedianClosenessRank);

public sealed record M5B1SeedRunRecord(
    string PerturbationId,
    int Seed,
    string RelativeRunDirectory,
    string RelativeManifestPath);

public sealed record M5B1ExplorationManifest(
    string ExperimentId,
    string ExperimentName,
    DateTimeOffset UtcTimestamp,
    IReadOnlyList<int> SeedPanel,
    IReadOnlyList<string> PerturbationIds,
    string GitCommit,
    EnvironmentSummary Environment,
    string ConfigFilePath,
    string ScenarioName,
    int TrialCountPerCondition,
    IReadOnlyList<string> ArtifactPaths,
    IReadOnlyList<string> Warnings,
    ManifestMetadata Metadata,
    EvaluationManifest Evaluation,
    ArtifactRetentionManifest Retention,
    IReadOnlyList<M5B1SeedRunRecord> SeedRuns);

public sealed record M5B1ExplorationArtifacts(
    IReadOnlyList<M5B1AucComparisonRow> ComparisonRows,
    IReadOnlyList<M5B1DeltaSummaryRow> DeltaSummaryRows,
    IReadOnlyList<M5B1StabilitySummaryRow> StabilitySummaryRows,
    string FindingsMarkdown);
