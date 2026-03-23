using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Execution;

namespace RfCompressionReplay.Tests;

public sealed class ArtifactRetentionTests
{
    [Fact]
    public void ValidatorRejectsUnsupportedArtifactRetentionMode()
    {
        var config = TestConfigFactory.CreateSyntheticEvaluationConfig("bad-retention") with
        {
            ArtifactRetentionMode = "archive-everything"
        };

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("ArtifactRetentionMode 'archive-everything' is not supported. Supported modes: full, milestone, smoke.", errors);
    }

    [Fact]
    public void MilestoneModeManifestRecordsRetentionAndRetainedArtifactPaths()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var config = TestConfigFactory.CreateSyntheticEvaluationConfig(
                experimentId: "milestone-retention",
                tasks: [TestConfigFactory.CreateOfdmTask(), TestConfigFactory.CreateGaussianEmitterTask()],
                detectors: TestConfigFactory.CreateM5A1CompressionDetectors(),
                snrDbValues: [-6d, 0d],
                windowLengths: [64],
                trialCountPerCondition: 2,
                artifactRetentionMode: ArtifactRetentionModes.Milestone) with
            {
                OutputDirectory = tempRoot,
                ExperimentName = "Milestone Retention Test",
                ManifestMetadata = new ManifestMetadataConfig(
                    "Milestone retention test run.",
                    "mx5-test",
                    new Dictionary<string, string>
                    {
                        ["milestone"] = "m5a1",
                        ["experimentType"] = "compressed-stream-decomposition-pass-1"
                    })
            };

            var run = CreateApplication("2026-03-23T10:00:00Z").Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");

            Assert.Equal(ArtifactRetentionModes.Milestone, run.Manifest.Retention.Mode);
            Assert.Contains("trials.csv", run.Manifest.Retention.OmittedArtifactKinds);
            Assert.Contains("roc_points.csv", run.Manifest.Retention.OmittedArtifactKinds);
            Assert.DoesNotContain("trials.csv", run.Manifest.ArtifactPaths);
            Assert.DoesNotContain("roc_points.csv", run.Manifest.ArtifactPaths);
            Assert.Contains("roc_points_compact.csv", run.Manifest.ArtifactPaths);
            Assert.Contains("m5a1_auc_comparison.csv", run.Manifest.ArtifactPaths);
            Assert.Contains("m5a1_findings.md", run.Manifest.ArtifactPaths);
            Assert.Contains("m5a1_delta_summary.csv", run.Manifest.ArtifactPaths);
            Assert.True(File.Exists(Path.Combine(run.RunDirectory, "roc_points_compact.csv")));
            Assert.False(File.Exists(Path.Combine(run.RunDirectory, "trials.csv")));
            Assert.False(File.Exists(Path.Combine(run.RunDirectory, "roc_points.csv")));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void SmokeModeRemainsMinimalWhileStillEmittingComparisonArtifacts()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var config = TestConfigFactory.CreateSyntheticEvaluationConfig(
                experimentId: "smoke-retention",
                tasks: [TestConfigFactory.CreateOfdmTask(), TestConfigFactory.CreateGaussianEmitterTask()],
                detectors: TestConfigFactory.CreateM4CompressionDetectors(),
                snrDbValues: [-6d],
                windowLengths: [64],
                trialCountPerCondition: 1,
                artifactRetentionMode: ArtifactRetentionModes.Smoke) with
            {
                OutputDirectory = tempRoot,
                ExperimentName = "Smoke Retention Test",
                ManifestMetadata = new ManifestMetadataConfig(
                    "Smoke retention test run.",
                    "mx5-smoke-test",
                    new Dictionary<string, string>
                    {
                        ["milestone"] = "m4",
                        ["experimentType"] = "score-identity-comparison"
                    })
            };

            var run = CreateApplication("2026-03-23T11:00:00Z").Run(config, Path.Combine(tempRoot, "config.json"), "/does/not/exist");

            Assert.Equal(ArtifactRetentionModes.Smoke, run.Manifest.Retention.Mode);
            Assert.DoesNotContain("trials.csv", run.Manifest.ArtifactPaths);
            Assert.DoesNotContain("roc_points.csv", run.Manifest.ArtifactPaths);
            Assert.DoesNotContain("roc_points_compact.csv", run.Manifest.ArtifactPaths);
            Assert.Contains("summary.json", run.Manifest.ArtifactPaths);
            Assert.Contains("summary.csv", run.Manifest.ArtifactPaths);
            Assert.Contains("m4_auc_comparison.csv", run.Manifest.ArtifactPaths);
            Assert.Contains("m4_findings.md", run.Manifest.ArtifactPaths);
            Assert.False(File.Exists(Path.Combine(run.RunDirectory, "trials.csv")));
            Assert.False(File.Exists(Path.Combine(run.RunDirectory, "roc_points.csv")));
            Assert.False(File.Exists(Path.Combine(run.RunDirectory, "roc_points_compact.csv")));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Theory]
    [InlineData("m5a1.compressed-stream-decomposition-smoke.json", ArtifactRetentionModes.Smoke)]
    [InlineData("m5a1.compressed-stream-decomposition.json", ArtifactRetentionModes.Milestone)]
    public void CheckedInRetentionConfigsRunEndToEnd(string configFileName, string expectedMode)
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var sampleConfigPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
            var config = ExperimentConfigJson.Load(sampleConfigPath) with { OutputDirectory = tempRoot };

            var run = CreateApplication("2026-03-23T12:00:00Z").Run(config, sampleConfigPath, "/does/not/exist");

            Assert.Equal(expectedMode, run.Manifest.Retention.Mode);
            Assert.True(File.Exists(Path.Combine(run.RunDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(run.RunDirectory, "summary.json")));
            Assert.True(File.Exists(Path.Combine(run.RunDirectory, "summary.csv")));

            if (string.Equals(expectedMode, ArtifactRetentionModes.Milestone, StringComparison.Ordinal))
            {
                Assert.True(File.Exists(Path.Combine(run.RunDirectory, "roc_points_compact.csv")));
                Assert.False(File.Exists(Path.Combine(run.RunDirectory, "roc_points.csv")));
                Assert.False(File.Exists(Path.Combine(run.RunDirectory, "trials.csv")));
            }
            else
            {
                Assert.False(File.Exists(Path.Combine(run.RunDirectory, "roc_points.csv")));
                Assert.False(File.Exists(Path.Combine(run.RunDirectory, "roc_points_compact.csv")));
                Assert.False(File.Exists(Path.Combine(run.RunDirectory, "trials.csv")));
            }
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    private static string CreateTempRoot()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        return tempRoot;
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
