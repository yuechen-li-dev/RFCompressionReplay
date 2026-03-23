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
                    TestConfigFactory.CreateNoiseOnlyCase(),
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
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "summary.csv")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "trials.csv")));
            Assert.True(File.Exists(Path.Combine(result.RunDirectory, "roc_points.csv")));
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

    [Fact]
    public void GeneratesM4ComparisonArtifactsForCompressionIdentityRun()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = TestConfigFactory.CreateSyntheticEvaluationConfig(
                experimentId: "m4-test",
                tasks: [TestConfigFactory.CreateOfdmTask(), TestConfigFactory.CreateGaussianEmitterTask()],
                detectors: TestConfigFactory.CreateM4CompressionDetectors(),
                snrDbValues: [-6d, 0d],
                windowLengths: [64],
                trialCountPerCondition: 2) with
            {
                OutputDirectory = tempRoot,
                ExperimentName = "M4 Comparison Test",
                ManifestMetadata = new ManifestMetadataConfig(
                    "Test M4 score identity comparison run.",
                    "m4-test",
                    new Dictionary<string, string> { ["milestone"] = "m4", ["experimentType"] = "score-identity-comparison" }),
            };

            var application = CreateApplication("2026-03-22T05:06:07Z");
            var result = application.Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");
            var comparisonPath = Path.Combine(result.RunDirectory, "m4_auc_comparison.csv");
            var findingsPath = Path.Combine(result.RunDirectory, "m4_findings.md");

            Assert.True(File.Exists(comparisonPath));
            Assert.True(File.Exists(findingsPath));
            Assert.Contains("m4_auc_comparison.csv", result.Manifest.ArtifactPaths);
            Assert.Contains("m4_findings.md", result.Manifest.ArtifactPaths);
            Assert.Contains("aucLzmsaPaper", File.ReadAllText(comparisonPath));
            Assert.Contains("M4 Score-Identity Comparison Findings", File.ReadAllText(findingsPath));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void GeneratesM4aComparisonArtifactsForConfirmationRun()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = TestConfigFactory.CreateSyntheticEvaluationConfig(
                experimentId: "m4a-test",
                tasks: [TestConfigFactory.CreateOfdmTask(), TestConfigFactory.CreateGaussianEmitterTask()],
                detectors: TestConfigFactory.CreateM4CompressionDetectors(),
                snrDbValues: [-6d, 0d],
                windowLengths: [64],
                trialCountPerCondition: 2) with
            {
                OutputDirectory = tempRoot,
                ExperimentName = "M4a Comparison Test",
                ManifestMetadata = new ManifestMetadataConfig(
                    "Test M4a confirmation rerun.",
                    "m4a-test",
                    new Dictionary<string, string> { ["milestone"] = "m4a", ["experimentType"] = "score-identity-confirmation-rerun" }),
            };

            var application = CreateApplication("2026-03-22T05:16:07Z");
            var result = application.Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");
            var comparisonPath = Path.Combine(result.RunDirectory, "m4a_auc_comparison.csv");
            var findingsPath = Path.Combine(result.RunDirectory, "m4a_findings.md");

            Assert.True(File.Exists(comparisonPath));
            Assert.True(File.Exists(findingsPath));
            Assert.Contains("m4a_auc_comparison.csv", result.Manifest.ArtifactPaths);
            Assert.Contains("m4a_findings.md", result.Manifest.ArtifactPaths);
            Assert.Contains("aucLzmsaPaper", File.ReadAllText(comparisonPath));
            Assert.Contains("M4a Score-Identity Comparison Findings", File.ReadAllText(findingsPath));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void GeneratesM5A1ComparisonArtifactsForDecompositionRun()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = TestConfigFactory.CreateSyntheticEvaluationConfig(
                experimentId: "m5a1-test",
                tasks: [TestConfigFactory.CreateOfdmTask(), TestConfigFactory.CreateGaussianEmitterTask()],
                detectors: TestConfigFactory.CreateM5A1CompressionDetectors(),
                snrDbValues: [-6d, 0d],
                windowLengths: [64],
                trialCountPerCondition: 2) with
            {
                OutputDirectory = tempRoot,
                ExperimentName = "M5a1 Comparison Test",
                ManifestMetadata = new ManifestMetadataConfig(
                    "Test M5a1 compressed-stream decomposition run.",
                    "m5a1-test",
                    new Dictionary<string, string> { ["milestone"] = "m5a1", ["experimentType"] = "compressed-stream-decomposition-pass-1" }),
            };

            var application = CreateApplication("2026-03-22T05:26:07Z");
            var result = application.Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");
            var comparisonPath = Path.Combine(result.RunDirectory, "m5a1_auc_comparison.csv");
            var findingsPath = Path.Combine(result.RunDirectory, "m5a1_findings.md");
            var deltaSummaryPath = Path.Combine(result.RunDirectory, "m5a1_delta_summary.csv");

            Assert.True(File.Exists(comparisonPath));
            Assert.True(File.Exists(findingsPath));
            Assert.True(File.Exists(deltaSummaryPath));
            Assert.Contains("m5a1_auc_comparison.csv", result.Manifest.ArtifactPaths);
            Assert.Contains("m5a1_findings.md", result.Manifest.ArtifactPaths);
            Assert.Contains("m5a1_delta_summary.csv", result.Manifest.ArtifactPaths);
            Assert.Contains("aucLzmsaMeanCompressedByteValue", File.ReadAllText(comparisonPath));
            Assert.Contains("M5a1 Compressed-Stream Decomposition Findings", File.ReadAllText(findingsPath));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void GeneratesM5A2ComparisonArtifactsForRelandRun()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = TestConfigFactory.CreateSyntheticEvaluationConfig(
                experimentId: "m5a2r-test",
                tasks: [TestConfigFactory.CreateOfdmTask(), TestConfigFactory.CreateGaussianEmitterTask()],
                detectors: TestConfigFactory.CreateM5A2CompressionDetectors(),
                snrDbValues: [-6d, 0d],
                windowLengths: [64],
                trialCountPerCondition: 2) with
            {
                OutputDirectory = tempRoot,
                ExperimentName = "M5a2 Reland Test",
                ManifestMetadata = new ManifestMetadataConfig(
                    "Test M5a2 compressed-stream decomposition reland run.",
                    "m5a2-test",
                    new Dictionary<string, string> { ["milestone"] = "m5a2", ["experimentType"] = "compressed-stream-decomposition-pass-2" }),
            };

            var application = CreateApplication("2026-03-23T05:36:07Z");
            var result = application.Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");
            var comparisonPath = Path.Combine(result.RunDirectory, "m5a2_auc_comparison.csv");
            var findingsPath = Path.Combine(result.RunDirectory, "m5a2_findings.md");
            var deltaSummaryPath = Path.Combine(result.RunDirectory, "m5a2_delta_summary.csv");

            Assert.True(File.Exists(comparisonPath));
            Assert.True(File.Exists(findingsPath));
            Assert.True(File.Exists(deltaSummaryPath));
            Assert.Contains("m5a2_auc_comparison.csv", result.Manifest.ArtifactPaths);
            Assert.Contains("m5a2_findings.md", result.Manifest.ArtifactPaths);
            Assert.Contains("m5a2_delta_summary.csv", result.Manifest.ArtifactPaths);
            Assert.Contains("aucLzmsaSuffixThirdMeanCompressedByteValue", File.ReadAllText(comparisonPath));
            Assert.Contains("M5a2 Compressed-Stream Decomposition Findings", File.ReadAllText(findingsPath));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void GeneratesM5A3StabilityArtifactsForSeedPanelRun()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = TestConfigFactory.CreateM5A3StabilityConfig(
                experimentId: "m5a3-test",
                trialCountPerCondition: 2) with
            {
                OutputDirectory = tempRoot,
                ExperimentName = "M5a3 Stability Test",
                ManifestMetadata = new ManifestMetadataConfig(
                    "Test M5a3 stability confirmation run.",
                    "m5a3-test",
                    new Dictionary<string, string> { ["milestone"] = "m5a3", ["experimentType"] = "stability-confirmation" })
            };

            var baseApplication = CreateApplication("2026-03-23T05:46:07Z");
            var stabilityApplication = new M5A3StabilityExperimentApplication(
                new FixedRunClock(DateTimeOffset.Parse("2026-03-23T05:46:07Z")),
                baseApplication,
                new EnvironmentSummaryProvider(),
                new GitCommitResolver());
            var runDirectory = stabilityApplication.Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");

            Assert.True(File.Exists(Path.Combine(runDirectory, "m5a3_auc_comparison.csv")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "m5a3_delta_summary.csv")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "m5a3_stability_summary.csv")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "m5a3_findings.md")));
            Assert.Contains("seed,taskName,conditionSnrDb", File.ReadAllText(Path.Combine(runDirectory, "m5a3_auc_comparison.csv")));
            Assert.Contains("closestNeighborCount", File.ReadAllText(Path.Combine(runDirectory, "m5a3_stability_summary.csv")));
            Assert.Contains("M5a3 Stability Confirmation Findings", File.ReadAllText(Path.Combine(runDirectory, "m5a3_findings.md")));
            Assert.Equal(3, Directory.GetDirectories(Path.Combine(runDirectory, "seed-runs")).Length);
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
