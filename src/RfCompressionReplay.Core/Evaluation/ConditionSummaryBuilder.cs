using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class ConditionSummaryBuilder
{
    public static SummaryRecord Build(
        string taskName,
        string detectorName,
        string detectorMode,
        ScoreOrientation orientation,
        double? conditionSnrDb,
        int windowLength,
        IReadOnlyList<TrialRecord> trials,
        double auc)
    {
        var scores = trials.Select(trial => trial.Score).ToArray();
        var mean = scores.Average();
        var variance = scores.Length == 1
            ? 0d
            : scores.Select(score => Math.Pow(score - mean, 2d)).Average();

        return new SummaryRecord(
            TaskName: taskName,
            ScenarioName: null,
            TargetLabel: null,
            DetectorName: detectorName,
            DetectorMode: detectorMode,
            ScoreOrientation: orientation.ToString(),
            ConditionSnrDb: conditionSnrDb,
            SourceSnrDb: null,
            WindowLength: windowLength,
            Count: trials.Count,
            PositiveCount: trials.Count(trial => trial.IsPositiveClass == true),
            NegativeCount: trials.Count(trial => trial.IsPositiveClass == false),
            MinScore: DetectorMath.RoundScore(scores.Min()),
            MaxScore: DetectorMath.RoundScore(scores.Max()),
            MeanScore: DetectorMath.RoundScore(mean),
            StandardDeviation: DetectorMath.RoundScore(Math.Sqrt(variance)),
            AboveThresholdCount: trials.Count(trial => trial.IsAboveThreshold),
            Auc: auc);
    }
}
