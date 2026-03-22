namespace RfCompressionReplay.Core.Config;

public sealed record ExperimentConfig(
    string ExperimentId,
    string ExperimentName,
    int Seed,
    string OutputDirectory,
    ScenarioConfig Scenario,
    int TrialCount,
    DetectorConfig Detector,
    SignalConfig? Signal,
    SyntheticBenchmarkConfig? Benchmark,
    EvaluationConfig? Evaluation,
    ManifestMetadataConfig ManifestMetadata);

public sealed record ScenarioConfig(
    string Name,
    int SampleWindowCount,
    int SamplesPerWindow);

public sealed record DetectorConfig(
    string Name,
    double Threshold,
    string Mode);

public sealed record SignalConfig(
    string Name,
    double BaseLevel,
    double NoiseScale);

public sealed record SyntheticBenchmarkConfig(
    int BaseStreamLength,
    GaussianNoiseConfig Noise,
    IReadOnlyList<SyntheticCaseConfig> Cases);

public sealed record GaussianNoiseConfig(
    double Mean,
    double StandardDeviation);

public sealed record SyntheticCaseConfig(
    string Name,
    string TargetLabel,
    string SourceType,
    double? SnrDb,
    GaussianEmitterConfig? GaussianEmitter,
    OfdmLikeSignalConfig? OfdmLike);

public sealed record GaussianEmitterConfig(
    double Mean,
    double StandardDeviation);

public sealed record OfdmLikeSignalConfig(
    int SubcarrierCount,
    int SamplesPerSymbol,
    int SymbolSeed,
    double CarrierSpacing,
    double Amplitude);

public sealed record EvaluationConfig(
    IReadOnlyList<BenchmarkTaskConfig> Tasks,
    IReadOnlyList<DetectorConfig> Detectors,
    IReadOnlyList<double> SnrDbValues,
    IReadOnlyList<int> WindowLengths,
    int TrialCountPerCondition);

public sealed record BenchmarkTaskConfig(
    string Name,
    string Description,
    SyntheticCaseConfig PositiveCase,
    SyntheticCaseConfig NegativeCase);

public sealed record ManifestMetadataConfig(
    string Notes,
    string VersionTag,
    IReadOnlyDictionary<string, string>? Tags)
{
    public static ManifestMetadataConfig Empty { get; } = new(string.Empty, string.Empty, new Dictionary<string, string>());
}
