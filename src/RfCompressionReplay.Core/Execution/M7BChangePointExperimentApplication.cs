using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Models;
using RfCompressionReplay.Core.Randomness;
using RfCompressionReplay.Core.Signals.Synthetic;

namespace RfCompressionReplay.Core.Execution;

public sealed class M7BChangePointExperimentApplication
{
    private readonly IRunClock _clock;
    private readonly EnvironmentSummaryProvider _environmentSummaryProvider;
    private readonly GitCommitResolver _gitCommitResolver;
    private readonly SyntheticCaseStreamBuilder _streamBuilder;

    public M7BChangePointExperimentApplication(
        IRunClock clock,
        EnvironmentSummaryProvider environmentSummaryProvider,
        GitCommitResolver gitCommitResolver)
        : this(
            clock,
            environmentSummaryProvider,
            gitCommitResolver,
            new SyntheticCaseStreamBuilder(
                new GaussianNoiseGenerator(),
                new GaussianEmitterGenerator(),
                new OfdmLikeSignalGenerator(),
                new BurstOfdmLikeGenerator(new OfdmLikeSignalGenerator()),
                new CorrelatedGaussianProcessGenerator(),
                new SnrMixer()))
    {
    }

    public M7BChangePointExperimentApplication(
        IRunClock clock,
        EnvironmentSummaryProvider environmentSummaryProvider,
        GitCommitResolver gitCommitResolver,
        SyntheticCaseStreamBuilder streamBuilder)
    {
        _clock = clock;
        _environmentSummaryProvider = environmentSummaryProvider;
        _gitCommitResolver = gitCommitResolver;
        _streamBuilder = streamBuilder;
    }

    public string Run(M7BChangePointConfig config, string configPath, string repositoryRoot)
    {
        var validationErrors = M7BChangePointConfigValidator.Validate(config);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException("Configuration validation failed: " + string.Join(" ", validationErrors));
        }

        var clockTime = _clock.UtcNow;
        var fullConfigPath = Path.GetFullPath(configPath);
        var configDirectory = Path.GetDirectoryName(fullConfigPath) ?? Directory.GetCurrentDirectory();
        var outputRoot = Path.GetFullPath(config.OutputDirectory, configDirectory);
        var runDirectory = CreateRunDirectory(outputRoot, config.ExperimentId, clockTime);
        Directory.CreateDirectory(runDirectory);

        var warnings = new List<string>();
        var gitCommit = _gitCommitResolver.Resolve(repositoryRoot);
        if (gitCommit == "unknown")
        {
            warnings.Add("Git commit could not be resolved.");
        }

        var perConditionRows = RunEvaluation(config);
        var artifacts = M7BChangePointReportBuilder.Build(config, perConditionRows);

        var comparisonPath = Path.Combine(runDirectory, "m7b_boundary_comparison.csv");
        var taskSummaryPath = Path.Combine(runDirectory, "m7b_task_summary.csv");
        var findingsPath = Path.Combine(runDirectory, "m7b_findings.md");
        var manifestPath = Path.Combine(runDirectory, "manifest.json");

        M7BChangePointReportBuilder.WriteComparisonCsv(comparisonPath, artifacts.ComparisonRows);
        M7BChangePointReportBuilder.WriteTaskSummaryCsv(taskSummaryPath, artifacts.TaskSummaryRows);
        File.WriteAllText(findingsPath, artifacts.FindingsMarkdown);

        var manifest = new M7BChangePointManifest(
            config.ExperimentId,
            config.ExperimentName,
            clockTime,
            config.SeedPanel.ToArray(),
            gitCommit,
            _environmentSummaryProvider.Create(),
            Path.GetRelativePath(runDirectory, fullConfigPath),
            config.Scenario.Name,
            config.Evaluation.StreamCountPerCondition,
            [
                "manifest.json",
                "m7b_boundary_comparison.csv",
                "m7b_task_summary.csv",
                "m7b_findings.md"
            ],
            warnings,
            new ManifestMetadata(
                config.ManifestMetadata.Notes,
                config.ManifestMetadata.VersionTag,
                config.ManifestMetadata.Tags),
            new EvaluationManifest(
                config.Benchmark.Tasks.Select(task => task.Name).ToArray(),
                config.Evaluation.Detectors.Select(detector => detector.Name).ToArray(),
                config.Evaluation.SnrDbValues.ToArray(),
                config.Evaluation.WindowLengths.ToArray(),
                config.Evaluation.StreamCountPerCondition),
            new ArtifactRetentionManifest(
                config.ArtifactRetentionMode,
                ["summary.json", "summary.csv", "trials.csv", "roc_points.csv", "roc_points_compact.csv"],
                "M7b retains only the manifest, compact boundary comparison CSV, compact task summary CSV, and findings markdown by default. Re-run the same config locally with temporary instrumentation if you need raw trace dumps for audit work."));

        ExperimentConfigJson.Save(manifestPath, manifest);
        return runDirectory;
    }

    private IReadOnlyList<(string TaskName, int Seed, double SnrDb, int WindowLength, string DetectorName, IReadOnlyList<M7BStreamBoundaryMetrics> Metrics)> RunEvaluation(M7BChangePointConfig config)
    {
        var rows = new List<(string TaskName, int Seed, double SnrDb, int WindowLength, string DetectorName, IReadOnlyList<M7BStreamBoundaryMetrics> Metrics)>();

        for (var taskIndex = 0; taskIndex < config.Benchmark.Tasks.Count; taskIndex++)
        {
            var task = config.Benchmark.Tasks[taskIndex];

            for (var seedIndex = 0; seedIndex < config.SeedPanel.Count; seedIndex++)
            {
                var seed = config.SeedPanel[seedIndex];

                for (var snrIndex = 0; snrIndex < config.Evaluation.SnrDbValues.Count; snrIndex++)
                {
                    var snrDb = config.Evaluation.SnrDbValues[snrIndex];

                    for (var windowIndex = 0; windowIndex < config.Evaluation.WindowLengths.Count; windowIndex++)
                    {
                        var windowLength = config.Evaluation.WindowLengths[windowIndex];
                        var stride = Math.Max(1, (int)Math.Round(windowLength * config.Evaluation.WindowStrideFraction));
                        var tolerance = Math.Max(1, (int)Math.Round(windowLength * config.Evaluation.BoundaryToleranceWindowMultiple));
                        var minPeakSpacing = Math.Max(1, (int)Math.Round(windowLength * config.Evaluation.MinPeakSpacingWindowMultiple));
                        var streamPackage = BuildStreamsForCondition(config, task, taskIndex, seed, seedIndex, snrDb, snrIndex, windowLength, windowIndex);

                        foreach (var detectorConfig in config.Evaluation.Detectors)
                        {
                            var detector = DetectorFactory.Create(detectorConfig);
                            var metrics = new List<M7BStreamBoundaryMetrics>(streamPackage.Count);

                            foreach (var streamCase in streamPackage)
                            {
                                var scoreTrace = M7BChangePointAnalyzer.ComputeWindowScores(streamCase.Stream, windowLength, stride, detector, detectorConfig);
                                var proposals = M7BChangePointAnalyzer.ProposeBoundaries(
                                    scoreTrace,
                                    streamCase.Stream.Length,
                                    windowLength,
                                    stride,
                                    config.Evaluation.PeakThresholdMadMultiplier,
                                    minPeakSpacing,
                                    config.Evaluation.MaxBoundaryProposals);
                                metrics.Add(M7BChangePointAnalyzer.EvaluateBoundaries(proposals, streamCase.TruthBoundaries, tolerance));
                            }

                            rows.Add((task.Name, seed, snrDb, windowLength, detectorConfig.Name, metrics));
                        }
                    }
                }
            }
        }

        return rows;
    }

    private IReadOnlyList<(double[] Stream, IReadOnlyList<int> TruthBoundaries)> BuildStreamsForCondition(
        M7BChangePointConfig config,
        M7BStreamTaskConfig task,
        int taskIndex,
        int seed,
        int seedIndex,
        double snrDb,
        int snrIndex,
        int windowLength,
        int windowIndex)
    {
        var streams = new List<(double[] Stream, IReadOnlyList<int> TruthBoundaries)>(config.Evaluation.StreamCountPerCondition);

        for (var streamIndex = 0; streamIndex < config.Evaluation.StreamCountPerCondition; streamIndex++)
        {
            var parts = new List<double[]>();
            var truthBoundaries = new List<int>();
            var offset = 0;

            for (var regimeIndex = 0; regimeIndex < task.Regimes.Count; regimeIndex++)
            {
                var regime = task.Regimes[regimeIndex];
                var regimeSeed = SeedMath.Combine(seed, 1701, taskIndex, seedIndex, snrIndex, windowIndex, streamIndex, regimeIndex);
                var materializedCase = MaterializeCase(regime, snrDb);
                var benchmark = new SyntheticBenchmarkConfig(
                    regime.LengthSamples,
                    config.Benchmark.Noise,
                    [materializedCase]);
                var segment = _streamBuilder.BuildStream(regimeSeed, regimeIndex, benchmark, materializedCase);
                parts.Add(segment);
                offset += regime.LengthSamples;
                if (regimeIndex < task.Regimes.Count - 1)
                {
                    truthBoundaries.Add(offset);
                }
            }

            var stream = new double[parts.Sum(part => part.Length)];
            var destinationOffset = 0;
            foreach (var part in parts)
            {
                Array.Copy(part, 0, stream, destinationOffset, part.Length);
                destinationOffset += part.Length;
            }

            if (stream.Length < windowLength * 2)
            {
                throw new InvalidOperationException($"Task '{task.Name}' produced a stream shorter than two analysis windows for windowLength={windowLength}.");
            }

            streams.Add((stream, truthBoundaries));
        }

        return streams;
    }

    private static SyntheticCaseConfig MaterializeCase(M7BRegimeConfig regime, double conditionSnrDb)
    {
        if (!regime.ApplyConditionSnr
            || string.Equals(regime.SyntheticCase.SourceType, ExperimentConfigValidator.NoiseOnlySourceType, StringComparison.OrdinalIgnoreCase))
        {
            return regime.SyntheticCase;
        }

        return regime.SyntheticCase with { SnrDb = conditionSnrDb };
    }

    private static string CreateRunDirectory(string outputRoot, string experimentId, DateTimeOffset utcTimestamp)
    {
        var safeExperimentId = string.Concat(experimentId.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-'));
        var baseFolderName = $"{utcTimestamp:yyyyMMddTHHmmssZ}_{safeExperimentId}_seedpanel";
        var candidate = Path.Combine(outputRoot, baseFolderName);

        if (!Directory.Exists(candidate))
        {
            return candidate;
        }

        for (var collisionIndex = 2; collisionIndex < int.MaxValue; collisionIndex++)
        {
            candidate = Path.Combine(outputRoot, $"{baseFolderName}_{collisionIndex}");
            if (!Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Could not create a unique M7b run directory beneath '{outputRoot}'.");
    }
}
