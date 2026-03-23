using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Execution;

public sealed class M5B2ExplorationExperimentApplication
{
    private readonly IRunClock _clock;
    private readonly ExperimentApplication _seedApplication;
    private readonly EnvironmentSummaryProvider _environmentSummaryProvider;
    private readonly GitCommitResolver _gitCommitResolver;

    public M5B2ExplorationExperimentApplication(
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

    public string Run(M5B2ExplorationConfig config, string configPath, string repositoryRoot)
    {
        var validationErrors = M5B2ExplorationConfigValidator.Validate(config);
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

        var seedRuns = new List<M5B2SeedRunRecord>();
        var runResults = new List<(M5B2PerturbationConfig Perturbation, int Seed, ExperimentResult Result)>();

        foreach (var perturbation in config.Perturbations)
        {
            var perturbationRoot = Path.Combine(runDirectory, ".m5b2-temp", perturbation.Id);
            Directory.CreateDirectory(perturbationRoot);

            foreach (var seed in config.SeedPanel)
            {
                var seededConfig = config.ToSeededExperimentConfig(seed, perturbation) with
                {
                    OutputDirectory = perturbationRoot,
                    ManifestMetadata = WithPerturbationMetadata(config.ManifestMetadata, perturbation)
                };

                var run = _seedApplication.Run(seededConfig, fullConfigPath, repositoryRoot);
                seedRuns.Add(new M5B2SeedRunRecord(
                    perturbation.Id,
                    perturbation.AxisTag,
                    seed));
                runResults.Add((perturbation, seed, run.Result));
            }
        }

        DeleteTemporaryRunArtifacts(Path.Combine(runDirectory, ".m5b2-temp"));

        var artifacts = M5B2RepresentationPerturbationAxisReportBuilder.Build(config, runResults);
        var comparisonPath = Path.Combine(runDirectory, "m5b2_auc_comparison.csv");
        var deltaSummaryPath = Path.Combine(runDirectory, "m5b2_delta_summary.csv");
        var axisSummaryPath = Path.Combine(runDirectory, "m5b2_axis_summary.csv");
        var findingsPath = Path.Combine(runDirectory, "m5b2_findings.md");
        var manifestPath = Path.Combine(runDirectory, "manifest.json");

        M5B2RepresentationPerturbationAxisReportBuilder.WriteComparisonCsv(comparisonPath, artifacts.ComparisonRows);
        M5B2RepresentationPerturbationAxisReportBuilder.WriteDeltaSummaryCsv(deltaSummaryPath, artifacts.DeltaSummaryRows);
        M5B2RepresentationPerturbationAxisReportBuilder.WriteAxisSummaryCsv(axisSummaryPath, artifacts.AxisSummaryRows);
        File.WriteAllText(findingsPath, artifacts.FindingsMarkdown);

        var manifest = new M5B2ExplorationManifest(
            config.ExperimentId,
            config.ExperimentName,
            clockTime,
            config.SeedPanel.ToArray(),
            config.Perturbations.Select(perturbation => perturbation.Id).ToArray(),
            gitCommit,
            _environmentSummaryProvider.Create(),
            Path.GetRelativePath(runDirectory, fullConfigPath),
            config.Scenario.Name,
            config.Evaluation.TrialCountPerCondition,
            [
                "manifest.json",
                "m5b2_auc_comparison.csv",
                "m5b2_delta_summary.csv",
                "m5b2_axis_summary.csv",
                "m5b2_findings.md"
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

    private static void DeleteTemporaryRunArtifacts(string temporaryRoot)
    {
        if (!Directory.Exists(temporaryRoot))
        {
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(temporaryRoot, "*", SearchOption.AllDirectories))
        {
            File.Delete(filePath);
        }

        foreach (var directoryPath in Directory.EnumerateDirectories(temporaryRoot, "*", SearchOption.AllDirectories).OrderByDescending(path => path.Length))
        {
            Directory.Delete(directoryPath, false);
        }

        Directory.Delete(temporaryRoot, false);
    }

    private static ManifestMetadataConfig WithPerturbationMetadata(ManifestMetadataConfig metadata, M5B2PerturbationConfig perturbation)
    {
        var tags = metadata.Tags is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata.Tags, StringComparer.OrdinalIgnoreCase);

        tags["perturbationId"] = perturbation.Id;
        tags["perturbationAxis"] = perturbation.AxisTag;
        tags["representationScale"] = perturbation.Representation.SampleScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        tags["representationFormat"] = perturbation.Representation.NumericFormat;

        return metadata with { Tags = tags };
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

        throw new InvalidOperationException($"Could not create a unique M5b2 run directory beneath '{outputRoot}'.");
    }
}
