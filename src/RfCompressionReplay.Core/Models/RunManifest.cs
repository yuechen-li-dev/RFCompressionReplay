namespace RfCompressionReplay.Core.Models;

public sealed record RunManifest(
    string ExperimentId,
    string ExperimentName,
    DateTimeOffset UtcTimestamp,
    int Seed,
    string GitCommit,
    EnvironmentSummary Environment,
    string ConfigFilePath,
    string ScenarioName,
    int TrialCount,
    IReadOnlyList<string> ArtifactPaths,
    IReadOnlyList<string> Warnings,
    ManifestMetadata Metadata);

public sealed record ManifestMetadata(
    string Notes,
    string VersionTag,
    IReadOnlyDictionary<string, string>? Tags);

public sealed record EnvironmentSummary(
    string MachineName,
    string UserName,
    string OperatingSystem,
    string FrameworkDescription,
    string ProcessArchitecture,
    string CurrentDirectory);
