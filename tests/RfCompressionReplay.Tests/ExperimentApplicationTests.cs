using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
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
                ExperimentId: "m1-app",
                ExperimentName: "M1 App Test",
                Seed: 7,
                OutputDirectory: Path.Combine(tempRoot, "artifacts"),
                Scenario: new ScenarioConfig("dummy", 2, 4),
                TrialCount: 2,
                Detector: new DetectorConfig(DetectorCatalog.EnergyDetectorName, 0.5, DetectorCatalog.EnergyDetectorMode),
                Signal: new SignalConfig("dummy-signal", 1.0, 0.15),
                ManifestMetadata: new ManifestMetadataConfig("note", "v1", new Dictionary<string, string> { ["suite"] = "tests" }));

            var application = CreateApplication("2026-03-04T05:06:07Z");

            var result = application.Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");

            Assert.Equal("m1-app", result.Manifest.ExperimentId);
            Assert.Equal(2, result.Result.Trials.Count);
            Assert.NotNull(result.Result.Artifacts);
            Assert.All(result.Manifest.ArtifactPaths, relativePath => Assert.False(Path.IsPathRooted(relativePath)));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "summary.json")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "trials.csv")));
            Assert.Contains("Git commit could not be resolved.", result.Manifest.Warnings);
            Assert.Equal("note", result.Manifest.Metadata.Notes);
            Assert.Equal("v1", result.Manifest.Metadata.VersionTag);
            Assert.Equal("tests", result.Manifest.Metadata.Tags!["suite"]);
            Assert.All(result.Result.Trials, trial =>
            {
                Assert.Equal(DetectorCatalog.EnergyDetectorName, trial.DetectorName);
                Assert.NotEqual(0d, trial.Score);
            });
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
            var config = CreateConfig("same-second", "artifacts", DetectorCatalog.EnergyDetectorName, DetectorCatalog.EnergyDetectorMode);
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
            var config = CreateConfig("relative-output", "artifacts", DetectorCatalog.EnergyDetectorName, DetectorCatalog.EnergyDetectorMode);
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

    [Theory]
    [InlineData(DetectorCatalog.EnergyDetectorName, DetectorCatalog.EnergyDetectorMode)]
    [InlineData(DetectorCatalog.CovarianceAbsoluteValueDetectorName, DetectorCatalog.CovarianceAbsoluteValueDetectorMode)]
    [InlineData(DetectorCatalog.LzmsaPaperDetectorName, DetectorCatalog.LzmsaPaperDetectorMode)]
    public void RunsEndToEndForEachSupportedDetector(string detectorName, string detectorMode)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var threshold = detectorName == DetectorCatalog.LzmsaPaperDetectorName ? 1000d : 0d;
            var config = CreateConfig($"run-{detectorName}", "artifacts", detectorName, detectorMode, threshold);
            var configPath = Path.Combine(tempRoot, $"{detectorName}.json");
            File.WriteAllText(configPath, "{}\n");
            var application = CreateApplication("2026-03-22T21:00:00Z");

            var run = application.Run(config, configPath, "/does/not/exist");
            var summaryJson = File.ReadAllText(Path.Combine(run.RunDirectory, "summary.json"));
            var trialsCsv = File.ReadAllText(Path.Combine(run.RunDirectory, "trials.csv"));

            Assert.All(run.Result.Trials, trial =>
            {
                Assert.Equal(detectorName, trial.DetectorName);
                Assert.Equal(detectorMode, trial.DetectorMode);
                Assert.NotEqual(0d, trial.Score);
            });

            Assert.Contains(detectorName, summaryJson);
            Assert.Contains(detectorName, trialsCsv);
            Assert.DoesNotContain("placeholder", summaryJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("placeholder", trialsCsv, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }


    [Theory]
    [InlineData("m1.ed.json", DetectorCatalog.EnergyDetectorName)]
    [InlineData("m1.cav.json", DetectorCatalog.CovarianceAbsoluteValueDetectorName)]
    [InlineData("m1.lzmsa-paper.json", DetectorCatalog.LzmsaPaperDetectorName)]
    public void SampleConfigsRunEndToEnd(string configFileName, string detectorName)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var sampleConfigPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
            var config = ExperimentConfigJson.Load(sampleConfigPath) with { OutputDirectory = tempRoot };
            var application = CreateApplication("2026-03-22T21:15:00Z");

            var run = application.Run(config, sampleConfigPath, "/does/not/exist");
            var trialsCsv = File.ReadAllText(Path.Combine(run.RunDirectory, "trials.csv"));

            Assert.All(run.Result.Trials, trial =>
            {
                Assert.Equal(detectorName, trial.DetectorName);
                Assert.NotEqual(0d, trial.Score);
            });
            Assert.Contains(detectorName, trialsCsv);
        }
        finally
        {
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

    private static ExperimentConfig CreateConfig(string experimentId, string outputDirectory, string detectorName, string detectorMode, double threshold = 0.5)
    {
        return new ExperimentConfig(
            ExperimentId: experimentId,
            ExperimentName: "M1 App Test",
            Seed: 7,
            OutputDirectory: outputDirectory,
            Scenario: new ScenarioConfig("dummy", 2, 4),
            TrialCount: 2,
            Detector: new DetectorConfig(detectorName, threshold, detectorMode),
            Signal: new SignalConfig("dummy-signal", 1.0, 0.15),
            ManifestMetadata: new ManifestMetadataConfig("note", "v1", new Dictionary<string, string> { ["suite"] = "tests" }));
    }
}
