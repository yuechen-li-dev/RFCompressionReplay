namespace RfCompressionReplay.Core.Models;

public sealed record ExperimentResult(
    IReadOnlyList<TrialRecord> Trials,
    ExperimentSummary Summary,
    ArtifactPaths? Artifacts);
