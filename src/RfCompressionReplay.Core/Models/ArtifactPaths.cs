namespace RfCompressionReplay.Core.Models;

public sealed record ArtifactPaths(
    string RunDirectory,
    string ManifestPath,
    string SummaryPath,
    string SummaryCsvPath,
    string TrialsCsvPath,
    string RocPointsCsvPath,
    string? M4AucComparisonCsvPath = null,
    string? M4FindingsPath = null);
