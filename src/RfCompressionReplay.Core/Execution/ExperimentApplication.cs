using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
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
            TrialCount: config.TrialCount,
            ArtifactPaths: Array.Empty<string>(),
            Warnings: warnings,
            Metadata: new ManifestMetadata(
                config.ManifestMetadata.Notes,
                config.ManifestMetadata.VersionTag,
                config.ManifestMetadata.Tags));

        var artifactPaths = _artifactFileWriter.WriteRunArtifacts(runDirectory, result, manifestTemplate);
        var manifest = manifestTemplate with
        {
            ArtifactPaths = new[]
            {
                Path.GetRelativePath(runDirectory, artifactPaths.ManifestPath),
                Path.GetRelativePath(runDirectory, artifactPaths.SummaryPath),
                Path.GetRelativePath(runDirectory, artifactPaths.TrialsCsvPath),
            }
        };

        ExperimentConfigJson.Save(artifactPaths.ManifestPath, manifest);
        var finalizedResult = result with { Artifacts = artifactPaths };
        return new RunExecutionResult(manifest, finalizedResult, runDirectory);
    }

    private static IExperimentScenario CreateScenario(ExperimentConfig config)
    {
        IDetector detector = DetectorFactory.Create(config.Detector);

        if (string.Equals(config.Scenario.Name, ExperimentConfigValidator.DummyScenarioName, StringComparison.OrdinalIgnoreCase))
        {
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

            return new SyntheticBenchmarkScenario(streamBuilder, new ConsecutiveWindowSampler(), detector);
        }

        throw new InvalidOperationException($"Scenario '{config.Scenario.Name}' is not supported in M2. Supported scenarios: {ExperimentConfigValidator.DummyScenarioName}, {ExperimentConfigValidator.SyntheticBenchmarkScenarioName}.");
    }
}
