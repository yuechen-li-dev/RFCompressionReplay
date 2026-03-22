namespace RfCompressionReplay.Core.Evaluation;

public sealed record RocPoint(
    double? Threshold,
    double TruePositiveRate,
    double FalsePositiveRate,
    int TruePositives,
    int FalsePositives,
    int PositiveCount,
    int NegativeCount);

public sealed record RocCurveResult(
    ScoreOrientation Orientation,
    double Auc,
    IReadOnlyList<RocPoint> Points);
