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
    ManifestMetadataConfig ManifestMetadata,
    string ArtifactRetentionMode = ArtifactRetentionModes.Full,
    RepresentationConfig? Representation = null);

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
    OfdmLikeSignalConfig? OfdmLike,
    BurstOfdmLikeConfig? BurstOfdmLike = null,
    CorrelatedGaussianProcessConfig? CorrelatedGaussian = null);

public sealed record GaussianEmitterConfig(
    double Mean,
    double StandardDeviation);

public sealed record OfdmLikeSignalConfig(
    int SubcarrierCount,
    int SamplesPerSymbol,
    int SymbolSeed,
    double CarrierSpacing,
    double Amplitude);

public sealed record BurstOfdmLikeConfig(
    OfdmLikeSignalConfig Carrier,
    double StartFraction,
    double LengthFraction);

public sealed record CorrelatedGaussianProcessConfig(
    double InnovationStandardDeviation,
    double ArCoefficient);

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

public sealed record RepresentationConfig(
    double SampleScale = 1d,
    string NumericFormat = RepresentationFormats.Float64LittleEndian,
    string NormalizationMode = RepresentationNormalizations.None,
    double NormalizationTarget = 1d);

public static class RepresentationFormats
{
    public const string Float64LittleEndian = "float64-le";
    public const string Float32LittleEndian = "float32-le";

    public static IReadOnlyList<string> SupportedFormats { get; } =
    [
        Float64LittleEndian,
        Float32LittleEndian,
    ];

    public static bool IsSupported(string format)
    {
        return SupportedFormats.Contains(format, StringComparer.OrdinalIgnoreCase);
    }

    public static string SupportedFormatsDisplay => string.Join(", ", SupportedFormats);
}

public static class RepresentationNormalizations
{
    public const string None = "none";
    public const string Rms = "rms";

    public static IReadOnlyList<string> SupportedModes { get; } =
    [
        None,
        Rms,
    ];

    public static bool IsSupported(string mode)
    {
        return SupportedModes.Contains(mode, StringComparer.OrdinalIgnoreCase);
    }

    public static string SupportedModesDisplay => string.Join(", ", SupportedModes);
}
