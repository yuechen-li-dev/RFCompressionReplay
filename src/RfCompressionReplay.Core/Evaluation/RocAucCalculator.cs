using RfCompressionReplay.Core.Detectors;

namespace RfCompressionReplay.Core.Evaluation;

public sealed class RocAucCalculator
{
    public RocCurveResult Calculate(IReadOnlyList<BinaryScoreRecord> scores, ScoreOrientation orientation)
    {
        if (scores.Count == 0)
        {
            throw new InvalidOperationException("ROC/AUC requires at least one scored example.");
        }

        var positiveCount = scores.Count(score => score.IsPositiveClass);
        var negativeCount = scores.Count - positiveCount;
        if (positiveCount == 0 || negativeCount == 0)
        {
            throw new InvalidOperationException("ROC/AUC requires at least one positive score and one negative score.");
        }

        var ordered = orientation == ScoreOrientation.HigherScoreMorePositive
            ? scores.OrderByDescending(score => score.Score).ToArray()
            : scores.OrderBy(score => score.Score).ToArray();

        var points = new List<RocPoint>
        {
            new(
                Threshold: null,
                TruePositiveRate: 0d,
                FalsePositiveRate: 0d,
                TruePositives: 0,
                FalsePositives: 0,
                PositiveCount: positiveCount,
                NegativeCount: negativeCount),
        };

        var truePositives = 0;
        var falsePositives = 0;
        var index = 0;
        while (index < ordered.Length)
        {
            var threshold = ordered[index].Score;
            while (index < ordered.Length && ordered[index].Score.Equals(threshold))
            {
                if (ordered[index].IsPositiveClass)
                {
                    truePositives++;
                }
                else
                {
                    falsePositives++;
                }

                index++;
            }

            points.Add(new RocPoint(
                Threshold: threshold,
                TruePositiveRate: (double)truePositives / positiveCount,
                FalsePositiveRate: (double)falsePositives / negativeCount,
                TruePositives: truePositives,
                FalsePositives: falsePositives,
                PositiveCount: positiveCount,
                NegativeCount: negativeCount));
        }

        var auc = 0d;
        for (var pointIndex = 1; pointIndex < points.Count; pointIndex++)
        {
            var previous = points[pointIndex - 1];
            var current = points[pointIndex];
            var deltaX = current.FalsePositiveRate - previous.FalsePositiveRate;
            auc += deltaX * (current.TruePositiveRate + previous.TruePositiveRate) / 2d;
        }

        return new RocCurveResult(orientation, DetectorMath.RoundScore(auc), points);
    }
}
