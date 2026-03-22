namespace RfCompressionReplay.Core.Models;

public sealed record SummaryRecord(
    int TrialCount,
    double MinScore,
    double MaxScore,
    double MeanScore,
    int AboveThresholdCount);
