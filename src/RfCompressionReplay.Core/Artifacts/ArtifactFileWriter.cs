using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Artifacts;

public sealed class ArtifactFileWriter
{
    private readonly CsvArtifactWriter _csvWriter;

    public ArtifactFileWriter(CsvArtifactWriter csvWriter)
    {
        _csvWriter = csvWriter;
    }

    public ArtifactPaths WriteRunArtifacts(string runDirectory, ExperimentResult result, RunManifest manifest)
    {
        Directory.CreateDirectory(runDirectory);

        var manifestPath = Path.Combine(runDirectory, "manifest.json");
        var summaryPath = Path.Combine(runDirectory, "summary.json");
        var summaryCsvPath = Path.Combine(runDirectory, "summary.csv");
        var trialsPath = Path.Combine(runDirectory, "trials.csv");
        var rocPointsPath = Path.Combine(runDirectory, "roc_points.csv");

        ExperimentConfigJson.Save(manifestPath, manifest);
        ExperimentConfigJson.Save(summaryPath, result.Summary);
        _csvWriter.WriteSummary(summaryCsvPath, result.Summary.Groups);
        _csvWriter.WriteTrials(trialsPath, result.Trials);
        _csvWriter.WriteRocPoints(rocPointsPath, result.Evaluation?.RocPoints ?? Array.Empty<RocPointRecord>());

        return new ArtifactPaths(runDirectory, manifestPath, summaryPath, summaryCsvPath, trialsPath, rocPointsPath);
    }
}
