namespace RfCompressionReplay.Core.Models;

public sealed record EvaluationArtifacts(
    IReadOnlyList<RocPointRecord> RocPoints);

public sealed record RocPointRecord(
    string TaskName,
    string DetectorName,
    string DetectorMode,
    string ScoreOrientation,
    double? ConditionSnrDb,
    int WindowLength,
    double? Threshold,
    double TruePositiveRate,
    double FalsePositiveRate,
    int TruePositives,
    int FalsePositives,
    int PositiveCount,
    int NegativeCount,
    double Auc);

public sealed record ExperimentResult(
    IReadOnlyList<TrialRecord> Trials,
    ExperimentSummary Summary,
    EvaluationArtifacts? Evaluation,
    ArtifactPaths? Artifacts);
