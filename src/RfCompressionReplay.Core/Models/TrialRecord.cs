namespace RfCompressionReplay.Core.Models;

public sealed record TrialRecord(
    int TrialIndex,
    string? TaskName,
    string ScenarioName,
    string TargetLabel,
    string? ClassLabel,
    bool? IsPositiveClass,
    string SourceType,
    string DetectorName,
    string DetectorMode,
    string? ScoreOrientation,
    double? ConditionSnrDb,
    double? SourceSnrDb,
    int WindowLength,
    int WindowCount,
    int SampleCount,
    int StartIndex,
    double Score,
    bool IsAboveThreshold,
    double MeanSample,
    double PeakSample);
