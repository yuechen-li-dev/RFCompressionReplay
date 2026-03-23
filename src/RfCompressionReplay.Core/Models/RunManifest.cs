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
    ManifestMetadata Metadata,
    EvaluationManifest? Evaluation,
    ArtifactRetentionManifest Retention);

public sealed record ManifestMetadata(
    string Notes,
    string VersionTag,
    IReadOnlyDictionary<string, string>? Tags);

public sealed record EvaluationManifest(
    IReadOnlyList<string> TaskNames,
    IReadOnlyList<string> DetectorNames,
    IReadOnlyList<double> SnrDbValues,
    IReadOnlyList<int> WindowLengths,
    int TrialCountPerCondition);

public sealed record ArtifactRetentionManifest(
    string Mode,
    IReadOnlyList<string> OmittedArtifactKinds,
    string RegenerationNote);

public sealed record EnvironmentSummary(
    string MachineName,
    string UserName,
    string OperatingSystem,
    string FrameworkDescription,
    string ProcessArchitecture,
    string CurrentDirectory);
