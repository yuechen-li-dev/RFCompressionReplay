namespace RfCompressionReplay.Core.Detectors;

public sealed record DetectorResult(string DetectorName, double Score, bool IsAboveThreshold, IReadOnlyDictionary<string, double> Metrics);
