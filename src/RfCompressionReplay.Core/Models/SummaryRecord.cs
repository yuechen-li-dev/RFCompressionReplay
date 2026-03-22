namespace RfCompressionReplay.Core.Models;

public sealed record SummaryRecord(
    string DetectorName,
    string DetectorMode,
    int TrialCount,
    double MinScore,
    double MaxScore,
    double MeanScore,
    int AboveThresholdCount);
