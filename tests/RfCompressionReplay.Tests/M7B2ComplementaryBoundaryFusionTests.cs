using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Execution;

namespace RfCompressionReplay.Tests;

public sealed class M7B2ComplementaryBoundaryFusionTests
{
    [Fact]
    public void NormalizedAverageFusionChangeTraceAveragesPerTraceMinMaxNormalization()
    {
        var fused = M7BChangePointAnalyzer.ComputeNormalizedAverageFusionChangeTrace(
        [
            new double[] { 0d, 2d, 4d },
            new double[] { 10d, 10d, 20d },
        ]);

        Assert.Collection(
            fused,
            value => Assert.Equal(0d, value, 6),
            value => Assert.Equal(0.25d, value, 6),
            value => Assert.Equal(1d, value, 6));
    }

    [Fact]
    public void M7B2SmokeApplicationEmitsCompactArtifacts()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = TestConfigFactory.CreateM7B2ComplementaryBoundaryFusionConfig(
                "m7b2-smoke-test",
                streamCountPerCondition: 2,
                artifactRetentionMode: ArtifactRetentionModes.Smoke) with
            {
                OutputDirectory = tempRoot,
                ManifestMetadata = new ManifestMetadataConfig(
                    "Test M7b2 smoke run.",
                    "m7b2-test",
                    new Dictionary<string, string> { ["milestone"] = "m7b2", ["experimentType"] = "complementary-boundary-fusion" }),
            };

            var application = new M7B2ComplementaryBoundaryFusionExperimentApplication(
                new FixedRunClock(DateTimeOffset.Parse("2026-03-23T09:00:00Z")),
                new EnvironmentSummaryProvider(),
                new GitCommitResolver());
            var configPath = Path.Combine(tempRoot, "m7b2-config.json");
            File.WriteAllText(configPath, "{}\n");

            var runDirectory = application.Run(config, configPath, "/does/not/exist");

            Assert.True(File.Exists(Path.Combine(runDirectory, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "m7b2_boundary_comparison.csv")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "m7b2_fusion_summary.csv")));
            Assert.True(File.Exists(Path.Combine(runDirectory, "m7b2_findings.md")));
            Assert.Contains("signalId", File.ReadAllText(Path.Combine(runDirectory, "m7b2_boundary_comparison.csv")));
            Assert.Contains("fusedSignalId", File.ReadAllText(Path.Combine(runDirectory, "m7b2_fusion_summary.csv")));
            Assert.Contains("M7b2 Complementary Boundary Fusion Findings", File.ReadAllText(Path.Combine(runDirectory, "m7b2_findings.md")));
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
