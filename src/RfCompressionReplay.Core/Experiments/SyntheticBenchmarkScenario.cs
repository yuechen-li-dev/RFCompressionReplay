using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;
using RfCompressionReplay.Core.Randomness;
using RfCompressionReplay.Core.Signals.Synthetic;

namespace RfCompressionReplay.Core.Experiments;

public sealed class SyntheticBenchmarkScenario : IExperimentScenario
{
    private readonly SyntheticCaseStreamBuilder _streamBuilder;
    private readonly ConsecutiveWindowSampler _windowSampler;
    private readonly IDetector _detector;

    public SyntheticBenchmarkScenario(
        SyntheticCaseStreamBuilder streamBuilder,
        ConsecutiveWindowSampler windowSampler,
        IDetector detector)
    {
        _streamBuilder = streamBuilder;
        _windowSampler = windowSampler;
        _detector = detector;
    }

    public string Name => ExperimentConfigValidator.SyntheticBenchmarkScenarioName;

    public ExperimentResult Execute(ExperimentConfig config, ISeededRandom random)
    {
        if (config.Benchmark is null)
        {
            throw new InvalidOperationException("Synthetic benchmark execution requires Benchmark configuration.");
        }

        var trials = new List<TrialRecord>(config.TrialCount * config.Benchmark.Cases.Count);

        for (var caseIndex = 0; caseIndex < config.Benchmark.Cases.Count; caseIndex++)
        {
            var syntheticCase = config.Benchmark.Cases[caseIndex];
            var baseStream = _streamBuilder.BuildStream(config.Seed, caseIndex, config.Benchmark, syntheticCase);
            var startIndexRandom = new SeededRandom(SeedMath.Combine(config.Seed, 401, caseIndex));
            var startIndices = _windowSampler.CreateStartIndices(
                config.Benchmark.BaseStreamLength,
                config.TrialCount,
                config.Scenario.SampleWindowCount,
                config.Scenario.SamplesPerWindow,
                startIndexRandom);

            for (var trialIndex = 0; trialIndex < config.TrialCount; trialIndex++)
            {
                var windows = _windowSampler.ExtractWindows(
                    baseStream,
                    trialIndex,
                    startIndices[trialIndex],
                    config.Scenario.SampleWindowCount,
                    config.Scenario.SamplesPerWindow);

                var detectorResult = _detector.Evaluate(new DetectorInput(trialIndex, windows), config.Detector);
                var allSamples = windows.SelectMany(window => window.Samples).ToArray();
                var meanSample = allSamples.Length == 0 ? 0d : DetectorMath.RoundScore(allSamples.Average());
                var peakSample = allSamples.Length == 0 ? 0d : DetectorMath.RoundScore(allSamples.Max());

                trials.Add(new TrialRecord(
                    TrialIndex: trialIndex,
                    ScenarioName: syntheticCase.Name,
                    TargetLabel: syntheticCase.TargetLabel,
                    SourceType: syntheticCase.SourceType,
                    DetectorName: detectorResult.DetectorName,
                    DetectorMode: detectorResult.DetectorMode,
                    SnrDb: syntheticCase.SnrDb,
                    WindowLength: config.Scenario.SamplesPerWindow,
                    WindowCount: windows.Count,
                    SampleCount: allSamples.Length,
                    StartIndex: startIndices[trialIndex],
                    Score: detectorResult.Score,
                    IsAboveThreshold: detectorResult.IsAboveThreshold,
                    MeanSample: meanSample,
                    PeakSample: peakSample));
            }
        }

        var summaryGroups = trials
            .GroupBy(trial => new { trial.ScenarioName, trial.TargetLabel, trial.DetectorName, trial.DetectorMode })
            .Select(group =>
            {
                var scores = group.Select(trial => trial.Score).ToArray();
                var mean = scores.Average();
                var variance = scores.Length == 1
                    ? 0d
                    : scores.Select(score => Math.Pow(score - mean, 2d)).Average();

                return new SummaryRecord(
                    ScenarioName: group.Key.ScenarioName,
                    TargetLabel: group.Key.TargetLabel,
                    DetectorName: group.Key.DetectorName,
                    DetectorMode: group.Key.DetectorMode,
                    Count: group.Count(),
                    MinScore: DetectorMath.RoundScore(scores.Min()),
                    MaxScore: DetectorMath.RoundScore(scores.Max()),
                    MeanScore: DetectorMath.RoundScore(mean),
                    StandardDeviation: DetectorMath.RoundScore(Math.Sqrt(variance)),
                    AboveThresholdCount: group.Count(trial => trial.IsAboveThreshold));
            })
            .OrderBy(summary => summary.ScenarioName, StringComparer.Ordinal)
            .ToArray();

        return new ExperimentResult(trials, new ExperimentSummary(summaryGroups), null);
    }
}
