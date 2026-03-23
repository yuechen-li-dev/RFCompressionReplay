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

    [Theory]
    [InlineData("m5b3.scale-handling-refinement.json", ArtifactRetentionModes.Milestone, 72, 4)]
    [InlineData("m5b3.scale-handling-refinement-smoke.json", ArtifactRetentionModes.Smoke, 4, 3)]
    public void DeserializesM5B3Configs(string configFileName, string expectedRetentionMode, int expectedTrialCountPerCondition, int expectedScaleCount)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = M5B3ExplorationConfigJson.Load(path);

        Assert.Equal(expectedRetentionMode, config.ArtifactRetentionMode);
        Assert.Equal(expectedTrialCountPerCondition, config.Evaluation.TrialCountPerCondition);
        Assert.Equal([86420, 97531, 24680], config.SeedPanel);
        Assert.Equal(expectedScaleCount, config.ScaleValues.Count);
        Assert.Equal(2, config.RepresentationFamilies.Count);
        Assert.Equal(4, config.Evaluation.Detectors.Count);
        Assert.Contains(config.RepresentationFamilies, family => string.Equals(family.Id, "raw-scaled", StringComparison.Ordinal));
        Assert.Contains(config.RepresentationFamilies, family => string.Equals(family.Id, "normalized-rms", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("m6a1.usefulness-mapping.json", ArtifactRetentionModes.Milestone, 48)]
    [InlineData("m6a1.usefulness-mapping-smoke.json", ArtifactRetentionModes.Smoke, 6)]
    public void DeserializesM6A1Configs(string configFileName, string expectedRetentionMode, int expectedTrialCountPerCondition)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = M6A1UsefulnessConfigJson.Load(path);

        Assert.Equal(expectedRetentionMode, config.ArtifactRetentionMode);
        Assert.Equal(expectedTrialCountPerCondition, config.Evaluation.TrialCountPerCondition);
        Assert.Equal([86420, 97531, 24680], config.SeedPanel);
        Assert.Equal(
            [
                BenchmarkTaskCatalog.StructuredBurstVsNoiseOnly,
                BenchmarkTaskCatalog.ColoredNuisanceVsWhiteNoise,
                BenchmarkTaskCatalog.EqualEnergyStructuredVsUnstructured,
            ],
            config.Evaluation.Tasks.Select(task => task.Name).ToArray());
        Assert.Equal(
            [
                DetectorCatalog.EnergyDetectorName,
                DetectorCatalog.CovarianceAbsoluteValueDetectorName,
                DetectorCatalog.LzmsaPaperDetectorName,
                DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
            ],
            config.Evaluation.Detectors.Select(detector => detector.Name).ToArray());
    }

    [Theory]
    [InlineData("m6a2.complementary-value-usefulness.json", ArtifactRetentionModes.Milestone, 48)]
    [InlineData("m6a2.complementary-value-usefulness-smoke.json", ArtifactRetentionModes.Smoke, 6)]
    public void DeserializesM6A2Configs(string configFileName, string expectedRetentionMode, int expectedTrialCountPerCondition)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = M6A2ComplementaryValueConfigJson.Load(path);

        Assert.Equal(expectedRetentionMode, config.ArtifactRetentionMode);
        Assert.Equal(expectedTrialCountPerCondition, config.Evaluation.TrialCountPerCondition);
        Assert.Equal([86420, 97531, 24680], config.SeedPanel);
        Assert.Equal(
            [
                BenchmarkTaskCatalog.EngineeredStructureVsNaturalCorrelation,
                BenchmarkTaskCatalog.EqualEnergyEngineeredStructureVsNaturalCorrelation,
            ],
            config.Evaluation.Tasks.Select(task => task.Name).ToArray());
        Assert.Equal(
            [
                DetectorCatalog.EnergyDetectorName,
                DetectorCatalog.CovarianceAbsoluteValueDetectorName,
                DetectorCatalog.LzmsaPaperDetectorName,
                DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
            ],
            config.Evaluation.Detectors.Select(detector => detector.Name).ToArray());
        Assert.Equal(
            [
                M6A2ComplementaryValueConfigValidator.BundleAId,
                M6A2ComplementaryValueConfigValidator.BundleBId,
            ],
            config.Bundles.Select(bundle => bundle.Id).ToArray());
    }

    [Theory]
    [InlineData("m7b2.complementary-boundary-fusion.json", ArtifactRetentionModes.Milestone, 8)]
    [InlineData("m7b2.complementary-boundary-fusion-smoke.json", ArtifactRetentionModes.Smoke, 2)]
    public void DeserializesM7B2Configs(string configFileName, string expectedRetentionMode, int expectedStreamCountPerCondition)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = M7B2ComplementaryBoundaryFusionConfigJson.Load(path);

        Assert.Equal(expectedRetentionMode, config.ArtifactRetentionMode);
        Assert.Equal(expectedStreamCountPerCondition, config.Evaluation.StreamCountPerCondition);
        Assert.Equal([86420, 97531, 24680], config.SeedPanel);
        Assert.Equal(
            [
                BenchmarkTaskCatalog.QuietToStructuredRegime,
                BenchmarkTaskCatalog.CorrelatedNuisanceToEngineeredStructure,
                BenchmarkTaskCatalog.StructureToStructureRegimeShift,
            ],
            config.Benchmark.Tasks.Select(task => task.Name).ToArray());
        Assert.Equal(
            [
                DetectorCatalog.EnergyDetectorName,
                DetectorCatalog.CovarianceAbsoluteValueDetectorName,
                DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
            ],
            config.Evaluation.Detectors.Select(detector => detector.Name).ToArray());
        Assert.Equal(
            [M7B2ComplementaryBoundaryFusionConfigValidator.RequiredFusionSignalId],
            config.Evaluation.Fusions.Select(fusion => fusion.SignalId).ToArray());
    }

    [Theory]
    [InlineData("m7b.change-point-usefulness.json", ArtifactRetentionModes.Milestone, 8)]
    [InlineData("m7b.change-point-usefulness-smoke.json", ArtifactRetentionModes.Smoke, 2)]
    public void DeserializesM7BConfigs(string configFileName, string expectedRetentionMode, int expectedStreamCountPerCondition)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../configs", configFileName));
        var config = M7BChangePointConfigJson.Load(path);

        Assert.Equal(expectedRetentionMode, config.ArtifactRetentionMode);
        Assert.Equal(expectedStreamCountPerCondition, config.Evaluation.StreamCountPerCondition);
        Assert.Equal([86420, 97531, 24680], config.SeedPanel);
        Assert.Equal(
            [
                BenchmarkTaskCatalog.QuietToStructuredRegime,
                BenchmarkTaskCatalog.CorrelatedNuisanceToEngineeredStructure,
                BenchmarkTaskCatalog.StructureToStructureRegimeShift,
            ],
            config.Benchmark.Tasks.Select(task => task.Name).ToArray());
        Assert.Equal(
            [
                DetectorCatalog.EnergyDetectorName,
                DetectorCatalog.CovarianceAbsoluteValueDetectorName,
                DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
            ],
            config.Evaluation.Detectors.Select(detector => detector.Name).ToArray());
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

        Assert.Contains(errors, error => error.Contains("Detector.Name 'bogus-detector' is not supported in M3.", StringComparison.Ordinal)
            && error.Contains(DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.Ordinal));
        Assert.Contains("Benchmark.Cases[0].SourceType 'bogus-source' is not supported in M3. Supported source types: noise-only, gaussian-emitter, ofdm-like, burst-ofdm-like, correlated-gaussian.", errors);
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

    [Fact]
    public void M5B3ValidatorRejectsMissingScalePanelAndFamilyShape()
    {
        var config = TestConfigFactory.CreateM5B3ExplorationConfig(
            "m5b3-bad",
            seedPanel: [7],
            scaleValues: [1d, 2d],
            representationFamilies:
            [
                new M5B3RepresentationFamilyConfig(
                    "raw-scaled",
                    "bad raw family",
                    new RepresentationConfig(1d, RepresentationFormats.Float32LittleEndian, RepresentationNormalizations.None, 1d)),
                new M5B3RepresentationFamilyConfig(
                    "normalized-rms",
                    "bad normalized family",
                    new RepresentationConfig(1d, RepresentationFormats.Float64LittleEndian, RepresentationNormalizations.None, 1d))
            ],
            detectors: TestConfigFactory.CreateM5A2CompressionDetectors());

        var errors = M5B3ExplorationConfigValidator.Validate(config);

        Assert.Contains("SeedPanel must contain at least two explicit seeds for M5b3 exploration.", errors);
        Assert.Contains("ScaleValues must contain a compact explicit scale panel of 3 or 4 values for M5b3.", errors);
        Assert.Contains("The raw-scaled family must use numericFormat float64-le.", errors);
        Assert.Contains("The normalized-rms family must use normalizationMode rms.", errors);
        Assert.Contains("M5b3 exploration requires the focused detector panel exactly: lzmsa-paper, lzmsa-mean-compressed-byte-value, lzmsa-compressed-byte-bucket-64-127-proportion, lzmsa-suffix-third-mean-compressed-byte-value.", errors);
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
    public void M6A1ValidatorRejectsWrongTaskPanelAndDetectorPanel()
    {
        var config = TestConfigFactory.CreateM6A1UsefulnessConfig(
            "m6a1-bad",
            tasks: [TestConfigFactory.CreateOfdmTask(), TestConfigFactory.CreateColoredNuisanceTask(), TestConfigFactory.CreateEqualEnergyTask()],
            detectors: TestConfigFactory.CreateM5A1CompressionDetectors());

        var errors = M6A1UsefulnessConfigValidator.Validate(config);

        Assert.Contains("M6a1 usefulness mapping requires the exact three-task suite in order: structured-burst-vs-noise-only, colored-nuisance-vs-white-noise, equal-energy-structured-vs-unstructured.", errors);
        Assert.Contains("M6a1 usefulness mapping requires the focused detector panel exactly: ed, cav, lzmsa-paper, lzmsa-rms-normalized-mean-compressed-byte-value.", errors);
    }

    [Fact]
    public void M6A2ValidatorRejectsWrongTaskBundleAndDetectorPanel()
    {
        var config = TestConfigFactory.CreateM6A2ComplementaryValueConfig(
            "m6a2-bad",
            tasks: [TestConfigFactory.CreateStructuredBurstTask(), TestConfigFactory.CreateEqualEnergyTask()],
            detectors: TestConfigFactory.CreateM5A1CompressionDetectors(),
            bundles:
            [
                new FeatureBundleConfig("wrong-a", "wrong", [DetectorCatalog.EnergyDetectorName]),
                new FeatureBundleConfig("wrong-b", "wrong", [DetectorCatalog.CovarianceAbsoluteValueDetectorName]),
            ]);

        var errors = M6A2ComplementaryValueConfigValidator.Validate(config);

        Assert.Contains("M6a2 complementary-value usefulness mapping requires the focused two-task suite in order: engineered-structure-vs-natural-correlation, equal-energy-engineered-structure-vs-natural-correlation.", errors);
        Assert.Contains("M6a2 complementary-value usefulness mapping requires the focused detector panel exactly: ed, cav, lzmsa-paper, lzmsa-rms-normalized-mean-compressed-byte-value.", errors);
        Assert.Contains("M6a2 complementary-value usefulness mapping requires exactly two bundles in order: bundle-a-ed-cav, bundle-b-ed-cav-rms-normalized-mean.", errors);
    }

    [Fact]
    public void M7B2ValidatorRejectsWrongTaskPanelDetectorPanelAndFusionRule()
    {
        var config = TestConfigFactory.CreateM7B2ComplementaryBoundaryFusionConfig(
            "m7b2-invalid",
            seedPanel: [86420, 97531],
            tasks: [TestConfigFactory.CreateQuietToStructuredStreamTask()],
            detectors:
            [
                new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode),
            ]) with
        {
            Evaluation = new M7B2BoundaryFusionEvaluationConfig(
                Detectors:
                [
                    new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode),
                ],
                Fusions:
                [
                    new M7B2FusionConfig("bad-fusion", "bad", "max-of-signals", [DetectorCatalog.EnergyDetectorName])
                ],
                SnrDbValues: [-9d, -3d, 0d],
                WindowLengths: [64, 128],
                StreamCountPerCondition: 2,
                MaxBoundaryProposals: 3,
                WindowStrideFraction: 0.5d,
                BoundaryToleranceWindowMultiple: 1.0d,
                MinPeakSpacingWindowMultiple: 1.0d,
                PeakThresholdMadMultiplier: 1.5d)
        };

        var errors = M7B2ComplementaryBoundaryFusionConfigValidator.Validate(config);

        Assert.Contains("SeedPanel must contain at least three explicit seeds for M7b2 complementary boundary fusion.", errors);
        Assert.Contains("M7b2 complementary boundary fusion requires the exact three-task stream suite in order: quiet-to-structured-regime, correlated-nuisance-to-engineered-structure, structure-to-structure-regime-shift.", errors);
        Assert.Contains("M7b2 complementary boundary fusion requires the focused detector panel exactly: ed, cav, lzmsa-rms-normalized-mean-compressed-byte-value.", errors);
        Assert.Contains("M7b2 complementary boundary fusion expects fusion SignalId 'ed-cav-rms-normalized-mean-fused'.", errors);
        Assert.Contains("M7b2 complementary boundary fusion expects fusion Rule 'normalized-adjacent-change-minmax-average'.", errors);
        Assert.Contains("M7b2 complementary boundary fusion expects fusion source detectors exactly: ed, cav, lzmsa-rms-normalized-mean-compressed-byte-value.", errors);
    }

    [Fact]
    public void M7BValidatorRejectsWrongTaskPanelAndDetectorPanel()
    {
        var config = TestConfigFactory.CreateM7BChangePointConfig(
            "m7b-bad",
            seedPanel: [7],
            tasks: [TestConfigFactory.CreateQuietToStructuredStreamTask(), TestConfigFactory.CreateStructureShiftTask()],
            detectors: TestConfigFactory.CreateM6A1Detectors());

        var errors = M7BChangePointConfigValidator.Validate(config);

        Assert.Contains("SeedPanel must contain at least three explicit seeds for M7b change-point usefulness mapping.", errors);
        Assert.Contains("M7b change-point usefulness mapping requires the exact three-task stream suite in order: quiet-to-structured-regime, correlated-nuisance-to-engineered-structure, structure-to-structure-regime-shift.", errors);
        Assert.Contains("M7b change-point usefulness mapping requires the focused detector panel exactly: ed, cav, lzmsa-rms-normalized-mean-compressed-byte-value.", errors);
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
