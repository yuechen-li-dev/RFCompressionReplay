using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Execution;

public sealed class M6A1UsefulnessExperimentApplication
{
    private readonly IRunClock _clock;
    private readonly ExperimentApplication _seedApplication;
    private readonly EnvironmentSummaryProvider _environmentSummaryProvider;
    private readonly GitCommitResolver _gitCommitResolver;

    public M6A1UsefulnessExperimentApplication(
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

    public string Run(M6A1UsefulnessConfig config, string configPath, string repositoryRoot)
    {
        var validationErrors = M6A1UsefulnessConfigValidator.Validate(config);
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

        var tempSeedRoot = Path.Combine(runDirectory, ".m6a1-temp");
        Directory.CreateDirectory(tempSeedRoot);

        var warnings = new List<string>();
        var gitCommit = _gitCommitResolver.Resolve(repositoryRoot);
        if (gitCommit == "unknown")
        {
            warnings.Add("Git commit could not be resolved.");
        }

        var seedResults = new List<(int Seed, ExperimentResult Result)>();

        try
        {
            foreach (var seed in config.SeedPanel)
            {
                var seededConfig = config.ToSeededExperimentConfig(seed) with
                {
                    OutputDirectory = tempSeedRoot
                };

                var run = _seedApplication.Run(seededConfig, fullConfigPath, repositoryRoot);
                seedResults.Add((seed, run.Result));
                Directory.Delete(run.RunDirectory, true);
            }
        }
        finally
        {
            if (Directory.Exists(tempSeedRoot))
            {
                Directory.Delete(tempSeedRoot, true);
            }
        }

        var artifacts = M6A1UsefulnessReportBuilder.Build(config, seedResults);
        var comparisonPath = Path.Combine(runDirectory, "m6a1_auc_comparison.csv");
        var taskSummaryPath = Path.Combine(runDirectory, "m6a1_task_summary.csv");
        var findingsPath = Path.Combine(runDirectory, "m6a1_findings.md");
        var manifestPath = Path.Combine(runDirectory, "manifest.json");

        M6A1UsefulnessReportBuilder.WriteComparisonCsv(comparisonPath, artifacts.ComparisonRows);
        M6A1UsefulnessReportBuilder.WriteTaskSummaryCsv(taskSummaryPath, artifacts.TaskSummaryRows);
        File.WriteAllText(findingsPath, artifacts.FindingsMarkdown);

        var manifest = new M6A1UsefulnessManifest(
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
                "m6a1_auc_comparison.csv",
                "m6a1_task_summary.csv",
                "m6a1_findings.md"
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
                "M6a1 keeps only the top-level manifest, AUC comparison CSV, task summary CSV, and findings markdown by default. Re-run the same config through the underlying seeded experiment path if you need per-seed summary or ROC artifacts."));

        ExperimentConfigJson.Save(manifestPath, manifest);
        return runDirectory;
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

        throw new InvalidOperationException($"Could not create a unique M6a1 run directory beneath '{outputRoot}'.");
    }
}
