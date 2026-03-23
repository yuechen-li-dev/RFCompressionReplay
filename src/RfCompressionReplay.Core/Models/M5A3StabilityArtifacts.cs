namespace RfCompressionReplay.Core.Models;

public sealed record M5A3AucComparisonRow(
    int Seed,
    string TaskName,
    double ConditionSnrDb,
    int WindowLength,
    double PaperAuc,
    double MeanCompressedByteValueAuc,
    double CompressedByteVarianceAuc,
    double Bucket0To63ProportionAuc,
    double Bucket64To127ProportionAuc,
    double Bucket128To191ProportionAuc,
    double Bucket192To255ProportionAuc,
    double PrefixThirdMeanCompressedByteValueAuc,
    double SuffixThirdMeanCompressedByteValueAuc,
    double AbsoluteDeltaFromPaperMeanCompressedByteValue,
    double AbsoluteDeltaFromPaperCompressedByteVariance,
    double AbsoluteDeltaFromPaperBucket0To63Proportion,
    double AbsoluteDeltaFromPaperBucket64To127Proportion,
    double AbsoluteDeltaFromPaperBucket128To191Proportion,
    double AbsoluteDeltaFromPaperBucket192To255Proportion,
    double AbsoluteDeltaFromPaperPrefixThirdMeanCompressedByteValue,
    double AbsoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue);

public sealed record M5A3DeltaSummaryRow(
    string AlternativeDetectorName,
    string FeatureFamily,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper);

public sealed record M5A3StabilitySummaryRow(
    string AlternativeDetectorName,
    string FeatureFamily,
    int ClosestNeighborCount,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper,
    double MedianClosenessRank);

public sealed record M5A3SeedRunRecord(
    int Seed,
    string RelativeRunDirectory,
    string RelativeManifestPath);

public sealed record M5A3StabilityManifest(
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
    ArtifactRetentionManifest Retention,
    IReadOnlyList<M5A3SeedRunRecord> SeedRuns);

public sealed record M5A3StabilityArtifacts(
    IReadOnlyList<M5A3AucComparisonRow> ComparisonRows,
    IReadOnlyList<M5A3DeltaSummaryRow> DeltaSummaryRows,
    IReadOnlyList<M5A3StabilitySummaryRow> StabilitySummaryRows,
    string FindingsMarkdown);
