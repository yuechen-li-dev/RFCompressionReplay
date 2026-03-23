namespace RfCompressionReplay.Core.Models;

public sealed record ArtifactPaths(
    string RunDirectory,
    string ManifestPath,
    string SummaryPath,
    string SummaryCsvPath,
    string? TrialsCsvPath = null,
    string? RocPointsCsvPath = null,
    string? RocPointsCompactCsvPath = null,
    string? M4AucComparisonCsvPath = null,
    string? M4FindingsPath = null,
    string? M5A1AucComparisonCsvPath = null,
    string? M5A1FindingsPath = null,
    string? M5A1DeltaSummaryCsvPath = null,
    string? M5A2AucComparisonCsvPath = null,
    string? M5A2FindingsPath = null,
    string? M5A2DeltaSummaryCsvPath = null);
