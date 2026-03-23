using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public sealed class TinyLogisticRegressionBundleEvaluator
{
    private readonly RocAucCalculator _rocAucCalculator;

    public TinyLogisticRegressionBundleEvaluator(RocAucCalculator rocAucCalculator)
    {
        _rocAucCalculator = rocAucCalculator;
    }

    public IReadOnlyList<M6A2BundleConditionRow> Evaluate(
        IReadOnlyList<(int Seed, ExperimentResult Result)> seedResults,
        IReadOnlyList<FeatureBundleConfig> bundles)
    {
        var examples = seedResults
            .SelectMany(result => BuildExamples(result.Seed, result.Result.Trials))
            .ToArray();

        var rows = new List<M6A2BundleConditionRow>();
        var conditions = examples
            .GroupBy(example => (example.TaskFamilyId, example.Seed, example.SnrDb, example.WindowLength))
            .OrderBy(group => group.Key.TaskFamilyId, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Seed)
            .ThenBy(group => group.Key.SnrDb)
            .ThenBy(group => group.Key.WindowLength);

        foreach (var condition in conditions)
        {
            var training = examples
                .Where(example => string.Equals(example.TaskFamilyId, condition.Key.TaskFamilyId, StringComparison.Ordinal)
                    && example.SnrDb.Equals(condition.Key.SnrDb)
                    && example.WindowLength == condition.Key.WindowLength
                    && example.Seed != condition.Key.Seed)
                .ToArray();
            var test = condition.ToArray();

            foreach (var bundle in bundles)
            {
                var model = Train(training, bundle.FeatureDetectors);
                var scoredTest = test
                    .Select(example => new BinaryScoreRecord(
                        example.IsPositiveClass,
                        Score(model, bundle.FeatureDetectors, example.FeatureScores)))
                    .ToArray();
                var auc = _rocAucCalculator.Calculate(scoredTest, ScoreOrientation.HigherScoreMorePositive).Auc;

                rows.Add(new M6A2BundleConditionRow(
                    condition.Key.TaskFamilyId,
                    condition.Key.Seed,
                    condition.Key.SnrDb,
                    condition.Key.WindowLength,
                    bundle.Id,
                    Round(auc)));
            }
        }

        return rows;
    }

    private static FeatureExample[] BuildExamples(int seed, IReadOnlyList<TrialRecord> trials)
    {
        return trials
            .Where(trial => trial.TaskName is not null && trial.ConditionSnrDb.HasValue && trial.IsPositiveClass.HasValue)
            .GroupBy(trial => new FeatureExampleKey(
                trial.TaskName!,
                seed,
                trial.ConditionSnrDb!.Value,
                trial.WindowLength,
                trial.IsPositiveClass!.Value,
                trial.TrialIndex))
            .Select(group => new FeatureExample(
                group.Key.TaskFamilyId,
                group.Key.Seed,
                group.Key.SnrDb,
                group.Key.WindowLength,
                group.Key.IsPositiveClass,
                group.ToDictionary(trial => trial.DetectorName, trial => trial.Score, StringComparer.OrdinalIgnoreCase)))
            .OrderBy(example => example.TaskFamilyId, StringComparer.Ordinal)
            .ThenBy(example => example.Seed)
            .ThenBy(example => example.SnrDb)
            .ThenBy(example => example.WindowLength)
            .ThenByDescending(example => example.IsPositiveClass)
            .ToArray();
    }

    private static LogisticModel Train(IReadOnlyList<FeatureExample> training, IReadOnlyList<string> featureDetectors)
    {
        if (training.Count == 0)
        {
            throw new InvalidOperationException("Tiny logistic bundle evaluation requires at least one training example.");
        }

        var featureCount = featureDetectors.Count;
        var means = new double[featureCount];
        var standardDeviations = new double[featureCount];

        for (var featureIndex = 0; featureIndex < featureCount; featureIndex++)
        {
            var detectorId = featureDetectors[featureIndex];
            var values = training.Select(example => GetFeatureValue(example.FeatureScores, detectorId)).ToArray();
            means[featureIndex] = values.Average();
            var variance = values.Select(value => (value - means[featureIndex]) * (value - means[featureIndex])).Average();
            standardDeviations[featureIndex] = variance <= 1e-12d ? 1d : Math.Sqrt(variance);
        }

        var weights = new double[featureCount];
        var bias = 0d;
        const double learningRate = 0.1d;
        const double l2Penalty = 0.001d;
        const int iterations = 400;

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var gradient = new double[featureCount];
            var biasGradient = 0d;

            foreach (var example in training)
            {
                var standardized = Standardize(featureDetectors, example.FeatureScores, means, standardDeviations);
                var score = Dot(weights, standardized) + bias;
                var prediction = Sigmoid(score);
                var target = example.IsPositiveClass ? 1d : 0d;
                var error = prediction - target;

                for (var featureIndex = 0; featureIndex < featureCount; featureIndex++)
                {
                    gradient[featureIndex] += error * standardized[featureIndex];
                }

                biasGradient += error;
            }

            for (var featureIndex = 0; featureIndex < featureCount; featureIndex++)
            {
                gradient[featureIndex] = (gradient[featureIndex] / training.Count) + (l2Penalty * weights[featureIndex]);
                weights[featureIndex] -= learningRate * gradient[featureIndex];
            }

            bias -= learningRate * (biasGradient / training.Count);
        }

        return new LogisticModel(weights, bias, means, standardDeviations);
    }

    private static double Score(LogisticModel model, IReadOnlyList<string> featureDetectors, IReadOnlyDictionary<string, double> featureScores)
    {
        var standardized = Standardize(featureDetectors, featureScores, model.Means, model.StandardDeviations);
        return Round(Dot(model.Weights, standardized) + model.Bias);
    }

    private static double[] Standardize(
        IReadOnlyList<string> featureDetectors,
        IReadOnlyDictionary<string, double> featureScores,
        IReadOnlyList<double> means,
        IReadOnlyList<double> standardDeviations)
    {
        var standardized = new double[featureDetectors.Count];
        for (var featureIndex = 0; featureIndex < featureDetectors.Count; featureIndex++)
        {
            var value = GetFeatureValue(featureScores, featureDetectors[featureIndex]);
            standardized[featureIndex] = (value - means[featureIndex]) / standardDeviations[featureIndex];
        }

        return standardized;
    }

    private static double GetFeatureValue(IReadOnlyDictionary<string, double> featureScores, string detectorId)
    {
        if (!featureScores.TryGetValue(detectorId, out var value))
        {
            throw new InvalidOperationException($"Missing detector feature '{detectorId}' while building an M6a2 bundle example.");
        }

        return value;
    }

    private static double Dot(IReadOnlyList<double> weights, IReadOnlyList<double> values)
    {
        var sum = 0d;
        for (var index = 0; index < weights.Count; index++)
        {
            sum += weights[index] * values[index];
        }

        return sum;
    }

    private static double Sigmoid(double value)
    {
        if (value >= 0d)
        {
            var exp = Math.Exp(-value);
            return 1d / (1d + exp);
        }

        var positiveExp = Math.Exp(value);
        return positiveExp / (1d + positiveExp);
    }

    private static double Round(double value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }

    private sealed record FeatureExample(
        string TaskFamilyId,
        int Seed,
        double SnrDb,
        int WindowLength,
        bool IsPositiveClass,
        IReadOnlyDictionary<string, double> FeatureScores);

    private readonly record struct FeatureExampleKey(
        string TaskFamilyId,
        int Seed,
        double SnrDb,
        int WindowLength,
        bool IsPositiveClass,
        int TrialIndex);

    private sealed record LogisticModel(
        IReadOnlyList<double> Weights,
        double Bias,
        IReadOnlyList<double> Means,
        IReadOnlyList<double> StandardDeviations);
}
