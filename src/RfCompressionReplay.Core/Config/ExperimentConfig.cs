using System.Text.Json.Serialization;

namespace RfCompressionReplay.Core.Config;

public sealed record ExperimentConfig(
    string ExperimentId,
    string ExperimentName,
    int Seed,
    string OutputDirectory,
    ScenarioConfig Scenario,
    int TrialCount,
    DetectorConfig Detector,
    SignalConfig Signal,
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

public sealed record ManifestMetadataConfig(
    string Notes,
    string VersionTag,
    IReadOnlyDictionary<string, string>? Tags)
{
    public static ManifestMetadataConfig Empty { get; } = new(string.Empty, string.Empty, new Dictionary<string, string>());
}
