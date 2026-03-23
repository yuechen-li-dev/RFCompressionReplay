using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Execution;

public sealed class M5A3StabilityExperimentApplication
{
    private readonly IRunClock _clock;
    private readonly ExperimentApplication _seedApplication;
    private readonly EnvironmentSummaryProvider _environmentSummaryProvider;
    private readonly GitCommitResolver _gitCommitResolver;

    public M5A3StabilityExperimentApplication(
        IRunClock clock,
        ExperimentApplication seedApplication,
        EnvironmentSummaryProvider environmentSummaryProvider,
        GitCommitResolver gitCommitResolver)
    {
        _clock = clock;
        _seedApplication = seedApplication;
        _environmentSummaryProvider = environmentSummaryProvider;
        _gitCommitResolver = gitCommitResolver;
    }

    public string Run(M5A3StabilityConfig config, string configPath, string repositoryRoot)
    {
        var validationErrors = M5A3StabilityConfigValidator.Validate(config);
        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException("Configuration validation failed: " + string.Join(" ", validationErrors));
        }

        var clockTime = _clock.UtcNow;
        var fullConfigPath = Path.GetFullPath(configPath);
        var configDirectory = Path.GetDirectoryName(fullConfigPath) ?? Directory.GetCurrentDirectory();
        var outputRoot = Path.GetFullPath(config.OutputDirectory, configDirectory);
        var runDirectory = CreateSeedPanelRunDirectory(outputRoot, config.ExperimentId, clockTime);
        Directory.CreateDirectory(runDirectory);

        var seedRunRoot = Path.Combine(runDirectory, "seed-runs");
        Directory.CreateDirectory(seedRunRoot);

        var warnings = new List<string>();
        var gitCommit = _gitCommitResolver.Resolve(repositoryRoot);
        if (gitCommit == "unknown")
        {
            warnings.Add("Git commit could not be resolved.");
        }

        var seedRuns = new List<M5A3SeedRunRecord>();
        var seedResults = new List<(int Seed, ExperimentResult Result)>();

        foreach (var seed in config.SeedPanel)
        {
            var seededConfig = config.ToSeededExperimentConfig(seed) with
            {
                OutputDirectory = seedRunRoot
            };

            var run = _seedApplication.Run(seededConfig, fullConfigPath, repositoryRoot);
            seedRuns.Add(new M5A3SeedRunRecord(
                seed,
                Path.GetRelativePath(runDirectory, run.RunDirectory),
                Path.GetRelativePath(runDirectory, Path.Combine(run.RunDirectory, "manifest.json"))));
            seedResults.Add((seed, run.Result));
        }

        var artifacts = M5A3StabilityReportBuilder.Build(config, seedResults);
        var comparisonPath = Path.Combine(runDirectory, "m5a3_auc_comparison.csv");
        var deltaSummaryPath = Path.Combine(runDirectory, "m5a3_delta_summary.csv");
        var stabilitySummaryPath = Path.Combine(runDirectory, "m5a3_stability_summary.csv");
        var findingsPath = Path.Combine(runDirectory, "m5a3_findings.md");
        var manifestPath = Path.Combine(runDirectory, "manifest.json");

        M5A3StabilityReportBuilder.WriteComparisonCsv(comparisonPath, artifacts.ComparisonRows);
        M5A3StabilityReportBuilder.WriteDeltaSummaryCsv(deltaSummaryPath, artifacts.DeltaSummaryRows);
        M5A3StabilityReportBuilder.WriteStabilitySummaryCsv(stabilitySummaryPath, artifacts.StabilitySummaryRows);
        File.WriteAllText(findingsPath, artifacts.FindingsMarkdown);

        var manifest = new M5A3StabilityManifest(
            config.ExperimentId,
            config.ExperimentName,
            clockTime,
            config.SeedPanel.ToArray(),
            gitCommit,
            _environmentSummaryProvider.Create(),
            Path.GetRelativePath(runDirectory, fullConfigPath),
            config.Scenario.Name,
            config.Evaluation.TrialCountPerCondition,
            [
                "manifest.json",
                "m5a3_auc_comparison.csv",
                "m5a3_delta_summary.csv",
                "m5a3_stability_summary.csv",
                "m5a3_findings.md"
            ],
            warnings,
            new ManifestMetadata(
                config.ManifestMetadata.Notes,
                config.ManifestMetadata.VersionTag,
                config.ManifestMetadata.Tags),
            new EvaluationManifest(
                config.Evaluation.Tasks.Select(task => task.Name).ToArray(),
                config.Evaluation.Detectors.Select(detector => detector.Name).ToArray(),
                config.Evaluation.SnrDbValues.ToArray(),
                config.Evaluation.WindowLengths.ToArray(),
                config.Evaluation.TrialCountPerCondition),
            new ArtifactRetentionManifest(
                config.ArtifactRetentionMode,
                ArtifactRetentionPlan.Create(config.ArtifactRetentionMode).OmittedArtifactKinds,
                ArtifactRetentionPlan.Create(config.ArtifactRetentionMode).RegenerationNote),
            seedRuns);

        ExperimentConfigJson.Save(manifestPath, manifest);
        return runDirectory;
    }

    private static string CreateSeedPanelRunDirectory(string outputRoot, string experimentId, DateTimeOffset utcTimestamp)
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

        throw new InvalidOperationException($"Could not create a unique M5a3 run directory beneath '{outputRoot}'.");
    }
}
