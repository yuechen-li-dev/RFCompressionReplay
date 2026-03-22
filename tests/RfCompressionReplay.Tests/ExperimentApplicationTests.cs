using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Execution;

namespace RfCompressionReplay.Tests;

public sealed class ExperimentApplicationTests
{
    [Fact]
    public void RunsSyntheticBenchmarkEndToEndAndGeneratesMetadataRichArtifacts()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = TestConfigFactory.CreateSyntheticBenchmarkConfig(
                experimentId: "m2-app",
                detectorName: DetectorCatalog.EnergyDetectorName,
                detectorMode: DetectorCatalog.EnergyDetectorMode,
                threshold: 0.5d,
                cases:
                [
                    new SyntheticCaseConfig("noise-only-baseline", "noise-only", ExperimentConfigValidator.NoiseOnlySourceType, null, null, null),
                    TestConfigFactory.CreateGaussianEmitterCase(0d),
                    TestConfigFactory.CreateOfdmLikeCase(-3d)
                ]);
            config = config with { OutputDirectory = Path.Combine(tempRoot, "artifacts") };

            var application = CreateApplication("2026-03-04T05:06:07Z");
            var result = application.Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");

            Assert.Equal("m2-app", result.Manifest.ExperimentId);
            Assert.Equal(config.TrialCount * 3, result.Result.Trials.Count);
            Assert.Equal(3, result.Result.Summary.Groups.Count);
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "summary.json")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "trials.csv")));
            Assert.Contains("Git commit could not be resolved.", result.Manifest.Warnings);
            Assert.All(result.Result.Trials, trial =>
            {
                Assert.NotEqual(0d, trial.Score);
                Assert.False(string.IsNullOrWhiteSpace(trial.ScenarioName));
                Assert.False(string.IsNullOrWhiteSpace(trial.TargetLabel));
                Assert.True(trial.WindowLength > 0);
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
            var config = TestConfigFactory.CreateSyntheticBenchmarkConfig("same-second", DetectorCatalog.EnergyDetectorName, DetectorCatalog.EnergyDetectorMode);
            var configPath = Path.Combine(tempRoot, "configs", "config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, "{}\n");
            var application = CreateApplication("2026-03-22T20:40:00Z");

            var firstRun = application.Run(config, configPath, "/does/not/exist");
            var secondRun = application.Run(config, configPath, "/does/not/exist");

            Assert.NotEqual(firstRun.RunDirectory, secondRun.RunDirectory);
            Assert.Equal(Path.Combine(tempRoot, "configs", "artifacts", "20260322T204000Z_same-second_seed7"), firstRun.RunDirectory);
            Assert.Equal(Path.Combine(tempRoot, "configs", "artifacts", "20260322T204000Z_same-second_seed7_2"), secondRun.RunDirectory);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Theory]
    [InlineData("m2.noise-only.ed.json", DetectorCatalog.EnergyDetectorName)]
    [InlineData("m2.gaussian-emitter.ed.json", DetectorCatalog.EnergyDetectorName)]
    [InlineData("m2.ofdm-like.cav.json", DetectorCatalog.CovarianceAbsoluteValueDetectorName)]
    [InlineData("m2.mixed.lzmsa-paper.json", DetectorCatalog.LzmsaPaperDetectorName)]
    public void M2SampleConfigsRunEndToEnd(string configFileName, string detectorName)
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
            var summaryJson = File.ReadAllText(Path.Combine(run.RunDirectory, "summary.json"));

            Assert.All(run.Result.Trials, trial =>
            {
                Assert.Equal(detectorName, trial.DetectorName);
                Assert.NotEqual(0d, trial.Score);
                Assert.False(string.IsNullOrWhiteSpace(trial.ScenarioName));
            });
            Assert.Contains(detectorName, trialsCsv);
            Assert.Contains("scenarioName", trialsCsv);
            Assert.Contains("groups", summaryJson);
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
}
