using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;

namespace RfCompressionReplay.Tests;

public sealed class ExperimentConfigTests
{
    [Fact]
    public void DeserializesM2SampleConfig()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs/m2.gaussian-emitter.ed.json"));
        var config = ExperimentConfigJson.Load(path);

        Assert.Equal("m2-gaussian-emitter-ed", config.ExperimentId);
        Assert.Equal(12345, config.Seed);
        Assert.Equal(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, config.Scenario.Name);
        Assert.Equal(6, config.TrialCount);
        Assert.Equal(DetectorCatalog.EnergyDetectorName, config.Detector.Name);
        Assert.Equal(DetectorCatalog.EnergyDetectorMode, config.Detector.Mode);
        Assert.NotNull(config.Benchmark);
        Assert.Single(config.Benchmark!.Cases);
        Assert.Null(config.Evaluation);
    }

    [Theory]
    [InlineData("m3.ofdm-sweep.json", 2)]
    [InlineData("m3.lzmsa-compressed-length.json", 1)]
    [InlineData("m3.lzmsa-normalized-compressed-length.json", 1)]
    [InlineData("m3.mixed.json", 5)]
    public void DeserializesM3SampleConfig(string configFileName, int expectedDetectorCount)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = ExperimentConfigJson.Load(path);

        Assert.NotNull(config.Evaluation);
        Assert.Equal(expectedDetectorCount, config.Evaluation!.Detectors.Count);
        Assert.NotEmpty(config.Evaluation.Tasks);
        Assert.NotEmpty(config.Evaluation.SnrDbValues);
        Assert.NotEmpty(config.Evaluation.WindowLengths);
    }

    [Theory]
    [InlineData("m5a1.compressed-stream-decomposition.json", 4)]
    [InlineData("m5a1.compressed-stream-decomposition-smoke.json", 4)]
    public void DeserializesM5A1Configs(string configFileName, int expectedDetectorCount)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = ExperimentConfigJson.Load(path);

        Assert.NotNull(config.Evaluation);
        Assert.Equal(expectedDetectorCount, config.Evaluation!.Detectors.Count);
        Assert.Contains(config.Evaluation.Detectors, detector => detector.Name == DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName);
        Assert.Equal("m5a1", config.ManifestMetadata.Tags!["milestone"]);
    }

    [Fact]
    public void ValidatorReturnsClearMessagesForInvalidSyntheticBenchmarkConfig()
    {
        var config = new ExperimentConfig(
            ExperimentId: "",
            ExperimentName: "",
            Seed: -1,
            OutputDirectory: "",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 0, 0),
            TrialCount: 0,
            Detector: new DetectorConfig("", 0.5, ""),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(0, new GaussianNoiseConfig(0d, 0d), Array.Empty<SyntheticCaseConfig>()),
            Evaluation: null,
            ManifestMetadata: ManifestMetadataConfig.Empty);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("ExperimentId is required.", errors);
        Assert.Contains("Seed must be zero or greater.", errors);
        Assert.Contains("TrialCount must be greater than zero.", errors);
        Assert.Contains("Scenario.SampleWindowCount must be greater than zero.", errors);
        Assert.Contains("Detector.Mode is required.", errors);
        Assert.Contains("Benchmark.BaseStreamLength must be greater than zero.", errors);
        Assert.Contains("Benchmark.Noise.StandardDeviation must be greater than zero.", errors);
        Assert.Contains("Benchmark.Cases must contain at least one synthetic case.", errors);
    }

    [Fact]
    public void ValidatorRejectsUnsupportedDetectorAndSourceIdentifiers()
    {
        var config = new ExperimentConfig(
            ExperimentId: "bad-identifiers",
            ExperimentName: "Bad identifiers",
            Seed: 1,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 1, 8),
            TrialCount: 1,
            Detector: new DetectorConfig("bogus-detector", 0.5, "bogus-mode"),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                64,
                new GaussianNoiseConfig(0d, 1d),
                [new SyntheticCaseConfig("bad-case", "signal-present", "bogus-source", 0d, null, null)]),
            Evaluation: null,
            ManifestMetadata: ManifestMetadataConfig.Empty);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("Detector.Name 'bogus-detector' is not supported in M3. Supported detectors: ed, cav, lzmsa-paper, lzmsa-compressed-length, lzmsa-normalized-compressed-length, lzmsa-mean-compressed-byte-value.", errors);
        Assert.Contains("Benchmark.Cases[0].SourceType 'bogus-source' is not supported in M3. Supported source types: noise-only, gaussian-emitter, ofdm-like.", errors);
    }

    [Theory]
    [InlineData(DetectorCatalog.LzmsaPaperDetectorName, "compressed-length", DetectorCatalog.LzmsaPaperDetectorMode)]
    [InlineData(DetectorCatalog.LzmsaCompressedLengthDetectorName, DetectorCatalog.LzmsaPaperDetectorMode, DetectorCatalog.LzmsaCompressedLengthDetectorMode)]
    [InlineData(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, DetectorCatalog.LzmsaCompressedLengthDetectorMode, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode)]
    [InlineData(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, DetectorCatalog.LzmsaCompressedLengthDetectorMode, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode)]
    public void ValidatorRejectsUnsupportedDetectorModes(string detectorName, string invalidMode, string expectedMode)
    {
        var config = TestConfigFactory.CreateSyntheticBenchmarkConfig(
            experimentId: "bad-mode",
            detectorName: detectorName,
            detectorMode: invalidMode);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains($"Detector.Mode '{invalidMode}' is not supported for detector '{detectorName}' in M3. Supported modes: {expectedMode}.", errors);
    }

    [Fact]
    public void ValidatorRejectsInvalidEvaluationSweeps()
    {
        var config = TestConfigFactory.CreateSyntheticEvaluationConfig("bad-eval") with
        {
            Evaluation = new EvaluationConfig(
                Tasks: [new BenchmarkTaskConfig(BenchmarkTaskCatalog.OfdmSignalPresentVsNoiseOnly, string.Empty, TestConfigFactory.CreateOfdmLikeCase(), TestConfigFactory.CreateNoiseOnlyCase())],
                Detectors: Array.Empty<DetectorConfig>(),
                SnrDbValues: Array.Empty<double>(),
                WindowLengths: [0],
                TrialCountPerCondition: 0),
        };

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("Evaluation.Tasks[0].Description is required.", errors);
        Assert.Contains("Evaluation.Detectors must contain at least one detector.", errors);
        Assert.Contains("Evaluation.SnrDbValues must contain at least one SNR value.", errors);
        Assert.Contains("Evaluation.WindowLengths[0] must be greater than zero.", errors);
        Assert.Contains("Evaluation.TrialCountPerCondition must be greater than zero.", errors);
    }

    [Theory]
    [InlineData(-0.01d)]
    [InlineData(255.01d)]
    public void ValidatorRejectsOutOfRangeMeanCompressedByteValueThreshold(double invalidThreshold)
    {
        var config = TestConfigFactory.CreateSyntheticBenchmarkConfig(
            experimentId: "bad-mean-threshold",
            detectorName: DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
            detectorMode: DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode,
            threshold: invalidThreshold);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains(
            $"Detector.Threshold for detector '{DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName}' must be between 0 and 255 inclusive because the mean compressed byte value is bounded to that range.",
            errors);
    }

    [Fact]
    public void ValidatorRejectsNonFiniteThresholds()
    {
        var config = TestConfigFactory.CreateSyntheticBenchmarkConfig(
            experimentId: "nan-threshold",
            detectorName: DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
            detectorMode: DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode,
            threshold: double.NaN);

        var errors = ExperimentConfigValidator.Validate(config);

        Assert.Contains("Detector.Threshold must be a finite number.", errors);
    }
}
