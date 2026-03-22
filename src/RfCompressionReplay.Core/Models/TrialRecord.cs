namespace RfCompressionReplay.Core.Models;

public sealed record TrialRecord(
    int TrialIndex,
    int WindowCount,
    int SampleCount,
    double Score,
    bool IsAboveThreshold,
    double MeanSample,
    double PeakSample);
