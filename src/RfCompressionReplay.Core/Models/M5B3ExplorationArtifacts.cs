namespace RfCompressionReplay.Core.Models;

public sealed record M5B3AucComparisonRow(
    string RepresentationFamilyId,
    double ScaleValue,
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

public sealed record M5B3DeltaSummaryRow(
    string RepresentationFamilyId,
    string AlternativeDetectorName,
    string FeatureFamily,
    int ClosestNeighborCount,
    int CombinationCount,
    double WinRate,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper,
    double MedianClosenessRank);

public sealed record M5B3ScaleSummaryRow(
    string RepresentationFamilyId,
    double ScaleValue,
    string AlternativeDetectorName,
    string FeatureFamily,
    int ClosestNeighborCount,
    int CombinationCount,
    double WinRate,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper,
    double MedianClosenessRank,
    string ScaleMedianLeader,
    string ScaleClosestLeader,
    string TrendLabel);

public sealed record M5B3SeedRunRecord(
    string RepresentationFamilyId,
    double ScaleValue,
    int Seed);

public sealed record M5B3ExplorationManifest(
    string ExperimentId,
    string ExperimentName,
    DateTimeOffset UtcTimestamp,
    IReadOnlyList<int> SeedPanel,
    IReadOnlyList<double> ScaleValues,
    IReadOnlyList<string> RepresentationFamilyIds,
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
    IReadOnlyList<M5B3SeedRunRecord> SeedRuns);

public sealed record M5B3ExplorationArtifacts(
    IReadOnlyList<M5B3AucComparisonRow> ComparisonRows,
    IReadOnlyList<M5B3DeltaSummaryRow> DeltaSummaryRows,
    IReadOnlyList<M5B3ScaleSummaryRow> ScaleSummaryRows,
    string FindingsMarkdown);
