using System.Text.Json;
using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Tests;

public sealed class ArtifactWriterTests
{
    [Fact]
    public void WritesManifestAndSummaryArtifacts()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var writer = new ArtifactFileWriter(new CsvArtifactWriter());
            var runDirectory = Path.Combine(tempRoot, "run");
            var result = new ExperimentResult(
                new[] { new TrialRecord(0, 1, 4, 1.2, true, 1.1, 1.4) },
                new SummaryRecord(1, 1.2, 1.2, 1.2, 1),
                null);
            var manifest = new RunManifest(
                "exp",
                "Experiment",
                DateTimeOffset.Parse("2026-01-02T03:04:05Z"),
                123,
                "unknown",
                new EnvironmentSummary("machine", "user", "os", ".NET 8", "X64", "/tmp"),
                "../config.json",
                "dummy",
                1,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new ManifestMetadata("note", "v0", new Dictionary<string, string> { ["tag"] = "value" }));

            var artifacts = writer.WriteRunArtifacts(runDirectory, result, manifest);

            Assert.True(File.Exists(artifacts.ManifestPath));
            Assert.True(File.Exists(artifacts.SummaryPath));
            Assert.True(File.Exists(artifacts.TrialsCsvPath));

            var summaryJson = File.ReadAllText(artifacts.SummaryPath);
            Assert.Contains("meanScore", summaryJson);

            var manifestRoundTrip = JsonSerializer.Deserialize<RunManifest>(File.ReadAllText(artifacts.ManifestPath), ExperimentConfigJson.SerializerOptions);
            Assert.NotNull(manifestRoundTrip);
            Assert.Equal("exp", manifestRoundTrip!.ExperimentId);
            Assert.Equal("note", manifestRoundTrip.Metadata.Notes);
            Assert.Equal("v0", manifestRoundTrip.Metadata.VersionTag);
            Assert.Equal("value", manifestRoundTrip.Metadata.Tags!["tag"]);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }
}
