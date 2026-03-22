using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;
using RfCompressionReplay.Core.Randomness;
using RfCompressionReplay.Core.Signals;

namespace RfCompressionReplay.Core.Experiments;

public sealed class DummyScenario : IExperimentScenario
{
    private readonly ISignalProvider _signalProvider;
    private readonly IDetector _detector;

    public DummyScenario(ISignalProvider signalProvider, IDetector detector)
    {
        _signalProvider = signalProvider;
        _detector = detector;
    }

    public string Name => ExperimentConfigValidator.DummyScenarioName;

    public ExperimentResult Execute(ExperimentConfig config, ISeededRandom random)
    {
        var trials = new List<TrialRecord>(config.TrialCount);

        for (var trialIndex = 0; trialIndex < config.TrialCount; trialIndex++)
        {
            var windows = _signalProvider.CreateWindows(trialIndex, config.Scenario, config.Signal!, random);
            var detectorResult = _detector.Evaluate(new DetectorInput(trialIndex, windows), config.Detector);
            var allSamples = windows.SelectMany(window => window.Samples).ToArray();
            var meanSample = allSamples.Length == 0 ? 0d : DetectorMath.RoundScore(allSamples.Average());
            var peakSample = allSamples.Length == 0 ? 0d : DetectorMath.RoundScore(allSamples.Max());

            trials.Add(new TrialRecord(
                TrialIndex: trialIndex,
                TaskName: null,
                ScenarioName: Name,
                TargetLabel: "dummy",
                ClassLabel: "dummy",
                IsPositiveClass: null,
                SourceType: config.Signal!.Name,
                DetectorName: detectorResult.DetectorName,
                DetectorMode: detectorResult.DetectorMode,
                ScoreOrientation: DetectorCatalog.GetScoreOrientation(detectorResult.DetectorName).ToString(),
                ConditionSnrDb: null,
                SourceSnrDb: null,
                WindowLength: config.Scenario.SamplesPerWindow,
                WindowCount: windows.Count,
                SampleCount: allSamples.Length,
                StartIndex: 0,
                Score: detectorResult.Score,
                IsAboveThreshold: detectorResult.IsAboveThreshold,
                MeanSample: meanSample,
                PeakSample: peakSample));
        }

        var scores = trials.Select(trial => trial.Score).ToArray();
        var mean = scores.Average();
        var variance = scores.Length == 1 ? 0d : scores.Select(score => Math.Pow(score - mean, 2d)).Average();
        var summary = new SummaryRecord(
            TaskName: null,
            ScenarioName: Name,
            TargetLabel: "dummy",
            DetectorName: config.Detector.Name,
            DetectorMode: config.Detector.Mode,
            ScoreOrientation: DetectorCatalog.GetScoreOrientation(config.Detector.Name).ToString(),
            ConditionSnrDb: null,
            SourceSnrDb: null,
            WindowLength: config.Scenario.SamplesPerWindow,
            Count: trials.Count,
            PositiveCount: null,
            NegativeCount: null,
            MinScore: DetectorMath.RoundScore(scores.Min()),
            MaxScore: DetectorMath.RoundScore(scores.Max()),
            MeanScore: DetectorMath.RoundScore(mean),
            StandardDeviation: DetectorMath.RoundScore(Math.Sqrt(variance)),
            AboveThresholdCount: trials.Count(trial => trial.IsAboveThreshold),
            Auc: null);

        return new ExperimentResult(trials, new ExperimentSummary(new[] { summary }), null, null);
    }
}
