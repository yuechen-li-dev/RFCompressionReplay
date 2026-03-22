using System.Text.Json;
using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Tests;

public sealed class ArtifactWriterTests
{
    [Fact]
    public void WritesManifestSummaryTrialAndRocArtifacts()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var writer = new ArtifactFileWriter(new CsvArtifactWriter());
            var runDirectory = Path.Combine(tempRoot, "run");
            var result = new ExperimentResult(
                new[]
                {
                    new TrialRecord(0, "ofdm-signal-present-vs-noise-only", "noise-only-baseline", "noise-only", "noise-only", false, "noise-only", "ed", "average-energy", "HigherScoreMorePositive", -3d, null, 128, 2, 256, 17, 1.2, true, 0.1, 1.4)
                },
                new ExperimentSummary(
                    new[]
                    {
                        new SummaryRecord("ofdm-signal-present-vs-noise-only", null, null, "ed", "average-energy", "HigherScoreMorePositive", -3d, null, 128, 2, 1, 1, 1.2, 1.2, 1.2, 0d, 1, 0.75)
                    }),
                new EvaluationArtifacts(
                    new[]
                    {
                        new RocPointRecord("ofdm-signal-present-vs-noise-only", "ed", "average-energy", "HigherScoreMorePositive", -3d, 128, 1.2d, 1d, 0d, 1, 0, 1, 1, 0.75d)
                    }),
                null);
            var manifest = new RunManifest(
                "exp",
                "Experiment",
                DateTimeOffset.Parse("2026-01-02T03:04:05Z"),
                123,
                "unknown",
                new EnvironmentSummary("machine", "user", "os", ".NET 8", "X64", "/tmp"),
                "../config.json",
                "synthetic-benchmark",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new ManifestMetadata("note", "v0", new Dictionary<string, string> { ["tag"] = "value" }),
                new EvaluationManifest(["ofdm-signal-present-vs-noise-only"], ["ed"], [-3d], [128], 1));

            var artifacts = writer.WriteRunArtifacts(runDirectory, result, manifest);

            Assert.True(File.Exists(artifacts.ManifestPath));
            Assert.True(File.Exists(artifacts.SummaryPath));
            Assert.True(File.Exists(artifacts.SummaryCsvPath));
            Assert.True(File.Exists(artifacts.TrialsCsvPath));
            Assert.True(File.Exists(artifacts.RocPointsCsvPath));

            var summaryJson = File.ReadAllText(artifacts.SummaryPath);
            Assert.Contains("meanScore", summaryJson);
            Assert.Contains("taskName", summaryJson);

            var summaryCsv = File.ReadAllText(artifacts.SummaryCsvPath);
            Assert.Contains("auc", summaryCsv);

            var trialsCsv = File.ReadAllText(artifacts.TrialsCsvPath);
            Assert.Contains("detectorName", trialsCsv);
            Assert.Contains("taskName", trialsCsv);

            var rocCsv = File.ReadAllText(artifacts.RocPointsCsvPath);
            Assert.Contains("threshold", rocCsv);
            Assert.Contains("tpr", rocCsv);

            var manifestRoundTrip = JsonSerializer.Deserialize<RunManifest>(File.ReadAllText(artifacts.ManifestPath), ExperimentConfigJson.SerializerOptions);
            Assert.NotNull(manifestRoundTrip);
            Assert.Equal("exp", manifestRoundTrip!.ExperimentId);
            Assert.Equal("note", manifestRoundTrip.Metadata.Notes);
            Assert.Equal("v0", manifestRoundTrip.Metadata.VersionTag);
            Assert.Equal("value", manifestRoundTrip.Metadata.Tags!["tag"]);
            Assert.Single(manifestRoundTrip.Evaluation!.TaskNames);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }
}
