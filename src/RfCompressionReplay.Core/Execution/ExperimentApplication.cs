using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Experiments;
using RfCompressionReplay.Core.Models;
using RfCompressionReplay.Core.Randomness;
using RfCompressionReplay.Core.Signals;
using RfCompressionReplay.Core.Signals.Synthetic;

namespace RfCompressionReplay.Core.Execution;

public sealed class ExperimentApplication
{
    private readonly IRunClock _clock;
    private readonly RunDirectoryFactory _runDirectoryFactory;
    private readonly ArtifactFileWriter _artifactFileWriter;
    private readonly EnvironmentSummaryProvider _environmentSummaryProvider;
    private readonly GitCommitResolver _gitCommitResolver;

    public ExperimentApplication(
        IRunClock clock,
        RunDirectoryFactory runDirectoryFactory,
        ArtifactFileWriter artifactFileWriter,
        EnvironmentSummaryProvider environmentSummaryProvider,
        GitCommitResolver gitCommitResolver)
    {
        _clock = clock;
        _runDirectoryFactory = runDirectoryFactory;
        _artifactFileWriter = artifactFileWriter;
        _environmentSummaryProvider = environmentSummaryProvider;
        _gitCommitResolver = gitCommitResolver;
    }

    public RunExecutionResult Run(ExperimentConfig config, string configPath, string repositoryRoot)
    {
        var validationErrors = ExperimentConfigValidator.Validate(config);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException("Configuration validation failed: " + string.Join(" ", validationErrors));
        }

        var retention = ArtifactRetentionPlan.Create(config.ArtifactRetentionMode);
        var fullConfigPath = Path.GetFullPath(configPath);
        var configDirectory = Path.GetDirectoryName(fullConfigPath) ?? Directory.GetCurrentDirectory();
        var clockTime = _clock.UtcNow;
        var random = new SeededRandom(config.Seed);
        var scenario = CreateScenario(config);
        var result = scenario.Execute(config, random);

        var outputRoot = Path.GetFullPath(config.OutputDirectory, configDirectory);
        var runDirectory = _runDirectoryFactory.Create(outputRoot, config, clockTime);
        var warnings = new List<string>();
        var gitCommit = _gitCommitResolver.Resolve(repositoryRoot);
        if (gitCommit == "unknown")
        {
            warnings.Add("Git commit could not be resolved.");
        }

        var manifestTemplate = new RunManifest(
            ExperimentId: config.ExperimentId,
            ExperimentName: config.ExperimentName,
            UtcTimestamp: clockTime,
            Seed: config.Seed,
            GitCommit: gitCommit,
            Environment: _environmentSummaryProvider.Create(),
            ConfigFilePath: Path.GetRelativePath(runDirectory, fullConfigPath),
            ScenarioName: config.Scenario.Name,
            TrialCount: config.Evaluation?.TrialCountPerCondition ?? config.TrialCount,
            ArtifactPaths: Array.Empty<string>(),
            Warnings: warnings,
            Metadata: new ManifestMetadata(
                config.ManifestMetadata.Notes,
                config.ManifestMetadata.VersionTag,
                config.ManifestMetadata.Tags),
            Evaluation: CreateEvaluationManifest(config),
            Retention: new ArtifactRetentionManifest(
                retention.Mode,
                retention.OmittedArtifactKinds,
                retention.RegenerationNote));

        var artifactPaths = _artifactFileWriter.WriteRunArtifacts(runDirectory, config, result, manifestTemplate);
        var manifestArtifactPaths = new List<string>
        {
            Path.GetRelativePath(runDirectory, artifactPaths.ManifestPath),
            Path.GetRelativePath(runDirectory, artifactPaths.SummaryPath),
            Path.GetRelativePath(runDirectory, artifactPaths.SummaryCsvPath),
        };

        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.TrialsCsvPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.RocPointsCsvPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.RocPointsCompactCsvPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.M4AucComparisonCsvPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.M4FindingsPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.M5A1AucComparisonCsvPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.M5A1FindingsPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.M5A1DeltaSummaryCsvPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.M5A2AucComparisonCsvPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.M5A2FindingsPath);
        AddIfPresent(manifestArtifactPaths, runDirectory, artifactPaths.M5A2DeltaSummaryCsvPath);

        var manifest = manifestTemplate with
        {
            ArtifactPaths = manifestArtifactPaths
        };

        ExperimentConfigJson.Save(artifactPaths.ManifestPath, manifest);
        var finalizedResult = result with { Artifacts = artifactPaths };
        return new RunExecutionResult(manifest, finalizedResult, runDirectory);
    }

    private static void AddIfPresent(List<string> artifactPaths, string runDirectory, string? artifactPath)
    {
        if (artifactPath is not null)
        {
            artifactPaths.Add(Path.GetRelativePath(runDirectory, artifactPath));
        }
    }

    private static EvaluationManifest? CreateEvaluationManifest(ExperimentConfig config)
    {
        if (config.Evaluation is null)
        {
            return null;
        }

        return new EvaluationManifest(
            TaskNames: config.Evaluation.Tasks.Select(task => task.Name).ToArray(),
            DetectorNames: config.Evaluation.Detectors.Select(detector => detector.Name).ToArray(),
            SnrDbValues: config.Evaluation.SnrDbValues.ToArray(),
            WindowLengths: config.Evaluation.WindowLengths.ToArray(),
            TrialCountPerCondition: config.Evaluation.TrialCountPerCondition);
    }

    private static IExperimentScenario CreateScenario(ExperimentConfig config)
    {
        if (string.Equals(config.Scenario.Name, ExperimentConfigValidator.DummyScenarioName, StringComparison.OrdinalIgnoreCase))
        {
            IDetector detector = DetectorFactory.Create(config.Detector, config.Representation);
            ISignalProvider signalProvider = new DummySignalProvider();
            return new DummyScenario(signalProvider, detector);
        }

        if (string.Equals(config.Scenario.Name, ExperimentConfigValidator.SyntheticBenchmarkScenarioName, StringComparison.OrdinalIgnoreCase))
        {
            var streamBuilder = new SyntheticCaseStreamBuilder(
                new GaussianNoiseGenerator(),
                new GaussianEmitterGenerator(),
                new OfdmLikeSignalGenerator(),
                new SnrMixer());

            if (config.Evaluation is not null)
            {
                return new SyntheticEvaluationScenario(streamBuilder, new ConsecutiveWindowSampler(), new RocAucCalculator());
            }

            IDetector detector = DetectorFactory.Create(config.Detector, config.Representation);
            return new SyntheticBenchmarkScenario(streamBuilder, new ConsecutiveWindowSampler(), detector);
        }

        throw new InvalidOperationException($"Scenario '{config.Scenario.Name}' is not supported in M3. Supported scenarios: {ExperimentConfigValidator.DummyScenarioName}, {ExperimentConfigValidator.SyntheticBenchmarkScenarioName}.");
    }
}
