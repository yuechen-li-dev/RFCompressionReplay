namespace RfCompressionReplay.Core.Models;

public sealed record ExperimentResult(
    IReadOnlyList<TrialRecord> Trials,
    SummaryRecord Summary,
    ArtifactPaths? Artifacts);
