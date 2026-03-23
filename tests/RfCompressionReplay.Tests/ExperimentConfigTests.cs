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
        Assert.Equal(ArtifactRetentionModes.Full, config.ArtifactRetentionMode);
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
        Assert.Equal(ArtifactRetentionModes.Full, config.ArtifactRetentionMode);
        Assert.Equal(expectedDetectorCount, config.Evaluation!.Detectors.Count);
        Assert.NotEmpty(config.Evaluation.Tasks);
        Assert.NotEmpty(config.Evaluation.SnrDbValues);
        Assert.NotEmpty(config.Evaluation.WindowLengths);
    }

    [Theory]
    [InlineData("m5a1.compressed-stream-decomposition.json", 4)]
    [InlineData("m5a1.compressed-stream-decomposition-smoke.json", 4)]
    [InlineData("m5a2r.compressed-stream-decomposition.json", 9)]
    [InlineData("m5a2r.compressed-stream-decomposition-smoke.json", 9)]
    public void DeserializesM5A1Configs(string configFileName, int expectedDetectorCount)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = ExperimentConfigJson.Load(path);

        Assert.NotNull(config.Evaluation);
        Assert.Equal(expectedDetectorCount, config.Evaluation!.Detectors.Count);
        Assert.Contains(config.Evaluation.Detectors, detector => detector.Name == DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName);
        Assert.Equal(configFileName.Contains("smoke", StringComparison.Ordinal) ? ArtifactRetentionModes.Smoke : ArtifactRetentionModes.Milestone, config.ArtifactRetentionMode);
        Assert.Equal(configFileName.StartsWith("m5a2r", StringComparison.Ordinal) ? "m5a2" : "m5a1", config.ManifestMetadata.Tags!["milestone"]);
    }

    [Theory]
    [InlineData("m5a3.stability-confirmation.json", ArtifactRetentionModes.Milestone, 144)]
    [InlineData("m5a3.stability-confirmation-smoke.json", ArtifactRetentionModes.Smoke, 4)]
    public void DeserializesM5A3Configs(string configFileName, string expectedRetentionMode, int expectedTrialCountPerCondition)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = M5A3StabilityConfigJson.Load(path);

        Assert.Equal(expectedRetentionMode, config.ArtifactRetentionMode);
        Assert.Equal(expectedTrialCountPerCondition, config.Evaluation.TrialCountPerCondition);
        Assert.Equal([86420, 97531, 24680], config.SeedPanel);
        Assert.Equal(9, config.Evaluation.Detectors.Count);
    }

    [Theory]
    [InlineData("m5b1.representation-perturbation-exploration.json", ArtifactRetentionModes.Milestone, 72)]
    [InlineData("m5b1.representation-perturbation-exploration-smoke.json", ArtifactRetentionModes.Smoke, 4)]
    public void DeserializesM5B1Configs(string configFileName, string expectedRetentionMode, int expectedTrialCountPerCondition)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = M5B1ExplorationConfigJson.Load(path);

        Assert.Equal(expectedRetentionMode, config.ArtifactRetentionMode);
        Assert.Equal(expectedTrialCountPerCondition, config.Evaluation.TrialCountPerCondition);
        Assert.Equal([86420, 97531, 24680], config.SeedPanel);
        Assert.Equal(3, config.Perturbations.Count);
        Assert.Equal(4, config.Evaluation.Detectors.Count);
        Assert.Contains(config.Perturbations, perturbation => perturbation.Representation.NumericFormat == RepresentationFormats.Float32LittleEndian);
    }

    [Theory]
    [InlineData("m5b2.perturbation-axis-refinement.json", ArtifactRetentionModes.Milestone, 72, 4)]
    [InlineData("m5b2.perturbation-axis-refinement-smoke.json", ArtifactRetentionModes.Smoke, 4, 4)]
    public void DeserializesM5B2Configs(string configFileName, string expectedRetentionMode, int expectedTrialCountPerCondition, int expectedPerturbationCount)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = M5B2ExplorationConfigJson.Load(path);

        Assert.Equal(expectedRetentionMode, config.ArtifactRetentionMode);
        Assert.Equal(expectedTrialCountPerCondition, config.Evaluation.TrialCountPerCondition);
        Assert.Equal([86420, 97531, 24680], config.SeedPanel);
        Assert.Equal(expectedPerturbationCount, config.Perturbations.Count);
        Assert.Equal(4, config.Evaluation.Detectors.Count);
        Assert.Contains(config.Perturbations, perturbation => string.Equals(perturbation.AxisTag, M5B2PerturbationAxes.Scale, StringComparison.Ordinal));
        Assert.Contains(config.Perturbations, perturbation => string.Equals(perturbation.AxisTag, M5B2PerturbationAxes.Packing, StringComparison.Ordinal));
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
            ManifestMetadata: ManifestMetadataConfig.Empty,
            ArtifactRetentionMode: ArtifactRetentionModes.Full);

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

        Assert.Contains("Detector.Name 'bogus-detector' is not supported in M3. Supported detectors: ed, cav, lzmsa-paper, lzmsa-compressed-length, lzmsa-normalized-compressed-length, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-variance, lzmsa-compressed-byte-bucket-0-63-proportion, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-compressed-byte-bucket-128-191-proportion, lzmsa-compressed-byte-bucket-192-255-proportion, lzmsa-prefix-third-mean-compressed-byte-value, lzmsa-suffix-third-mean-compressed-byte-value.", errors);
        Assert.Contains("Benchmark.Cases[0].SourceType 'bogus-source' is not supported in M3. Supported source types: noise-only, gaussian-emitter, ofdm-like.", errors);
    }

    [Fact]
    public void M5A3ValidatorRejectsShortSeedPanels()
    {
        var config = TestConfigFactory.CreateM5A3StabilityConfig("m5a3-bad", seedPanel: [7, 7]);

        var errors = M5A3StabilityConfigValidator.Validate(config);

        Assert.Contains("SeedPanel must contain at least three explicit seeds for M5a3 stability confirmation.", errors);
        Assert.Contains("SeedPanel seeds must be distinct.", errors);
    }

    [Fact]
    public void M5B1ValidatorRejectsMissingFocusedPanelAndPerturbationShape()
    {
        var config = TestConfigFactory.CreateM5B1ExplorationConfig(
            "m5b1-bad",
            seedPanel: [7],
            perturbations:
            [
                new M5B1PerturbationConfig("baseline-a", "baseline", new RepresentationConfig(1d, RepresentationFormats.Float64LittleEndian)),
                new M5B1PerturbationConfig("baseline-b", "duplicate baseline", new RepresentationConfig(1d, RepresentationFormats.Float64LittleEndian)),
                new M5B1PerturbationConfig("float32", "float32", new RepresentationConfig(1d, RepresentationFormats.Float32LittleEndian)),
            ],
            detectors: TestConfigFactory.CreateM5A2CompressionDetectors());

        var errors = M5B1ExplorationConfigValidator.Validate(config);

        Assert.Contains("SeedPanel must contain at least two explicit seeds for M5b1 exploration.", errors);
        Assert.Contains("Perturbations must include exactly one baseline representation using sampleScale 1.0 and numericFormat float64-le.", errors);
        Assert.Contains("Perturbations must include exactly one numeric scaling perturbation using float64-le serialization.", errors);
        Assert.Contains("M5b1 exploration requires the focused detector panel exactly: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-suffix-third-mean-compressed-byte-value.", errors);
    }

    [Fact]
    public void M5B2ValidatorRejectsMissingAxisSeparationAndFocusedPanel()
    {
        var config = TestConfigFactory.CreateM5B2ExplorationConfig(
            "m5b2-bad",
            seedPanel: [7],
            perturbations:
            [
                new M5B2PerturbationConfig("baseline", M5B2PerturbationAxes.Baseline, "baseline", new RepresentationConfig(1d, RepresentationFormats.Float64LittleEndian)),
                new M5B2PerturbationConfig("scale-half", M5B2PerturbationAxes.Scale, "not isolated", new RepresentationConfig(0.5d, RepresentationFormats.Float32LittleEndian)),
                new M5B2PerturbationConfig("float32", M5B2PerturbationAxes.Packing, "not isolated", new RepresentationConfig(0.5d, RepresentationFormats.Float32LittleEndian))
            ],
            detectors: TestConfigFactory.CreateM5A2CompressionDetectors());

        var errors = M5B2ExplorationConfigValidator.Validate(config);

        Assert.Contains("SeedPanel must contain at least two explicit seeds for M5b2 exploration.", errors);
        Assert.Contains("Scale-only perturbation must change sampleScale away from 1.0 while keeping numericFormat float64-le.", errors);
        Assert.Contains("Packing-only perturbation must keep sampleScale 1.0 while changing numericFormat away from float64-le.", errors);
        Assert.Contains("M5b2 exploration requires the focused detector panel exactly: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-suffix-third-mean-compressed-byte-value.", errors);
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
