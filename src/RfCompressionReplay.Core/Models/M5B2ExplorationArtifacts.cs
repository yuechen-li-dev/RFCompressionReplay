namespace RfCompressionReplay.Core.Models;

public sealed record M5B2AucComparisonRow(
    string PerturbationId,
    string PerturbationAxisTag,
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

public sealed record M5B2DeltaSummaryRow(
    string PerturbationId,
    string PerturbationAxisTag,
    string AlternativeDetectorName,
    string FeatureFamily,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper);

public sealed record M5B2AxisSummaryRow(
    string PerturbationAxisTag,
    string AlternativeDetectorName,
    string FeatureFamily,
    int ClosestNeighborCount,
    int CombinationCount,
    double WinRate,
    double MedianAbsoluteAucDeltaFromPaper,
    double MaxAbsoluteAucDeltaFromPaper,
    double MedianClosenessRank,
    string AxisMedianLeader,
    string AxisClosestLeader);

public sealed record M5B2SeedRunRecord(
    string PerturbationId,
    string PerturbationAxisTag,
    int Seed);

public sealed record M5B2ExplorationManifest(
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
    IReadOnlyList<M5B2SeedRunRecord> SeedRuns);

public sealed record M5B2ExplorationArtifacts(
    IReadOnlyList<M5B2AucComparisonRow> ComparisonRows,
    IReadOnlyList<M5B2DeltaSummaryRow> DeltaSummaryRows,
    IReadOnlyList<M5B2AxisSummaryRow> AxisSummaryRows,
    string FindingsMarkdown);
