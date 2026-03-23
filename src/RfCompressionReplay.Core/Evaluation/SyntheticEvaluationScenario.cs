using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Experiments;
using RfCompressionReplay.Core.Models;
using RfCompressionReplay.Core.Randomness;
using RfCompressionReplay.Core.Signals.Synthetic;

namespace RfCompressionReplay.Core.Evaluation;

public sealed class SyntheticEvaluationScenario : IExperimentScenario
{
    private readonly SyntheticCaseStreamBuilder _streamBuilder;
    private readonly ConsecutiveWindowSampler _windowSampler;
    private readonly RocAucCalculator _rocAucCalculator;

    public SyntheticEvaluationScenario(
        SyntheticCaseStreamBuilder streamBuilder,
        ConsecutiveWindowSampler windowSampler,
        RocAucCalculator rocAucCalculator)
    {
        _streamBuilder = streamBuilder;
        _windowSampler = windowSampler;
        _rocAucCalculator = rocAucCalculator;
    }

    public string Name => ExperimentConfigValidator.SyntheticBenchmarkScenarioName;

    public ExperimentResult Execute(ExperimentConfig config, ISeededRandom random)
    {
        if (config.Benchmark is null || config.Evaluation is null)
        {
            throw new InvalidOperationException("Synthetic evaluation execution requires Benchmark and Evaluation configuration.");
        }

        var trials = new List<TrialRecord>();
        var summaries = new List<SummaryRecord>();
        var rocPoints = new List<RocPointRecord>();

        for (var taskIndex = 0; taskIndex < config.Evaluation.Tasks.Count; taskIndex++)
        {
            var task = config.Evaluation.Tasks[taskIndex];

            for (var detectorIndex = 0; detectorIndex < config.Evaluation.Detectors.Count; detectorIndex++)
            {
                var detectorConfig = config.Evaluation.Detectors[detectorIndex];
                var detector = DetectorFactory.Create(detectorConfig, config.Representation);
                var orientation = DetectorCatalog.GetScoreOrientation(detectorConfig.Name);

                for (var snrIndex = 0; snrIndex < config.Evaluation.SnrDbValues.Count; snrIndex++)
                {
                    var conditionSnrDb = config.Evaluation.SnrDbValues[snrIndex];

                    for (var windowIndex = 0; windowIndex < config.Evaluation.WindowLengths.Count; windowIndex++)
                    {
                        var windowLength = config.Evaluation.WindowLengths[windowIndex];
                        var conditionTrials = new List<TrialRecord>(config.Evaluation.TrialCountPerCondition * 2);

                        EvaluateCase(
                            config,
                            taskIndex,
                            detector,
                            detectorConfig,
                            task.Name,
                            conditionSnrDb,
                            snrIndex,
                            windowLength,
                            windowIndex,
                            task.PositiveCase,
                            isPositiveClass: true,
                            conditionTrials);

                        EvaluateCase(
                            config,
                            taskIndex,
                            detector,
                            detectorConfig,
                            task.Name,
                            conditionSnrDb,
                            snrIndex,
                            windowLength,
                            windowIndex,
                            task.NegativeCase,
                            isPositiveClass: false,
                            conditionTrials);

                        var roc = _rocAucCalculator.Calculate(
                            conditionTrials.Select(trial => new BinaryScoreRecord(trial.IsPositiveClass ?? false, trial.Score)).ToArray(),
                            orientation);

                        summaries.Add(ConditionSummaryBuilder.Build(
                            task.Name,
                            detectorConfig.Name,
                            detectorConfig.Mode,
                            orientation,
                            conditionSnrDb,
                            windowLength,
                            conditionTrials,
                            roc.Auc));

                        foreach (var point in roc.Points)
                        {
                            rocPoints.Add(new RocPointRecord(
                                TaskName: task.Name,
                                DetectorName: detectorConfig.Name,
                                DetectorMode: detectorConfig.Mode,
                                ScoreOrientation: orientation.ToString(),
                                ConditionSnrDb: conditionSnrDb,
                                WindowLength: windowLength,
                                Threshold: point.Threshold,
                                TruePositiveRate: point.TruePositiveRate,
                                FalsePositiveRate: point.FalsePositiveRate,
                                TruePositives: point.TruePositives,
                                FalsePositives: point.FalsePositives,
                                PositiveCount: point.PositiveCount,
                                NegativeCount: point.NegativeCount,
                                Auc: roc.Auc));
                        }

                        trials.AddRange(conditionTrials);
                    }
                }
            }
        }

        return new ExperimentResult(
            trials.OrderBy(trial => trial.TaskName, StringComparer.Ordinal)
                .ThenBy(trial => trial.DetectorName, StringComparer.Ordinal)
                .ThenBy(trial => trial.ConditionSnrDb)
                .ThenBy(trial => trial.WindowLength)
                .ThenByDescending(trial => trial.IsPositiveClass)
                .ThenBy(trial => trial.TrialIndex)
                .ToArray(),
            new ExperimentSummary(summaries
                .OrderBy(summary => summary.TaskName, StringComparer.Ordinal)
                .ThenBy(summary => summary.DetectorName, StringComparer.Ordinal)
                .ThenBy(summary => summary.ConditionSnrDb)
                .ThenBy(summary => summary.WindowLength)
                .ToArray()),
            new EvaluationArtifacts(rocPoints
                .OrderBy(point => point.TaskName, StringComparer.Ordinal)
                .ThenBy(point => point.DetectorName, StringComparer.Ordinal)
                .ThenBy(point => point.ConditionSnrDb)
                .ThenBy(point => point.WindowLength)
                .ThenBy(point => point.FalsePositiveRate)
                .ThenBy(point => point.TruePositiveRate)
                .ThenBy(point => point.Threshold)
                .ToArray()),
            null);
    }

    private void EvaluateCase(
        ExperimentConfig config,
        int taskIndex,
        IDetector detector,
        DetectorConfig detectorConfig,
        string taskName,
        double conditionSnrDb,
        int snrIndex,
        int windowLength,
        int windowIndex,
        SyntheticCaseConfig template,
        bool isPositiveClass,
        List<TrialRecord> destination)
    {
        var materializedCase = ApplyCondition(template, conditionSnrDb);
        var classIndex = isPositiveClass ? 1 : 0;
        var streamSeed = SeedMath.Combine(config.Seed, 701, taskIndex, snrIndex, windowIndex, classIndex);
        var startSeed = SeedMath.Combine(config.Seed, 907, taskIndex, snrIndex, windowIndex, classIndex);
        var benchmarkConfig = config.Benchmark! with { Cases = [materializedCase] };
        var baseStream = _streamBuilder.BuildStream(streamSeed, classIndex, benchmarkConfig, materializedCase);
        var startIndices = _windowSampler.CreateStartIndices(
            config.Benchmark!.BaseStreamLength,
            config.Evaluation!.TrialCountPerCondition,
            config.Scenario.SampleWindowCount,
            windowLength,
            new SeededRandom(startSeed));

        for (var trialIndex = 0; trialIndex < config.Evaluation.TrialCountPerCondition; trialIndex++)
        {
            var windows = _windowSampler.ExtractWindows(
                baseStream,
                trialIndex,
                startIndices[trialIndex],
                config.Scenario.SampleWindowCount,
                windowLength);

            var detectorResult = detector.Evaluate(new DetectorInput(trialIndex, windows), detectorConfig);
            var allSamples = windows.SelectMany(window => window.Samples).ToArray();
            var meanSample = allSamples.Length == 0 ? 0d : DetectorMath.RoundScore(allSamples.Average());
            var peakSample = allSamples.Length == 0 ? 0d : DetectorMath.RoundScore(allSamples.Max());

            destination.Add(new TrialRecord(
                TrialIndex: trialIndex,
                TaskName: taskName,
                ScenarioName: materializedCase.Name,
                TargetLabel: materializedCase.TargetLabel,
                ClassLabel: materializedCase.TargetLabel,
                IsPositiveClass: isPositiveClass,
                SourceType: materializedCase.SourceType,
                DetectorName: detectorResult.DetectorName,
                DetectorMode: detectorResult.DetectorMode,
                ScoreOrientation: DetectorCatalog.GetScoreOrientation(detectorResult.DetectorName).ToString(),
                ConditionSnrDb: conditionSnrDb,
                SourceSnrDb: materializedCase.SnrDb,
                WindowLength: windowLength,
                WindowCount: windows.Count,
                SampleCount: allSamples.Length,
                StartIndex: startIndices[trialIndex],
                Score: detectorResult.Score,
                IsAboveThreshold: detectorResult.IsAboveThreshold,
                MeanSample: meanSample,
                PeakSample: peakSample));
        }
    }

    private static SyntheticCaseConfig ApplyCondition(SyntheticCaseConfig template, double conditionSnrDb)
    {
        if (string.Equals(template.SourceType, ExperimentConfigValidator.NoiseOnlySourceType, StringComparison.OrdinalIgnoreCase))
        {
            return template with { SnrDb = null };
        }

        return template with { SnrDb = conditionSnrDb };
    }
}
