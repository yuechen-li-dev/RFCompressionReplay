namespace RfCompressionReplay.Core.Models;

public sealed record SummaryRecord(
    string ScenarioName,
    string TargetLabel,
    string DetectorName,
    string DetectorMode,
    int Count,
    double MinScore,
    double MaxScore,
    double MeanScore,
    double StandardDeviation,
    int AboveThresholdCount);

public sealed record ExperimentSummary(
    IReadOnlyList<SummaryRecord> Groups);
