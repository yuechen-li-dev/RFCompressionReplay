using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Execution;

namespace RfCompressionReplay.Tests;

public sealed class ExperimentApplicationTests
{
    [Fact]
    public void RunsEndToEndAndGeneratesArtifactsInTempDirectory()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = new ExperimentConfig(
                ExperimentId: "m0-app",
                ExperimentName: "M0 App Test",
                Seed: 7,
                OutputDirectory: Path.Combine(tempRoot, "artifacts"),
                Scenario: new ScenarioConfig("dummy", 2, 4),
                TrialCount: 2,
                Detector: new DetectorConfig("placeholder-detector", 0.5, "placeholder"),
                Signal: new SignalConfig("dummy-signal", 1.0, 0.15),
                ManifestMetadata: new ManifestMetadataConfig("note", "v0", new Dictionary<string, string> { ["suite"] = "tests" }));

            var application = new ExperimentApplication(
                new FixedRunClock(DateTimeOffset.Parse("2026-03-04T05:06:07Z")),
                new RunDirectoryFactory(),
                new ArtifactFileWriter(new CsvArtifactWriter()),
                new EnvironmentSummaryProvider(),
                new GitCommitResolver());

            var result = application.Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");

            Assert.Equal("m0-app", result.Manifest.ExperimentId);
            Assert.Equal(2, result.Result.Trials.Count);
            Assert.NotNull(result.Result.Artifacts);
            Assert.All(result.Manifest.ArtifactPaths, relativePath => Assert.False(Path.IsPathRooted(relativePath)));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "summary.json")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "trials.csv")));
            Assert.Contains("Git commit could not be resolved.", result.Manifest.Warnings);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }
}
