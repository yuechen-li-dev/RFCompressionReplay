using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Execution;

public sealed class M6A2ComplementaryValueExperimentApplication
{
    private readonly IRunClock _clock;
    private readonly ExperimentApplication _seedApplication;
    private readonly EnvironmentSummaryProvider _environmentSummaryProvider;
    private readonly GitCommitResolver _gitCommitResolver;
    private readonly TinyLogisticRegressionBundleEvaluator _bundleEvaluator;

    public M6A2ComplementaryValueExperimentApplication(
        IRunClock clock,
        ExperimentApplication seedApplication,
        EnvironmentSummaryProvider environmentSummaryProvider,
        GitCommitResolver gitCommitResolver,
        TinyLogisticRegressionBundleEvaluator bundleEvaluator)
    {
        _clock = clock;
        _seedApplication = seedApplication;
        _environmentSummaryProvider = environmentSummaryProvider;
        _gitCommitResolver = gitCommitResolver;
        _bundleEvaluator = bundleEvaluator;
    }

    public string Run(M6A2ComplementaryValueConfig config, string configPath, string repositoryRoot)
    {
        var validationErrors = M6A2ComplementaryValueConfigValidator.Validate(config);
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

        var tempSeedRoot = Path.Combine(runDirectory, ".m6a2-temp");
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

        var artifacts = M6A2ComplementaryValueReportBuilder.Build(config, seedResults, _bundleEvaluator);
        var comparisonPath = Path.Combine(runDirectory, "m6a2_auc_comparison.csv");
        var bundleSummaryPath = Path.Combine(runDirectory, "m6a2_bundle_summary.csv");
        var findingsPath = Path.Combine(runDirectory, "m6a2_findings.md");
        var manifestPath = Path.Combine(runDirectory, "manifest.json");

        M6A2ComplementaryValueReportBuilder.WriteComparisonCsv(comparisonPath, artifacts.ComparisonRows);
        M6A2ComplementaryValueReportBuilder.WriteBundleSummaryCsv(bundleSummaryPath, artifacts.BundleSummaryRows);
        File.WriteAllText(findingsPath, artifacts.FindingsMarkdown);

        var manifest = new M6A2ComplementaryValueManifest(
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
                "m6a2_auc_comparison.csv",
                "m6a2_bundle_summary.csv",
                "m6a2_findings.md"
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
                "M6a2 keeps only the top-level manifest, standalone AUC comparison CSV, bundle summary CSV, and findings markdown by default. Re-run the same config through the underlying seeded experiment path if you need per-seed summary or ROC artifacts."));

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

        throw new InvalidOperationException($"Could not create a unique M6a2 run directory beneath '{outputRoot}'.");
    }
}
