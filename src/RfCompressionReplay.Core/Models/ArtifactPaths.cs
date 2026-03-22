namespace RfCompressionReplay.Core.Models;

public sealed record ArtifactPaths(
    string RunDirectory,
    string ManifestPath,
    string SummaryPath,
    string SummaryCsvPath,
    string TrialsCsvPath,
    string RocPointsCsvPath);
