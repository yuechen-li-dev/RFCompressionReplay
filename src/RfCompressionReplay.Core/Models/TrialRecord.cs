namespace RfCompressionReplay.Core.Models;

public sealed record TrialRecord(
    int TrialIndex,
    string ScenarioName,
    string TargetLabel,
    string SourceType,
    string DetectorName,
    string DetectorMode,
    double? SnrDb,
    int WindowLength,
    int WindowCount,
    int SampleCount,
    int StartIndex,
    double Score,
    bool IsAboveThreshold,
    double MeanSample,
    double PeakSample);
