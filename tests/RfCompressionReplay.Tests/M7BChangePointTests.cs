using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Execution;

namespace RfCompressionReplay.Tests;

public sealed class M7BChangePointTests
{
    [Fact]
    public void BoundaryEvaluatorMatchesNearestBoundariesAndCountsFalsePositives()
    {
        var metrics = M7BChangePointAnalyzer.EvaluateBoundaries([95, 212, 360], [100, 220], 20);

        Assert.True(metrics.OnsetHit);
        Assert.True(metrics.OffsetHit);
        Assert.Equal(5d, metrics.OnsetLocalizationError);
        Assert.Equal(8d, metrics.OffsetLocalizationError);
        Assert.Equal(-5d, metrics.OnsetDetectionDelay);
        Assert.Equal(-8d, metrics.OffsetDetectionDelay);
        Assert.Equal(1, metrics.FalsePositiveCount);
    }

    [Fact]
    public void BoundaryProposalFindsDominantAdjacentScoreJumps()
    {
        var proposals = M7BChangePointAnalyzer.ProposeBoundaries(
            [0.1d, 0.12d, 0.11d, 0.9d, 0.88d, 0.2d, 0.21d],
            streamLength: 448,
            windowLength: 64,
            stride: 32,
            peakThresholdMadMultiplier: 1.5d,
            minPeakSpacing: 64,
            maxBoundaryProposals: 3);

        Assert.Equal([96, 160], proposals);
    }

    [Fact]
    public void M7BSmokeApplicationEmitsCompactArtifacts()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = TestConfigFactory.CreateM7BChangePointConfig(
                "m7b-smoke-test",
                streamCountPerCondition: 2,
                artifactRetentionMode: ArtifactRetentionModes.Smoke) with
            {
                OutputDirectory = tempRoot,
                ManifestMetadata = new ManifestMetadataConfig(
                    "Test M7b smoke run.",
                    "m7b-test",
                    new Dictionary<string, string> { ["milestone"] = "m7b", ["experimentType"] = "change-point-usefulness" }),
            };

            var application = new M7BChangePointExperimentApplication(
                new FixedRunClock(DateTimeOffset.Parse("2026-03-23T08:30:00Z")),
                new EnvironmentSummaryProvider(),
                new GitCommitResolver());
            var configPath = Path.Combine(tempRoot, "m7b-config.json");
            File.WriteAllText(configPath, "{}\n");

            var runDirectory = application.Run(config, configPath, "/does/not/exist");

            Assert.True(File.Exists(Path.Combine(runDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "m7b_boundary_comparison.csv")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "m7b_task_summary.csv")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "m7b_findings.md")));
            Assert.Contains("onsetHitRate", File.ReadAllText(Path.Combine(runDirectory, "m7b_boundary_comparison.csv")));
            Assert.Contains("medianOnsetHitRate", File.ReadAllText(Path.Combine(runDirectory, "m7b_task_summary.csv")));
            Assert.Contains("M7b Change-Point / Segmentation Usefulness Findings", File.ReadAllText(Path.Combine(runDirectory, "m7b_findings.md")));
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    private sealed class FixedRunClock : IRunClock
    {
        public FixedRunClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
