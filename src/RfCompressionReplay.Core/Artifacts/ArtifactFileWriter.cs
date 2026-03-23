using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Artifacts;

public sealed class ArtifactFileWriter
{
    private readonly CsvArtifactWriter _csvWriter;

    public ArtifactFileWriter(CsvArtifactWriter csvWriter)
    {
        _csvWriter = csvWriter;
    }

    public ArtifactPaths WriteRunArtifacts(string runDirectory, ExperimentConfig config, ExperimentResult result, RunManifest manifest)
    {
        Directory.CreateDirectory(runDirectory);

        var manifestPath = Path.Combine(runDirectory, "manifest.json");
        var summaryPath = Path.Combine(runDirectory, "summary.json");
        var summaryCsvPath = Path.Combine(runDirectory, "summary.csv");
        var trialsPath = Path.Combine(runDirectory, "trials.csv");
        var rocPointsPath = Path.Combine(runDirectory, "roc_points.csv");
        string? m4AucComparisonCsvPath = null;
        string? m4FindingsPath = null;
        string? m5A1AucComparisonCsvPath = null;
        string? m5A1FindingsPath = null;
        string? m5A1DeltaSummaryCsvPath = null;

        ExperimentConfigJson.Save(manifestPath, manifest);
        ExperimentConfigJson.Save(summaryPath, result.Summary);
        _csvWriter.WriteSummary(summaryCsvPath, result.Summary.Groups);
        _csvWriter.WriteTrials(trialsPath, result.Trials);
        _csvWriter.WriteRocPoints(rocPointsPath, result.Evaluation?.RocPoints ?? Array.Empty<RocPointRecord>());

        if (M4ScoreIdentityComparisonReportBuilder.IsEnabled(config))
        {
            var comparison = M4ScoreIdentityComparisonReportBuilder.Build(config, result);
            var artifactPrefix = M4ScoreIdentityComparisonReportBuilder.GetArtifactPrefix(config);
            m4AucComparisonCsvPath = Path.Combine(runDirectory, $"{artifactPrefix}_auc_comparison.csv");
            m4FindingsPath = Path.Combine(runDirectory, $"{artifactPrefix}_findings.md");
            M4ScoreIdentityComparisonReportBuilder.WriteCsv(m4AucComparisonCsvPath, comparison.Rows);
            File.WriteAllText(m4FindingsPath, comparison.FindingsMarkdown);
        }

        if (M5A1ScoreDecompositionReportBuilder.IsEnabled(config))
        {
            var comparison = M5A1ScoreDecompositionReportBuilder.Build(config, result);
            m5A1AucComparisonCsvPath = Path.Combine(runDirectory, $"{M5A1ScoreDecompositionReportBuilder.ArtifactPrefix}_auc_comparison.csv");
            m5A1FindingsPath = Path.Combine(runDirectory, $"{M5A1ScoreDecompositionReportBuilder.ArtifactPrefix}_findings.md");
            m5A1DeltaSummaryCsvPath = Path.Combine(runDirectory, $"{M5A1ScoreDecompositionReportBuilder.ArtifactPrefix}_delta_summary.csv");
            M5A1ScoreDecompositionReportBuilder.WriteComparisonCsv(m5A1AucComparisonCsvPath, comparison.Rows);
            M5A1ScoreDecompositionReportBuilder.WriteAggregateDeltaCsv(m5A1DeltaSummaryCsvPath, comparison.AggregateDeltaRows);
            File.WriteAllText(m5A1FindingsPath, comparison.FindingsMarkdown);
        }

        return new ArtifactPaths(runDirectory, manifestPath, summaryPath, summaryCsvPath, trialsPath, rocPointsPath, m4AucComparisonCsvPath, m4FindingsPath, m5A1AucComparisonCsvPath, m5A1FindingsPath, m5A1DeltaSummaryCsvPath);
    }
}
