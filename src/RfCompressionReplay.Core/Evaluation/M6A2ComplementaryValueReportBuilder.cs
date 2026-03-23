using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M6A2ComplementaryValueReportBuilder
{
    private static readonly string[] BaselineDetectors =
    [
        DetectorCatalog.EnergyDetectorName,
        DetectorCatalog.CovarianceAbsoluteValueDetectorName,
    ];

    private static readonly string[] CompressionDetectors =
    [
        DetectorCatalog.LzmsaPaperDetectorName,
        DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
    ];

    public static M6A2ComplementaryValueArtifacts Build(
        M6A2ComplementaryValueConfig config,
        IReadOnlyList<(int Seed, ExperimentResult Result)> seedResults,
        TinyLogisticRegressionBundleEvaluator bundleEvaluator)
    {
        var comparisonRows = seedResults
            .SelectMany(run => run.Result.Summary.Groups
                .Where(summary => summary.TaskName is not null && summary.ConditionSnrDb.HasValue && summary.WindowLength.HasValue && summary.Auc.HasValue)
                .Select(summary => new M6A2AucComparisonRow(
                    summary.TaskName!,
                    run.Seed,
                    summary.ConditionSnrDb!.Value,
                    summary.WindowLength!.Value,
                    summary.DetectorName,
                    Round(summary.Auc!.Value))))
            .OrderBy(row => row.TaskFamilyId, StringComparer.Ordinal)
            .ThenBy(row => row.Seed)
            .ThenBy(row => row.SnrDb)
            .ThenBy(row => row.WindowLength)
            .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
            .ToArray();

        var bundleConditionRows = bundleEvaluator.Evaluate(seedResults, config.Bundles);
        var bundleSummaryRows = config.Evaluation.Tasks
            .SelectMany(task => config.Bundles.Select(bundle => BuildBundleSummaryRow(task.Name, bundle.Id, bundleConditionRows)))
            .OrderBy(row => row.TaskFamilyId, StringComparer.Ordinal)
            .ThenByDescending(row => row.MedianAuc)
            .ThenByDescending(row => row.BestOrTiedBestConditionCount)
            .ThenBy(row => row.BundleId, StringComparer.Ordinal)
            .ToArray();

        return new M6A2ComplementaryValueArtifacts(
            comparisonRows,
            bundleConditionRows,
            bundleSummaryRows,
            BuildFindingsMarkdown(config, comparisonRows, bundleConditionRows, bundleSummaryRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M6A2AucComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskFamilyId,seed,snrDb,windowLength,detectorId,auc");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskFamilyId,
                row.Seed.ToString(CultureInfo.InvariantCulture),
                row.SnrDb.ToString("0.###", CultureInfo.InvariantCulture),
                row.WindowLength.ToString(CultureInfo.InvariantCulture),
                row.DetectorId,
                row.Auc.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteBundleSummaryCsv(string path, IReadOnlyList<M6A2BundleSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskFamilyId,bundleId,medianAuc,maxAuc,bestOrTiedBestConditionCount,conditionCount,conditionWinsOverBundleA,medianImprovementVsBundleA,maxImprovementVsBundleA,comparisonNote");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskFamilyId,
                row.BundleId,
                row.MedianAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.BestOrTiedBestConditionCount.ToString(CultureInfo.InvariantCulture),
                row.ConditionCount.ToString(CultureInfo.InvariantCulture),
                row.ConditionWinsOverBundleA.ToString(CultureInfo.InvariantCulture),
                row.MedianImprovementVsBundleA.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxImprovementVsBundleA.ToString("F6", CultureInfo.InvariantCulture),
                Escape(row.ComparisonNote)));
        }
    }

    private static M6A2BundleSummaryRow BuildBundleSummaryRow(
        string taskFamilyId,
        string bundleId,
        IReadOnlyList<M6A2BundleConditionRow> bundleConditionRows)
    {
        var rows = bundleConditionRows
            .Where(row => string.Equals(row.TaskFamilyId, taskFamilyId, StringComparison.Ordinal)
                && string.Equals(row.BundleId, bundleId, StringComparison.Ordinal))
            .ToArray();
        var groupedConditions = bundleConditionRows
            .Where(row => string.Equals(row.TaskFamilyId, taskFamilyId, StringComparison.Ordinal))
            .GroupBy(row => (row.Seed, row.SnrDb, row.WindowLength))
            .ToArray();
        var bundleAByCondition = bundleConditionRows
            .Where(row => string.Equals(row.TaskFamilyId, taskFamilyId, StringComparison.Ordinal)
                && string.Equals(row.BundleId, M6A2ComplementaryValueConfigValidator.BundleAId, StringComparison.Ordinal))
            .ToDictionary(row => (row.Seed, row.SnrDb, row.WindowLength), row => row.Auc);
        var improvements = rows
            .Select(row => row.Auc - bundleAByCondition[(row.Seed, row.SnrDb, row.WindowLength)])
            .OrderBy(value => value)
            .ToArray();

        return new M6A2BundleSummaryRow(
            taskFamilyId,
            bundleId,
            rows.Length == 0 ? 0d : Round(Median(rows.Select(row => row.Auc).OrderBy(value => value).ToArray())),
            rows.Length == 0 ? 0d : Round(rows.Max(row => row.Auc)),
            groupedConditions.Count(condition => IsBestOrTiedBest(condition, bundleId)),
            groupedConditions.Length,
            improvements.Count(value => value >= 0.001d),
            improvements.Length == 0 ? 0d : Round(Median(improvements)),
            improvements.Length == 0 ? 0d : Round(improvements.Max()),
            BuildBundleComparisonNote(bundleId, improvements));
    }

    private static string BuildFindingsMarkdown(
        M6A2ComplementaryValueConfig config,
        IReadOnlyList<M6A2AucComparisonRow> comparisonRows,
        IReadOnlyList<M6A2BundleConditionRow> bundleConditionRows,
        IReadOnlyList<M6A2BundleSummaryRow> bundleSummaryRows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# M6a2 Complementary-Value Usefulness Findings");
        builder.AppendLine();
        builder.AppendLine("## Scope");
        builder.AppendLine();
        builder.AppendLine($"- Task families: {string.Join(", ", config.Evaluation.Tasks.Select(task => $"`{task.Name}`"))}");
        builder.AppendLine($"- Detector panel: {string.Join(", ", config.Evaluation.Detectors.Select(detector => $"`{detector.Name}`"))}");
        builder.AppendLine($"- Bundle comparison: {string.Join(", ", config.Bundles.Select(bundle => $"`{bundle.Id}` = [{string.Join(", ", bundle.FeatureDetectors)}]"))}");
        builder.AppendLine("- Bundle readout: deterministic leave-one-seed-out logistic regression trained separately within each `(task family, SNR, window length)` condition and evaluated on the held-out seed only.");
        builder.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        builder.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        builder.AppendLine($"- Seeds: {string.Join(", ", config.SeedPanel)}");
        builder.AppendLine($"- Trial count per class per seed/condition: {config.Evaluation.TrialCountPerCondition}");
        builder.AppendLine($"- Retention mode used: `{config.ArtifactRetentionMode}` (top-level M6a2 retention keeps only manifest + compact standalone comparison + compact bundle summary + findings markdown)");
        builder.AppendLine($"- Config provenance: `{config.ExperimentId}` / `{config.ManifestMetadata.VersionTag}`");
        builder.AppendLine();
        builder.AppendLine("## Standalone Detector Read");
        builder.AppendLine();

        foreach (var task in config.Evaluation.Tasks)
        {
            var rows = comparisonRows.Where(row => string.Equals(row.TaskFamilyId, task.Name, StringComparison.Ordinal)).ToArray();
            var byDetector = rows
                .GroupBy(row => row.DetectorId, StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    DetectorId = group.Key,
                    MedianAuc = Round(Median(group.Select(row => row.Auc).OrderBy(value => value).ToArray())),
                    MaxAuc = Round(group.Max(row => row.Auc)),
                    BestOrTieCount = group.Count(row => IsBestOrTiedBest(rows.Where(candidate => candidate.Seed == row.Seed && candidate.SnrDb == row.SnrDb && candidate.WindowLength == row.WindowLength), row.DetectorId))
                })
                .OrderByDescending(row => row.MedianAuc)
                .ThenByDescending(row => row.BestOrTieCount)
                .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
                .ToArray();
            var bestBaseline = byDetector.First(row => BaselineDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase));
            var bestCompression = byDetector.First(row => CompressionDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase));
            var normalized = byDetector.Single(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase));
            var paper = byDetector.Single(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaPaperDetectorName, StringComparison.OrdinalIgnoreCase));

            builder.AppendLine($"### `{task.Name}`");
            builder.AppendLine();
            builder.AppendLine($"- Best baseline by median AUC: `{bestBaseline.DetectorId}` at {bestBaseline.MedianAuc:F3}.");
            builder.AppendLine($"- Best compression-derived standalone detector: `{bestCompression.DetectorId}` at {bestCompression.MedianAuc:F3}.");
            builder.AppendLine($"- RMS-normalized mean vs `lzmsa-paper`: {normalized.MedianAuc:F3} vs {paper.MedianAuc:F3} median AUC.");
            builder.AppendLine($"- Cautious read: {GetStandaloneTaskRead(bestBaseline.MedianAuc, bestCompression.MedianAuc, normalized.MedianAuc, paper.MedianAuc)}");
            builder.AppendLine();
        }

        builder.AppendLine("## Bundle Read");
        builder.AppendLine();

        foreach (var task in config.Evaluation.Tasks)
        {
            var rows = bundleSummaryRows.Where(row => string.Equals(row.TaskFamilyId, task.Name, StringComparison.Ordinal)).ToArray();
            var bundleA = rows.Single(row => string.Equals(row.BundleId, M6A2ComplementaryValueConfigValidator.BundleAId, StringComparison.Ordinal));
            var bundleB = rows.Single(row => string.Equals(row.BundleId, M6A2ComplementaryValueConfigValidator.BundleBId, StringComparison.Ordinal));

            builder.AppendLine($"### `{task.Name}`");
            builder.AppendLine();
            builder.AppendLine($"- Bundle A `[ED, CAV]`: median AUC {bundleA.MedianAuc:F3}, max AUC {bundleA.MaxAuc:F3}.");
            builder.AppendLine($"- Bundle B `[ED, CAV, RMS-normalized mean compressed byte value]`: median AUC {bundleB.MedianAuc:F3}, max AUC {bundleB.MaxAuc:F3}.");
            builder.AppendLine($"- Bundle B minus Bundle A: median {bundleB.MedianImprovementVsBundleA:F3}, max {bundleB.MaxImprovementVsBundleA:F3}, with {bundleB.ConditionWinsOverBundleA}/{bundleB.ConditionCount} held-out seed conditions at or above Bundle A by at least 0.001 AUC.");
            builder.AppendLine($"- Cautious read: {GetBundleTaskRead(bundleB.MedianImprovementVsBundleA, bundleB.MaxImprovementVsBundleA, bundleB.ConditionWinsOverBundleA, bundleB.ConditionCount)}");
            builder.AppendLine();
        }

        var standaloneCompetitiveTasks = config.Evaluation.Tasks.Count(task =>
        {
            var baselineMedian = GetDetectorMedian(comparisonRows, task.Name, BaselineDetectors);
            var compressionMedian = GetDetectorMedian(comparisonRows, task.Name, CompressionDetectors);
            return compressionMedian >= baselineMedian - 0.03d;
        });
        var bundleRowsB = bundleSummaryRows.Where(row => string.Equals(row.BundleId, M6A2ComplementaryValueConfigValidator.BundleBId, StringComparison.Ordinal)).ToArray();
        var bundleRowsA = bundleSummaryRows.Where(row => string.Equals(row.BundleId, M6A2ComplementaryValueConfigValidator.BundleAId, StringComparison.Ordinal)).ToArray();

        builder.AppendLine("## Overall Conclusion");
        builder.AppendLine();
        builder.AppendLine($"- {GetOverallConclusion(standaloneCompetitiveTasks, config.Evaluation.Tasks.Count, bundleRowsA, bundleRowsB)}");
        builder.AppendLine();
        builder.AppendLine("## Caveats");
        builder.AppendLine();
        builder.AppendLine("- This remains a synthetic-only benchmark suite.");
        builder.AppendLine("- The engineered structured processes are OFDM-like / repeated-frame-like synthetic constructions, not LTE-faithful signals.");
        builder.AppendLine("- The current deterministic compression-backend caveat remains in force.");
        builder.AppendLine("- No SDR-facing or deployment-readiness claim is made here.");
        builder.AppendLine("- M6a2 is still usefulness mapping inside the current harness, not deployment proof.");

        return builder.ToString();
    }

    private static string GetStandaloneTaskRead(double bestBaselineMedianAuc, double bestCompressionMedianAuc, double normalizedMedianAuc, double paperMedianAuc)
    {
        var gap = bestCompressionMedianAuc - bestBaselineMedianAuc;
        if (gap >= 0.03d)
        {
            return "compression-derived standalone features became genuinely competitive here and sometimes surpassed the strongest baseline within the tested grid.";
        }

        if (gap >= -0.02d)
        {
            return "compression-derived standalone features became closer to ED/CAV than in M6a1, but ED/CAV still stayed slightly better on median AUC.";
        }

        if (normalizedMedianAuc >= paperMedianAuc - 0.01d)
        {
            return "ED/CAV still led clearly, while RMS-normalized mean remained the stronger of the two compression summaries.";
        }

        return "ED/CAV still dominated and the compression-derived family remained secondary as a replacement detector.";
    }

    private static string GetBundleTaskRead(double medianImprovement, double maxImprovement, int winCount, int conditionCount)
    {
        if (medianImprovement >= 0.02d)
        {
            return "adding RMS-normalized mean gave a material bundle lift on this task family in the checked grid.";
        }

        if (medianImprovement > 0.005d || (medianImprovement >= 0d && winCount > conditionCount / 2))
        {
            return "adding RMS-normalized mean helped modestly and fairly consistently, which fits a complementary-feature interpretation better than a replacement-detector story.";
        }

        if (maxImprovement >= 0.02d)
        {
            return "the added feature occasionally helped, but the gain was condition-local rather than broad across the grid.";
        }

        return "adding RMS-normalized mean did not materially improve the simple ED+CAV bundle on this task family.";
    }

    private static string GetOverallConclusion(
        int standaloneCompetitiveTasks,
        int taskCount,
        IReadOnlyList<M6A2BundleSummaryRow> bundleRowsA,
        IReadOnlyList<M6A2BundleSummaryRow> bundleRowsB)
    {
        var medianBundleLift = Median(bundleRowsB.Select(row => row.MedianImprovementVsBundleA).OrderBy(value => value).ToArray());
        var bestBundleLift = bundleRowsB.Max(row => row.MaxImprovementVsBundleA);

        if (medianBundleLift >= 0.02d)
        {
            return "Within this synthetic suite, compression-derived features looked more useful as complementary inputs than as replacement detectors: standalone ED/CAV stayed strong overall, but adding RMS-normalized mean to ED+CAV produced a material median bundle gain on the fairer engineered-structure-vs-natural-correlation tasks.";
        }

        if (medianBundleLift > 0d || bestBundleLift >= 0.02d)
        {
            return standaloneCompetitiveTasks > 0
                ? "Within this synthetic suite, compression-derived standalone detectors became somewhat more competitive on the fairer task families, but their more practical role still looks complementary: RMS-normalized mean added modest value to the tiny ED+CAV bundle more clearly than it replaced ED or CAV outright."
                : "Within this synthetic suite, compression-derived features remained weak as standalone replacements for ED/CAV, but RMS-normalized mean showed at least modest complementary value inside the tiny ED+CAV bundle on part of the fairer task grid.";
        }

        return standaloneCompetitiveTasks == taskCount
            ? "Within this synthetic suite, compression-derived standalone detectors became more competitive on the fairer task families, but adding RMS-normalized mean to ED+CAV did not materially improve the tiny bundle, so the complementary-value claim stayed weak."
            : "Within this synthetic suite, compression-derived features did not materially improve over ED/CAV as standalone detectors or as an added tiny-bundle feature, even on the fairer engineered-structure-vs-natural-correlation tasks.";
    }

    private static bool IsBestOrTiedBest(IEnumerable<M6A2AucComparisonRow> rows, string detectorId)
    {
        var conditionRows = rows.ToArray();
        var best = conditionRows.Max(row => row.Auc);
        return conditionRows.Any(row => string.Equals(row.DetectorId, detectorId, StringComparison.OrdinalIgnoreCase)
            && Math.Abs(row.Auc - best) <= 0.000001d);
    }

    private static bool IsBestOrTiedBest(IGrouping<(int Seed, double SnrDb, int WindowLength), M6A2BundleConditionRow> rows, string bundleId)
    {
        var conditionRows = rows.ToArray();
        var best = conditionRows.Max(row => row.Auc);
        return conditionRows.Any(row => string.Equals(row.BundleId, bundleId, StringComparison.Ordinal)
            && Math.Abs(row.Auc - best) <= 0.000001d);
    }

    private static double GetDetectorMedian(
        IReadOnlyList<M6A2AucComparisonRow> rows,
        string taskFamilyId,
        IReadOnlyList<string> detectorIds)
    {
        return rows
            .Where(row => string.Equals(row.TaskFamilyId, taskFamilyId, StringComparison.Ordinal)
                && detectorIds.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase))
            .GroupBy(row => row.DetectorId, StringComparer.OrdinalIgnoreCase)
            .Select(group => Median(group.Select(row => row.Auc).OrderBy(value => value).ToArray()))
            .DefaultIfEmpty(0d)
            .Max();
    }

    private static string BuildBundleComparisonNote(string bundleId, IReadOnlyList<double> improvements)
    {
        if (string.Equals(bundleId, M6A2ComplementaryValueConfigValidator.BundleAId, StringComparison.Ordinal))
        {
            return "Reference ED+CAV bundle.";
        }

        var medianImprovement = improvements.Count == 0 ? 0d : Median(improvements);
        if (medianImprovement >= 0.02d)
        {
            return "RMS-normalized mean provided a material median lift over ED+CAV alone.";
        }

        if (medianImprovement > 0d)
        {
            return "RMS-normalized mean provided a modest median lift over ED+CAV alone.";
        }

        if (improvements.Any(value => value > 0d))
        {
            return "RMS-normalized mean helped in some conditions, but not on the median condition.";
        }

        return "RMS-normalized mean did not improve the ED+CAV reference bundle in this task-family aggregate.";
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0d;
        }

        var middle = values.Count / 2;
        if (values.Count % 2 == 1)
        {
            return values[middle];
        }

        return (values[middle - 1] + values[middle]) / 2d;
    }

    private static double Round(double value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
