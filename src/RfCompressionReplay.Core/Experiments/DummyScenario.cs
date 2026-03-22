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

    public string Name => "dummy";

    public ExperimentResult Execute(ExperimentConfig config, ISeededRandom random)
    {
        var trials = new List<TrialRecord>(config.TrialCount);

        for (var trialIndex = 0; trialIndex < config.TrialCount; trialIndex++)
        {
            var windows = _signalProvider.CreateWindows(trialIndex, config.Scenario, config.Signal, random);
            var detectorResult = _detector.Evaluate(new DetectorInput(trialIndex, windows), config.Detector);
            var allSamples = windows.SelectMany(window => window.Samples).ToArray();
            trials.Add(new TrialRecord(
                TrialIndex: trialIndex,
                WindowCount: windows.Count,
                SampleCount: allSamples.Length,
                Score: detectorResult.Score,
                IsAboveThreshold: detectorResult.IsAboveThreshold,
                MeanSample: detectorResult.Metrics["mean"],
                PeakSample: detectorResult.Metrics["peak"]));
        }

        var scores = trials.Select(trial => trial.Score).ToArray();
        var summary = new SummaryRecord(
            TrialCount: trials.Count,
            MinScore: Math.Round(scores.Min(), 6, MidpointRounding.AwayFromZero),
            MaxScore: Math.Round(scores.Max(), 6, MidpointRounding.AwayFromZero),
            MeanScore: Math.Round(scores.Average(), 6, MidpointRounding.AwayFromZero),
            AboveThresholdCount: trials.Count(trial => trial.IsAboveThreshold));

        return new ExperimentResult(trials, summary, null);
    }
}
