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

            var application = CreateApplication("2026-03-04T05:06:07Z");

            var result = application.Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");

            Assert.Equal("m0-app", result.Manifest.ExperimentId);
            Assert.Equal(2, result.Result.Trials.Count);
            Assert.NotNull(result.Result.Artifacts);
            Assert.All(result.Manifest.ArtifactPaths, relativePath => Assert.False(Path.IsPathRooted(relativePath)));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "summary.json")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "trials.csv")));
            Assert.Contains("Git commit could not be resolved.", result.Manifest.Warnings);
            Assert.Equal("note", result.Manifest.Metadata.Notes);
            Assert.Equal("v0", result.Manifest.Metadata.VersionTag);
            Assert.Equal("tests", result.Manifest.Metadata.Tags!["suite"]);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void UsesUniqueRunDirectoryWhenSameSecondRunAlreadyExists()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = CreateConfig("same-second", "artifacts");
            var configPath = Path.Combine(tempRoot, "configs", "config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, "{}\n");
            var application = CreateApplication("2026-03-22T20:40:00Z");

            var firstRun = application.Run(config, configPath, "/does/not/exist");
            var secondRun = application.Run(config, configPath, "/does/not/exist");

            Assert.NotEqual(firstRun.RunDirectory, secondRun.RunDirectory);
            Assert.Equal(Path.Combine(tempRoot, "configs", "artifacts", "20260322T204000Z_same-second_seed7"), firstRun.RunDirectory);
            Assert.Equal(Path.Combine(tempRoot, "configs", "artifacts", "20260322T204000Z_same-second_seed7_2"), secondRun.RunDirectory);
            Assert.True(File.Exists(Path.Combine(firstRun.RunDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(secondRun.RunDirectory, "manifest.json")));
            Assert.Equal(2, Directory.GetDirectories(Path.Combine(tempRoot, "configs", "artifacts")).Length);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void ResolvesRelativeOutputDirectoryAgainstConfigLocationInsteadOfCurrentDirectory()
    {
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var unrelatedCurrentDirectory = Path.Combine(tempRoot, "elsewhere");
        var configDirectory = Path.Combine(tempRoot, "configs");
        Directory.CreateDirectory(unrelatedCurrentDirectory);
        Directory.CreateDirectory(configDirectory);

        try
        {
            Directory.SetCurrentDirectory(unrelatedCurrentDirectory);
            var config = CreateConfig("relative-output", "artifacts");
            var configPath = Path.Combine(configDirectory, "config.json");
            File.WriteAllText(configPath, "{}\n");
            var application = CreateApplication("2026-03-22T20:45:00Z");

            var run = application.Run(config, configPath, "/does/not/exist");

            Assert.StartsWith(Path.Combine(configDirectory, "artifacts"), run.RunDirectory, StringComparison.Ordinal);
            Assert.DoesNotContain(Path.Combine(unrelatedCurrentDirectory, "artifacts"), run.RunDirectory, StringComparison.Ordinal);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
            Directory.Delete(tempRoot, true);
        }
    }

    private static ExperimentApplication CreateApplication(string utcTimestamp)
    {
        return new ExperimentApplication(
            new FixedRunClock(DateTimeOffset.Parse(utcTimestamp)),
            new RunDirectoryFactory(),
            new ArtifactFileWriter(new CsvArtifactWriter()),
            new EnvironmentSummaryProvider(),
            new GitCommitResolver());
    }

    private static ExperimentConfig CreateConfig(string experimentId, string outputDirectory)
    {
        return new ExperimentConfig(
            ExperimentId: experimentId,
            ExperimentName: "M0 App Test",
            Seed: 7,
            OutputDirectory: outputDirectory,
            Scenario: new ScenarioConfig("dummy", 2, 4),
            TrialCount: 2,
            Detector: new DetectorConfig("placeholder-detector", 0.5, "placeholder"),
            Signal: new SignalConfig("dummy-signal", 1.0, 0.15),
            ManifestMetadata: new ManifestMetadataConfig("note", "v0", new Dictionary<string, string> { ["suite"] = "tests" }));
    }
}
