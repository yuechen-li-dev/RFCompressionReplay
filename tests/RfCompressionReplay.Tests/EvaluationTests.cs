using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Execution;

namespace RfCompressionReplay.Tests;

public sealed class EvaluationTests
{
    [Fact]
    public void RocAucIsOneForPerfectSeparation()
    {
        var calculator = new RocAucCalculator();
        var result = calculator.Calculate(
        [
            new BinaryScoreRecord(true, 0.9d),
            new BinaryScoreRecord(true, 0.8d),
            new BinaryScoreRecord(false, 0.2d),
            new BinaryScoreRecord(false, 0.1d),
        ], ScoreOrientation.HigherScoreMorePositive);

        Assert.Equal(1d, result.Auc);
        Assert.Equal((0d, 0d), (result.Points[0].FalsePositiveRate, result.Points[0].TruePositiveRate));
        Assert.Equal((1d, 1d), (result.Points[^1].FalsePositiveRate, result.Points[^1].TruePositiveRate));
    }

    [Fact]
    public void RocAucIsOneForPerfectSeparationWhenLowerScoresMeanMorePositive()
    {
        var calculator = new RocAucCalculator();
        var result = calculator.Calculate(
        [
            new BinaryScoreRecord(true, 0.1d),
            new BinaryScoreRecord(true, 0.2d),
            new BinaryScoreRecord(false, 0.8d),
            new BinaryScoreRecord(false, 0.9d),
        ], ScoreOrientation.LowerScoreMorePositive);

        Assert.Equal(1d, result.Auc);
    }

    [Fact]
    public void RocAucIsZeroForInvertedOrderingWhenOrientationIsNotCorrected()
    {
        var calculator = new RocAucCalculator();
        var result = calculator.Calculate(
        [
            new BinaryScoreRecord(true, 0.1d),
            new BinaryScoreRecord(true, 0.2d),
            new BinaryScoreRecord(false, 0.8d),
            new BinaryScoreRecord(false, 0.9d),
        ], ScoreOrientation.HigherScoreMorePositive);

        Assert.Equal(0d, result.Auc);
    }

    [Fact]
    public void RocAucHandlesTiedScoresDeterministically()
    {
        var calculator = new RocAucCalculator();
        var result = calculator.Calculate(
        [
            new BinaryScoreRecord(true, 0.5d),
            new BinaryScoreRecord(false, 0.5d),
            new BinaryScoreRecord(true, 0.5d),
            new BinaryScoreRecord(false, 0.5d),
        ], ScoreOrientation.HigherScoreMorePositive);

        Assert.Equal(0.5d, result.Auc);
        Assert.Equal(2, result.Points.Count);
    }

    [Theory]
    [InlineData(DetectorCatalog.EnergyDetectorName, ScoreOrientation.HigherScoreMorePositive)]
    [InlineData(DetectorCatalog.CovarianceAbsoluteValueDetectorName, ScoreOrientation.HigherScoreMorePositive)]
    [InlineData(DetectorCatalog.LzmsaPaperDetectorName, ScoreOrientation.HigherScoreMorePositive)]
    [InlineData(DetectorCatalog.LzmsaCompressedLengthDetectorName, ScoreOrientation.LowerScoreMorePositive)]
    [InlineData(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, ScoreOrientation.LowerScoreMorePositive)]
    public void DetectorScoreOrientationIsExplicitlyDocumented(string detectorName, ScoreOrientation expectedOrientation)
    {
        Assert.Equal(expectedOrientation, DetectorCatalog.GetScoreOrientation(detectorName));
    }

    [Fact]
    public void EvaluationGroupsAucByTaskSnrAndWindowLength()
    {
        var config = TestConfigFactory.CreateSyntheticEvaluationConfig(
            experimentId: "grouping",
            tasks: [TestConfigFactory.CreateOfdmTask()],
            detectors: [new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode)],
            snrDbValues: [-6d, 0d],
            windowLengths: [64, 128],
            trialCountPerCondition: 2);

        var application = CreateApplication("2026-03-22T10:00:00Z");
        var tempRoot = CreateTempRoot();

        try
        {
            var run = application.Run(config with { OutputDirectory = tempRoot }, Path.Combine(tempRoot, "config.json"), "/does/not/exist");

            Assert.Equal(4, run.Result.Summary.Groups.Count);
            Assert.Equal(4, run.Result.Summary.Groups.Select(group => (group.ConditionSnrDb, group.WindowLength)).Distinct().Count());
            Assert.All(run.Result.Summary.Groups, group =>
            {
                Assert.Equal(2, group.PositiveCount);
                Assert.Equal(2, group.NegativeCount);
                Assert.NotNull(group.Auc);
            });
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Theory]
    [InlineData("m3.ofdm-sweep.json")]
    [InlineData("m3.gaussian-control.json")]
    [InlineData("m3.lzmsa-compressed-length.json")]
    [InlineData("m3.lzmsa-normalized-compressed-length.json")]
    [InlineData("m3.mixed.json")]
    public void M3SampleConfigsRunEndToEnd(string configFileName)
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var sampleConfigPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
            var config = ExperimentConfigJson.Load(sampleConfigPath) with { OutputDirectory = tempRoot };
            var application = CreateApplication("2026-03-22T21:15:00Z");

            var run = application.Run(config, sampleConfigPath, "/does/not/exist");
            var summaryCsv = File.ReadAllText(Path.Combine(run.RunDirectory, "summary.csv"));
            var rocCsv = File.ReadAllText(Path.Combine(run.RunDirectory, "roc_points.csv"));

            Assert.NotEmpty(run.Result.Evaluation!.RocPoints);
            Assert.All(run.Result.Summary.Groups, group => Assert.NotNull(group.Auc));
            Assert.Contains("auc", summaryCsv, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("tpr", rocCsv, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("scoreOrientation", rocCsv);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void EvaluationArtifactsAreDeterministicForSameSeedConfigAndClock()
    {
        var tempRoot = CreateTempRoot();
        Directory.CreateDirectory(tempRoot);

        try
        {
            var config = TestConfigFactory.CreateSyntheticEvaluationConfig(
                experimentId: "determinism",
                tasks: [TestConfigFactory.CreateGaussianEmitterTask()],
                detectors: [new DetectorConfig(DetectorCatalog.CovarianceAbsoluteValueDetectorName, 0.05d, DetectorCatalog.CovarianceAbsoluteValueDetectorMode)],
                snrDbValues: [-3d, 0d],
                windowLengths: [64],
                trialCountPerCondition: 3) with
            {
                OutputDirectory = tempRoot,
            };

            var application = CreateApplication("2026-03-22T23:00:00Z");
            var configPath = Path.Combine(tempRoot, "config.json");

            var first = application.Run(config, configPath, "/does/not/exist");
            var second = application.Run(config, configPath, "/does/not/exist");

            Assert.Equal(first.Result.Summary.Groups.Select(group => group.Auc).ToArray(), second.Result.Summary.Groups.Select(group => group.Auc).ToArray());
            Assert.Equal(File.ReadAllText(Path.Combine(first.RunDirectory, "roc_points.csv")), File.ReadAllText(Path.Combine(second.RunDirectory, "roc_points.csv")));
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
