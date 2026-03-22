namespace RfCompressionReplay.Core.Detectors;

public sealed record DetectorResult(
    string DetectorName,
    string DetectorMode,
    double Score,
    bool IsAboveThreshold,
    IReadOnlyDictionary<string, double> Metrics);
