using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;

namespace RfCompressionReplay.Tests;

internal static class TestConfigFactory
{
    public static ExperimentConfig CreateSyntheticBenchmarkConfig(
        string experimentId,
        string detectorName,
        string detectorMode,
        double threshold = 0d,
        IReadOnlyList<SyntheticCaseConfig>? cases = null,
        string artifactRetentionMode = ArtifactRetentionModes.Full)
    {
        return new ExperimentConfig(
            ExperimentId: experimentId,
            ExperimentName: "M2 Synthetic Test",
            Seed: 7,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 64),
            TrialCount: 4,
            Detector: new DetectorConfig(detectorName, threshold, detectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 2048,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: cases ??
                [
                    new SyntheticCaseConfig(
                        Name: "noise-only-baseline",
                        TargetLabel: "noise-only",
                        SourceType: ExperimentConfigValidator.NoiseOnlySourceType,
                        SnrDb: null,
                        GaussianEmitter: null,
                        OfdmLike: null)
                ]),
            Evaluation: null,
            ManifestMetadata: new ManifestMetadataConfig("note", "m2", new Dictionary<string, string> { ["suite"] = "tests" }),
            ArtifactRetentionMode: artifactRetentionMode);
    }

    public static ExperimentConfig CreateSyntheticEvaluationConfig(
        string experimentId,
        IReadOnlyList<BenchmarkTaskConfig>? tasks = null,
        IReadOnlyList<DetectorConfig>? detectors = null,
        IReadOnlyList<double>? snrDbValues = null,
        IReadOnlyList<int>? windowLengths = null,
        int trialCountPerCondition = 3,
        string artifactRetentionMode = ArtifactRetentionModes.Full)
    {
        return new ExperimentConfig(
            ExperimentId: experimentId,
            ExperimentName: "M3 Synthetic Evaluation Test",
            Seed: 7,
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 64),
            TrialCount: 4,
            Detector: new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 2048,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: Array.Empty<SyntheticCaseConfig>()),
            Evaluation: new EvaluationConfig(
                Tasks: tasks ?? [CreateOfdmTask()],
                Detectors: detectors ?? [new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode)],
                SnrDbValues: snrDbValues ?? [-6d, 0d],
                WindowLengths: windowLengths ?? [64, 128],
                TrialCountPerCondition: trialCountPerCondition),
            ManifestMetadata: new ManifestMetadataConfig("note", "m3", new Dictionary<string, string> { ["suite"] = "tests" }),
            ArtifactRetentionMode: artifactRetentionMode);
    }

    public static IReadOnlyList<DetectorConfig> CreateM4CompressionDetectors()
    {
        return
        [
            new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedLengthDetectorName, 64d, DetectorCatalog.LzmsaCompressedLengthDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, 0.25d, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode),
        ];
    }

    public static IReadOnlyList<DetectorConfig> CreateM5A1CompressionDetectors()
    {
        return
        [
            new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedLengthDetectorName, 64d, DetectorCatalog.LzmsaCompressedLengthDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, 0.25d, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, 128d, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode),
        ];
    }

    public static IReadOnlyList<DetectorConfig> CreateM6A1Detectors()
    {
        return
        [
            new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode),
            new DetectorConfig(DetectorCatalog.CovarianceAbsoluteValueDetectorName, 0d, DetectorCatalog.CovarianceAbsoluteValueDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, 128d, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorMode),
        ];
    }

    public static IReadOnlyList<FeatureBundleConfig> CreateM6A2Bundles()
    {
        return
        [
            new FeatureBundleConfig(
                M6A2ComplementaryValueConfigValidator.BundleAId,
                "Reference bundle using only ED and CAV.",
                [DetectorCatalog.EnergyDetectorName, DetectorCatalog.CovarianceAbsoluteValueDetectorName]),
            new FeatureBundleConfig(
                M6A2ComplementaryValueConfigValidator.BundleBId,
                "Reference ED+CAV bundle plus RMS-normalized mean compressed byte value.",
                [DetectorCatalog.EnergyDetectorName, DetectorCatalog.CovarianceAbsoluteValueDetectorName, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName]),
        ];
    }

    public static IReadOnlyList<DetectorConfig> CreateM5A2CompressionDetectors()
    {
        return
        [
            new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, 128d, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedByteVarianceDetectorName, 1000d, DetectorCatalog.LzmsaCompressedByteVarianceDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorName, 0.25d, DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, 0.25d, DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorName, 0.25d, DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorName, 0.25d, DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorName, 128d, DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, 128d, DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorMode),
        ];
    }

    public static M5A3StabilityConfig CreateM5A3StabilityConfig(
        string experimentId,
        IReadOnlyList<int>? seedPanel = null,
        IReadOnlyList<BenchmarkTaskConfig>? tasks = null,
        IReadOnlyList<DetectorConfig>? detectors = null,
        IReadOnlyList<double>? snrDbValues = null,
        IReadOnlyList<int>? windowLengths = null,
        int trialCountPerCondition = 3,
        string artifactRetentionMode = ArtifactRetentionModes.Milestone)
    {
        return new M5A3StabilityConfig(
            ExperimentId: experimentId,
            ExperimentName: "M5a3 Stability Test",
            SeedPanel: seedPanel ?? [7, 11, 13],
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 64),
            TrialCount: 4,
            Detector: new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 2048,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: Array.Empty<SyntheticCaseConfig>()),
            Evaluation: new EvaluationConfig(
                Tasks: tasks ?? [CreateOfdmTask(), CreateGaussianEmitterTask()],
                Detectors: detectors ?? CreateM5A2CompressionDetectors(),
                SnrDbValues: snrDbValues ?? [-6d, 0d],
                WindowLengths: windowLengths ?? [64],
                TrialCountPerCondition: trialCountPerCondition),
            ManifestMetadata: new ManifestMetadataConfig("note", "m5a3", new Dictionary<string, string> { ["suite"] = "tests", ["milestone"] = "m5a3" }),
            ArtifactRetentionMode: artifactRetentionMode);
    }

    public static IReadOnlyList<DetectorConfig> CreateM5B1CompressionDetectors()
    {
        return
        [
            new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, 128d, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, 0.25d, DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorMode),
            new DetectorConfig(DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, 128d, DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorMode),
        ];
    }

    public static IReadOnlyList<M5B1PerturbationConfig> CreateM5B1Perturbations()
    {
        return
        [
            new M5B1PerturbationConfig(
                "baseline",
                "Baseline representation: sampleScale 1.0 with float64-le serialization.",
                new RepresentationConfig(1d, RepresentationFormats.Float64LittleEndian)),
            new M5B1PerturbationConfig(
                "scale-half",
                "Numeric scaling perturbation: multiply each sample by 0.5 before float64-le serialization.",
                new RepresentationConfig(0.5d, RepresentationFormats.Float64LittleEndian)),
            new M5B1PerturbationConfig(
                "float32",
                "Serialization perturbation: keep baseline scaling but cast each sample to float32 before little-endian serialization.",
                new RepresentationConfig(1d, RepresentationFormats.Float32LittleEndian)),
        ];
    }

    public static M5B1ExplorationConfig CreateM5B1ExplorationConfig(
        string experimentId,
        IReadOnlyList<int>? seedPanel = null,
        IReadOnlyList<M5B1PerturbationConfig>? perturbations = null,
        IReadOnlyList<BenchmarkTaskConfig>? tasks = null,
        IReadOnlyList<DetectorConfig>? detectors = null,
        IReadOnlyList<double>? snrDbValues = null,
        IReadOnlyList<int>? windowLengths = null,
        int trialCountPerCondition = 3,
        string artifactRetentionMode = ArtifactRetentionModes.Milestone)
    {
        return new M5B1ExplorationConfig(
            ExperimentId: experimentId,
            ExperimentName: "M5b1 Exploration Test",
            SeedPanel: seedPanel ?? [7, 11, 13],
            Perturbations: perturbations ?? CreateM5B1Perturbations(),
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 64),
            TrialCount: 4,
            Detector: new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 2048,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: Array.Empty<SyntheticCaseConfig>()),
            Evaluation: new EvaluationConfig(
                Tasks: tasks ?? [CreateOfdmTask(), CreateGaussianEmitterTask()],
                Detectors: detectors ?? CreateM5B1CompressionDetectors(),
                SnrDbValues: snrDbValues ?? [-6d, 0d],
                WindowLengths: windowLengths ?? [64],
                TrialCountPerCondition: trialCountPerCondition),
            ManifestMetadata: new ManifestMetadataConfig("note", "m5b1", new Dictionary<string, string> { ["suite"] = "tests", ["milestone"] = "m5b1" }),
            ArtifactRetentionMode: artifactRetentionMode);
    }

    public static IReadOnlyList<M5B2PerturbationConfig> CreateM5B2Perturbations()
    {
        return
        [
            new M5B2PerturbationConfig(
                "baseline",
                M5B2PerturbationAxes.Baseline,
                "Baseline representation: sampleScale 1.0 with float64-le serialization.",
                new RepresentationConfig(1d, RepresentationFormats.Float64LittleEndian)),
            new M5B2PerturbationConfig(
                "scale-half",
                M5B2PerturbationAxes.Scale,
                "Scale-only perturbation: multiply each sample by 0.5 before float64-le serialization, with no extra clipping or normalization.",
                new RepresentationConfig(0.5d, RepresentationFormats.Float64LittleEndian)),
            new M5B2PerturbationConfig(
                "float32",
                M5B2PerturbationAxes.Packing,
                "Packing-only perturbation: keep scale 1.0 but cast each sample to float32 before little-endian serialization.",
                new RepresentationConfig(1d, RepresentationFormats.Float32LittleEndian)),
            new M5B2PerturbationConfig(
                "scale-half-float32",
                M5B2PerturbationAxes.Combined,
                "Combined perturbation: multiply each sample by 0.5, then cast to float32 before little-endian serialization.",
                new RepresentationConfig(0.5d, RepresentationFormats.Float32LittleEndian)),
        ];
    }

    public static M5B2ExplorationConfig CreateM5B2ExplorationConfig(
        string experimentId,
        IReadOnlyList<int>? seedPanel = null,
        IReadOnlyList<M5B2PerturbationConfig>? perturbations = null,
        IReadOnlyList<BenchmarkTaskConfig>? tasks = null,
        IReadOnlyList<DetectorConfig>? detectors = null,
        IReadOnlyList<double>? snrDbValues = null,
        IReadOnlyList<int>? windowLengths = null,
        int trialCountPerCondition = 3,
        string artifactRetentionMode = ArtifactRetentionModes.Milestone)
    {
        return new M5B2ExplorationConfig(
            ExperimentId: experimentId,
            ExperimentName: "M5b2 Exploration Test",
            SeedPanel: seedPanel ?? [7, 11, 13],
            Perturbations: perturbations ?? CreateM5B2Perturbations(),
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 64),
            TrialCount: 4,
            Detector: new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 2048,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: Array.Empty<SyntheticCaseConfig>()),
            Evaluation: new EvaluationConfig(
                Tasks: tasks ?? [CreateOfdmTask(), CreateGaussianEmitterTask()],
                Detectors: detectors ?? CreateM5B1CompressionDetectors(),
                SnrDbValues: snrDbValues ?? [-6d, 0d],
                WindowLengths: windowLengths ?? [64],
                TrialCountPerCondition: trialCountPerCondition),
            ManifestMetadata: new ManifestMetadataConfig("note", "m5b2", new Dictionary<string, string> { ["suite"] = "tests", ["milestone"] = "m5b2" }),
            ArtifactRetentionMode: artifactRetentionMode);
    }

    public static IReadOnlyList<M5B3RepresentationFamilyConfig> CreateM5B3RepresentationFamilies()
    {
        return
        [
            new M5B3RepresentationFamilyConfig(
                "raw-scaled",
                "Raw-scaled float64-le serialization with no normalization.",
                new RepresentationConfig(1d, RepresentationFormats.Float64LittleEndian, RepresentationNormalizations.None, 1d)),
            new M5B3RepresentationFamilyConfig(
                "normalized-rms",
                "Per-window RMS normalization to target RMS 1.0 after scaling, then float64-le serialization.",
                new RepresentationConfig(1d, RepresentationFormats.Float64LittleEndian, RepresentationNormalizations.Rms, 1d)),
        ];
    }

    public static M5B3ExplorationConfig CreateM5B3ExplorationConfig(
        string experimentId,
        IReadOnlyList<int>? seedPanel = null,
        IReadOnlyList<double>? scaleValues = null,
        IReadOnlyList<M5B3RepresentationFamilyConfig>? representationFamilies = null,
        IReadOnlyList<BenchmarkTaskConfig>? tasks = null,
        IReadOnlyList<DetectorConfig>? detectors = null,
        IReadOnlyList<double>? snrDbValues = null,
        IReadOnlyList<int>? windowLengths = null,
        int trialCountPerCondition = 3,
        string artifactRetentionMode = ArtifactRetentionModes.Milestone)
    {
        return new M5B3ExplorationConfig(
            ExperimentId: experimentId,
            ExperimentName: "M5b3 Exploration Test",
            SeedPanel: seedPanel ?? [7, 11, 13],
            ScaleValues: scaleValues ?? [0.5d, 1d, 2d],
            RepresentationFamilies: representationFamilies ?? CreateM5B3RepresentationFamilies(),
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 64),
            TrialCount: 4,
            Detector: new DetectorConfig(DetectorCatalog.LzmsaPaperDetectorName, 25000d, DetectorCatalog.LzmsaPaperDetectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 2048,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: Array.Empty<SyntheticCaseConfig>()),
            Evaluation: new EvaluationConfig(
                Tasks: tasks ?? [CreateOfdmTask(), CreateGaussianEmitterTask()],
                Detectors: detectors ?? CreateM5B1CompressionDetectors(),
                SnrDbValues: snrDbValues ?? [-6d, 0d],
                WindowLengths: windowLengths ?? [64],
                TrialCountPerCondition: trialCountPerCondition),
            ManifestMetadata: new ManifestMetadataConfig("note", "m5b3", new Dictionary<string, string> { ["suite"] = "tests", ["milestone"] = "m5b3" }),
            ArtifactRetentionMode: artifactRetentionMode);
    }

    public static M6A1UsefulnessConfig CreateM6A1UsefulnessConfig(
        string experimentId,
        IReadOnlyList<int>? seedPanel = null,
        IReadOnlyList<BenchmarkTaskConfig>? tasks = null,
        IReadOnlyList<DetectorConfig>? detectors = null,
        IReadOnlyList<double>? snrDbValues = null,
        IReadOnlyList<int>? windowLengths = null,
        int trialCountPerCondition = 12,
        string artifactRetentionMode = ArtifactRetentionModes.Milestone)
    {
        return new M6A1UsefulnessConfig(
            ExperimentId: experimentId,
            ExperimentName: "M6a1 Usefulness Mapping Test",
            SeedPanel: seedPanel ?? [86420, 97531, 24680],
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 128),
            TrialCount: 4,
            Detector: new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 4096,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: Array.Empty<SyntheticCaseConfig>()),
            Evaluation: new EvaluationConfig(
                Tasks: tasks ?? [CreateStructuredBurstTask(), CreateColoredNuisanceTask(), CreateEqualEnergyTask()],
                Detectors: detectors ?? CreateM6A1Detectors(),
                SnrDbValues: snrDbValues ?? [-9d, -3d, 0d],
                WindowLengths: windowLengths ?? [64, 128],
                TrialCountPerCondition: trialCountPerCondition),
            ManifestMetadata: new ManifestMetadataConfig("note", "m6a1", new Dictionary<string, string> { ["suite"] = "tests", ["milestone"] = "m6a1" }),
            ArtifactRetentionMode: artifactRetentionMode);
    }

    public static M6A2ComplementaryValueConfig CreateM6A2ComplementaryValueConfig(
        string experimentId,
        IReadOnlyList<int>? seedPanel = null,
        IReadOnlyList<BenchmarkTaskConfig>? tasks = null,
        IReadOnlyList<DetectorConfig>? detectors = null,
        IReadOnlyList<FeatureBundleConfig>? bundles = null,
        IReadOnlyList<double>? snrDbValues = null,
        IReadOnlyList<int>? windowLengths = null,
        int trialCountPerCondition = 12,
        string artifactRetentionMode = ArtifactRetentionModes.Milestone)
    {
        return new M6A2ComplementaryValueConfig(
            ExperimentId: experimentId,
            ExperimentName: "M6a2 Complementary Value Test",
            SeedPanel: seedPanel ?? [86420, 97531, 24680],
            OutputDirectory: "artifacts",
            Scenario: new ScenarioConfig(ExperimentConfigValidator.SyntheticBenchmarkScenarioName, 2, 128),
            TrialCount: 4,
            Detector: new DetectorConfig(DetectorCatalog.EnergyDetectorName, 1d, DetectorCatalog.EnergyDetectorMode),
            Signal: null,
            Benchmark: new SyntheticBenchmarkConfig(
                BaseStreamLength: 4096,
                Noise: new GaussianNoiseConfig(0d, 1d),
                Cases: Array.Empty<SyntheticCaseConfig>()),
            Evaluation: new EvaluationConfig(
                Tasks: tasks ?? [CreateEngineeredStructureVsNaturalCorrelationTask(), CreateEqualEnergyEngineeredStructureVsNaturalCorrelationTask()],
                Detectors: detectors ?? CreateM6A1Detectors(),
                SnrDbValues: snrDbValues ?? [-9d, -3d, 0d],
                WindowLengths: windowLengths ?? [64, 128],
                TrialCountPerCondition: trialCountPerCondition),
            Bundles: bundles ?? CreateM6A2Bundles(),
            ManifestMetadata: new ManifestMetadataConfig("note", "m6a2", new Dictionary<string, string> { ["suite"] = "tests", ["milestone"] = "m6a2" }),
            ArtifactRetentionMode: artifactRetentionMode);
    }

    public static BenchmarkTaskConfig CreateOfdmTask()
    {
        return new BenchmarkTaskConfig(
            Name: BenchmarkTaskCatalog.OfdmSignalPresentVsNoiseOnly,
            Description: "Positive class is an OFDM-like synthetic signal mixed into Gaussian background noise. Negative class is Gaussian noise only.",
            PositiveCase: CreateOfdmLikeCase(-3d),
            NegativeCase: CreateNoiseOnlyCase());
    }

    public static BenchmarkTaskConfig CreateGaussianEmitterTask()
    {
        return new BenchmarkTaskConfig(
            Name: BenchmarkTaskCatalog.GaussianEmitterVsNoiseOnly,
            Description: "Positive class is an independent Gaussian emitter mixed into Gaussian background noise. Negative class is Gaussian noise only.",
            PositiveCase: CreateGaussianEmitterCase(0d),
            NegativeCase: CreateNoiseOnlyCase());
    }

    public static BenchmarkTaskConfig CreateStructuredBurstTask()
    {
        return new BenchmarkTaskConfig(
            Name: BenchmarkTaskCatalog.StructuredBurstVsNoiseOnly,
            Description: "Positive class is a weak OFDM-like burst embedded in Gaussian noise at the configured SNR. Negative class is Gaussian white noise only.",
            PositiveCase: CreateBurstOfdmLikeCase(-3d),
            NegativeCase: CreateNoiseOnlyCase());
    }

    public static BenchmarkTaskConfig CreateColoredNuisanceTask()
    {
        return new BenchmarkTaskConfig(
            Name: BenchmarkTaskCatalog.ColoredNuisanceVsWhiteNoise,
            Description: "Positive class is a modest correlated Gaussian nuisance process mixed into Gaussian noise at the configured SNR. Negative class is Gaussian white noise only.",
            PositiveCase: CreateCorrelatedGaussianCase(-3d),
            NegativeCase: CreateNoiseOnlyCase());
    }

    public static BenchmarkTaskConfig CreateEqualEnergyTask()
    {
        return new BenchmarkTaskConfig(
            Name: BenchmarkTaskCatalog.EqualEnergyStructuredVsUnstructured,
            Description: "Positive class is an OFDM-like structured process mixed to the configured SNR. Negative class is a Gaussian emitter mixed to the same configured SNR so energy alone is intentionally weak.",
            PositiveCase: CreateOfdmLikeCase(-3d) with { Name = "equal-energy-structured" },
            NegativeCase: CreateGaussianEmitterCase(-3d) with { Name = "equal-energy-unstructured", TargetLabel = "less-structured" });
    }

    public static BenchmarkTaskConfig CreateEngineeredStructureVsNaturalCorrelationTask()
    {
        return new BenchmarkTaskConfig(
            Name: BenchmarkTaskCatalog.EngineeredStructureVsNaturalCorrelation,
            Description: "Positive class is a weak organized burst-ofdm-like process mixed into Gaussian noise at the configured SNR. Negative class is a non-iid correlated Gaussian nuisance process mixed to the same SNR, so both classes retain second-order structure.",
            PositiveCase: CreateBurstOfdmLikeCase(-3d) with { Name = "engineered-structured-burst", TargetLabel = "engineered-structure" },
            NegativeCase: CreateCorrelatedGaussianCase(-3d) with { Name = "natural-correlated-nuisance", TargetLabel = "natural-correlation" });
    }

    public static BenchmarkTaskConfig CreateEqualEnergyEngineeredStructureVsNaturalCorrelationTask()
    {
        return new BenchmarkTaskConfig(
            Name: BenchmarkTaskCatalog.EqualEnergyEngineeredStructureVsNaturalCorrelation,
            Description: "Positive class is an OFDM-like engineered structured process mixed to the configured SNR. Negative class is a correlated Gaussian nuisance process mixed to the same configured SNR so both classes are explicitly matched in nominal signal power and energy alone is intentionally weak.",
            PositiveCase: CreateOfdmLikeCase(-3d, symbolSeed: 177) with { Name = "equal-energy-engineered-structure", TargetLabel = "engineered-structure" },
            NegativeCase: CreateCorrelatedGaussianCase(-3d) with { Name = "equal-energy-natural-correlation", TargetLabel = "natural-correlation" });
    }

    public static SyntheticCaseConfig CreateNoiseOnlyCase()
    {
        return new SyntheticCaseConfig(
            Name: "noise-only-baseline",
            TargetLabel: "noise-only",
            SourceType: ExperimentConfigValidator.NoiseOnlySourceType,
            SnrDb: null,
            GaussianEmitter: null,
            OfdmLike: null);
    }

    public static SyntheticCaseConfig CreateGaussianEmitterCase(double snrDb = 0d)
    {
        return new SyntheticCaseConfig(
            Name: "gaussian-emitter-control",
            TargetLabel: "signal-present",
            SourceType: ExperimentConfigValidator.GaussianEmitterSourceType,
            SnrDb: snrDb,
            GaussianEmitter: new GaussianEmitterConfig(0d, 1d),
            OfdmLike: null);
    }

    public static SyntheticCaseConfig CreateBurstOfdmLikeCase(double snrDb = -3d)
    {
        return new SyntheticCaseConfig(
            Name: "structured-burst",
            TargetLabel: "structured-burst-present",
            SourceType: ExperimentConfigValidator.BurstOfdmLikeSourceType,
            SnrDb: snrDb,
            GaussianEmitter: null,
            OfdmLike: null,
            BurstOfdmLike: new BurstOfdmLikeConfig(
                new OfdmLikeSignalConfig(8, 32, 91, 0.75d, 1d),
                StartFraction: 0.35d,
                LengthFraction: 0.18d),
            CorrelatedGaussian: null);
    }

    public static SyntheticCaseConfig CreateCorrelatedGaussianCase(double snrDb = -3d)
    {
        return new SyntheticCaseConfig(
            Name: "colored-nuisance",
            TargetLabel: "correlated-nuisance",
            SourceType: ExperimentConfigValidator.CorrelatedGaussianSourceType,
            SnrDb: snrDb,
            GaussianEmitter: null,
            OfdmLike: null,
            BurstOfdmLike: null,
            CorrelatedGaussian: new CorrelatedGaussianProcessConfig(
                InnovationStandardDeviation: 1d,
                ArCoefficient: 0.92d));
    }

    public static SyntheticCaseConfig CreateOfdmLikeCase(double snrDb = -3d, int symbolSeed = 77)
    {
        return new SyntheticCaseConfig(
            Name: "ofdm-like-signal",
            TargetLabel: "signal-present",
            SourceType: ExperimentConfigValidator.OfdmLikeSourceType,
            SnrDb: snrDb,
            GaussianEmitter: null,
            OfdmLike: new OfdmLikeSignalConfig(8, 32, symbolSeed, 0.75d, 1d));
    }
}
