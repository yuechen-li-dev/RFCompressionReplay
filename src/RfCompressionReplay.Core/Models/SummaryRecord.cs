namespace RfCompressionReplay.Core.Models;

public sealed record SummaryRecord(
    string? TaskName,
    string? ScenarioName,
    string? TargetLabel,
    string DetectorName,
    string DetectorMode,
    string? ScoreOrientation,
    double? ConditionSnrDb,
    double? SourceSnrDb,
    int? WindowLength,
    int Count,
    int? PositiveCount,
    int? NegativeCount,
    double MinScore,
    double MaxScore,
    double MeanScore,
    double StandardDeviation,
    int AboveThresholdCount,
    double? Auc);

public sealed record ExperimentSummary(
    IReadOnlyList<SummaryRecord> Groups);
