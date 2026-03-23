using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M7BChangePointReportBuilder
{
    private static readonly string[] BaselineDetectors =
    [
        DetectorCatalog.EnergyDetectorName,
        DetectorCatalog.CovarianceAbsoluteValueDetectorName,
    ];

    public static M7BChangePointArtifacts Build(
        M7BChangePointConfig config,
        IReadOnlyList<(string TaskName, int Seed, double SnrDb, int WindowLength, string DetectorName, IReadOnlyList<M7BStreamBoundaryMetrics> Metrics)> conditionRows)
    {
        var comparisonRows = conditionRows
            .Select(row => new M7BBoundaryComparisonRow(
                row.TaskName,
                row.Seed,
                row.SnrDb,
                row.WindowLength,
                row.DetectorName,
                Round(row.Metrics.Count(metric => metric.OnsetHit) / (double)row.Metrics.Count),
                HasOffset(row.Metrics) ? Round(row.Metrics.Count(metric => metric.OffsetHit == true) / (double)row.Metrics.Count) : null,
                NullableMedian(row.Metrics.Select(metric => metric.OnsetLocalizationError)),
                HasOffset(row.Metrics) ? NullableMedian(row.Metrics.Select(metric => metric.OffsetLocalizationError)) : null,
                Round(Median(row.Metrics.Select(metric => (double)metric.FalsePositiveCount))),
                NullableMedian(row.Metrics.Select(metric => metric.OnsetDetectionDelay)),
                HasOffset(row.Metrics) ? NullableMedian(row.Metrics.Select(metric => metric.OffsetDetectionDelay)) : null,
                row.Metrics.Count))
            .OrderBy(row => row.TaskFamilyId, StringComparer.Ordinal)
            .ThenBy(row => row.Seed)
            .ThenBy(row => row.SnrDb)
            .ThenBy(row => row.WindowLength)
            .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
            .ToArray();

        var taskSummaryRows = config.Benchmark.Tasks
            .SelectMany(task => config.Evaluation.Detectors.Select(detector => BuildTaskSummaryRow(task.Name, detector.Name, comparisonRows)))
            .OrderBy(row => row.TaskFamilyId, StringComparer.Ordinal)
            .ThenByDescending(row => row.MedianOnsetHitRate)
            .ThenBy(row => row.MedianFalsePositiveCount)
            .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
            .ToArray();

        return new M7BChangePointArtifacts(
            comparisonRows,
            taskSummaryRows,
            BuildFindingsMarkdown(config, comparisonRows, taskSummaryRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M7BBoundaryComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskFamilyId,seed,snrDb,windowLength,detectorId,onsetHitRate,offsetHitRate,medianOnsetLocalizationError,medianOffsetLocalizationError,medianFalsePositiveCount,medianOnsetDetectionDelay,medianOffsetDetectionDelay,streamCount");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskFamilyId,
                row.Seed.ToString(CultureInfo.InvariantCulture),
                row.SnrDb.ToString("0.###", CultureInfo.InvariantCulture),
                row.WindowLength.ToString(CultureInfo.InvariantCulture),
                row.DetectorId,
                row.OnsetHitRate.ToString("F6", CultureInfo.InvariantCulture),
                FormatNullable(row.OffsetHitRate),
                FormatNullable(row.MedianOnsetLocalizationError),
                FormatNullable(row.MedianOffsetLocalizationError),
                row.MedianFalsePositiveCount.ToString("F6", CultureInfo.InvariantCulture),
                FormatNullable(row.MedianOnsetDetectionDelay),
                FormatNullable(row.MedianOffsetDetectionDelay),
                row.StreamCount.ToString(CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteTaskSummaryCsv(string path, IReadOnlyList<M7BTaskSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskFamilyId,detectorId,medianOnsetHitRate,medianOffsetHitRate,medianOnsetLocalizationError,medianOffsetLocalizationError,medianFalsePositiveCount,bestOrTiedBestConditionCount,distinctHitVsBestBaselineConditionCount,comparisonNote");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskFamilyId,
                row.DetectorId,
                row.MedianOnsetHitRate.ToString("F6", CultureInfo.InvariantCulture),
                FormatNullable(row.MedianOffsetHitRate),
                FormatNullable(row.MedianOnsetLocalizationError),
                FormatNullable(row.MedianOffsetLocalizationError),
                row.MedianFalsePositiveCount.ToString("F6", CultureInfo.InvariantCulture),
                row.BestOrTiedBestConditionCount.ToString(CultureInfo.InvariantCulture),
                row.DistinctHitVsBestBaselineConditionCount.ToString(CultureInfo.InvariantCulture),
                Escape(row.ComparisonNote)));
        }
    }

    private static M7BTaskSummaryRow BuildTaskSummaryRow(string taskName, string detectorId, IReadOnlyList<M7BBoundaryComparisonRow> comparisonRows)
    {
        var detectorRows = comparisonRows
            .Where(row => string.Equals(row.TaskFamilyId, taskName, StringComparison.Ordinal)
                && string.Equals(row.DetectorId, detectorId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var groupedConditions = comparisonRows
            .Where(row => string.Equals(row.TaskFamilyId, taskName, StringComparison.Ordinal))
            .GroupBy(row => (row.Seed, row.SnrDb, row.WindowLength))
            .ToArray();

        return new M7BTaskSummaryRow(
            taskName,
            detectorId,
            detectorRows.Length == 0 ? 0d : Round(Median(detectorRows.Select(row => row.OnsetHitRate))),
            detectorRows.All(row => row.OffsetHitRate is null) ? null : NullableMedian(detectorRows.Select(row => row.OffsetHitRate)),
            NullableMedian(detectorRows.Select(row => row.MedianOnsetLocalizationError)),
            NullableMedian(detectorRows.Select(row => row.MedianOffsetLocalizationError)),
            detectorRows.Length == 0 ? 0d : Round(Median(detectorRows.Select(row => row.MedianFalsePositiveCount))),
            groupedConditions.Count(condition => IsBestOrTiedBest(condition, detectorId)),
            groupedConditions.Count(condition => HasDistinctHitVsBestBaseline(condition, detectorId)),
            BuildComparisonNote(detectorId, detectorRows, groupedConditions));
    }

    private static string BuildFindingsMarkdown(
        M7BChangePointConfig config,
        IReadOnlyList<M7BBoundaryComparisonRow> comparisonRows,
        IReadOnlyList<M7BTaskSummaryRow> taskSummaryRows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# M7b Change-Point / Segmentation Usefulness Findings");
        builder.AppendLine();
        builder.AppendLine("## Scope");
        builder.AppendLine();
        builder.AppendLine($"- Stream task families: {string.Join(", ", config.Benchmark.Tasks.Select(task => $"`{task.Name}`"))}");
        builder.AppendLine($"- Detector panel: {string.Join(", ", config.Evaluation.Detectors.Select(detector => $"`{detector.Name}`"))}");
        builder.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        builder.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        builder.AppendLine($"- Seeds: {string.Join(", ", config.SeedPanel)}");
        builder.AppendLine($"- Stream count per seed/condition: {config.Evaluation.StreamCountPerCondition}");
        builder.AppendLine($"- Retention mode used: `{config.ArtifactRetentionMode}` (compact summary-first: manifest + boundary comparison CSV + task summary CSV + findings markdown)");
        builder.AppendLine($"- Config provenance: `{config.ExperimentId}` / `{config.ManifestMetadata.VersionTag}`");
        builder.AppendLine();
        builder.AppendLine("## Task-Family Read");
        builder.AppendLine();

        foreach (var task in config.Benchmark.Tasks)
        {
            var rows = taskSummaryRows.Where(row => string.Equals(row.TaskFamilyId, task.Name, StringComparison.Ordinal)).ToArray();
            var bestOverall = rows
                .OrderByDescending(row => row.MedianOnsetHitRate)
                .ThenBy(row => row.MedianFalsePositiveCount)
                .ThenBy(row => row.MedianOnsetLocalizationError ?? double.MaxValue)
                .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
                .First();
            var bestBaseline = rows
                .Where(row => BaselineDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(row => row.MedianOnsetHitRate)
                .ThenBy(row => row.MedianFalsePositiveCount)
                .ThenBy(row => row.MedianOnsetLocalizationError ?? double.MaxValue)
                .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
                .First();
            var normalized = rows.Single(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase));

            builder.AppendLine($"### `{task.Name}`");
            builder.AppendLine();
            builder.AppendLine($"- Best overall boundary cue by median onset hit rate: `{bestOverall.DetectorId}` with onset hit rate {bestOverall.MedianOnsetHitRate:F3}, median onset error {FormatMetric(bestOverall.MedianOnsetLocalizationError)}, and median false positives {bestOverall.MedianFalsePositiveCount:F3}.");
            builder.AppendLine($"- Best baseline: `{bestBaseline.DetectorId}` with onset hit rate {bestBaseline.MedianOnsetHitRate:F3}.");
            builder.AppendLine($"- RMS-normalized mean compressed byte value: onset hit rate {normalized.MedianOnsetHitRate:F3}, onset error {FormatMetric(normalized.MedianOnsetLocalizationError)}, median false positives {normalized.MedianFalsePositiveCount:F3}, distinct-hit conditions vs best baseline {normalized.DistinctHitVsBestBaselineConditionCount}.");
            builder.AppendLine($"- Cautious read: {GetTaskRead(bestBaseline, normalized)}");
            builder.AppendLine();
        }

        var normalizedRows = taskSummaryRows.Where(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var baselineRows = taskSummaryRows.Where(row => BaselineDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase)).ToArray();
        var normalizedConditionWins = comparisonRows
            .GroupBy(row => (row.TaskFamilyId, row.Seed, row.SnrDb, row.WindowLength))
            .Count(group =>
            {
                var best = group.OrderByDescending(row => row.OnsetHitRate)
                    .ThenBy(row => row.MedianFalsePositiveCount)
                    .ThenBy(row => row.MedianOnsetLocalizationError ?? double.MaxValue)
                    .First();
                return string.Equals(best.DetectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase);
            });

        builder.AppendLine("## Overall Role Read");
        builder.AppendLine();
        builder.AppendLine($"- RMS-normalized mean was outright best on {normalizedConditionWins} of {comparisonRows.GroupBy(row => (row.TaskFamilyId, row.Seed, row.SnrDb, row.WindowLength)).Count()} evaluated seed-conditions.");
        builder.AppendLine($"- Across task-family summaries, its median onset hit rate ranged from {normalizedRows.Min(row => row.MedianOnsetHitRate):F3} to {normalizedRows.Max(row => row.MedianOnsetHitRate):F3}, versus baseline family leaders ranging from {baselineRows.GroupBy(row => row.TaskFamilyId).Min(group => group.Max(row => row.MedianOnsetHitRate)):F3} to {baselineRows.GroupBy(row => row.TaskFamilyId).Max(group => group.Max(row => row.MedianOnsetHitRate)):F3}.");
        builder.AppendLine($"- Distinct-hit evidence: RMS-normalized mean recorded {normalizedRows.Sum(row => row.DistinctHitVsBestBaselineConditionCount)} condition-level cases where it hit an onset while the best baseline missed under the same `(task, seed, SNR, windowLength)` condition.");
        builder.AppendLine($"- Cautious practical read: {GetOverallRoleRead(normalizedRows, taskSummaryRows)}");
        builder.AppendLine();
        builder.AppendLine("## Overall Conclusion");
        builder.AppendLine();
        builder.AppendLine($"- {GetOverallConclusion(normalizedRows, taskSummaryRows)}");
        builder.AppendLine();
        builder.AppendLine("## Caveats");
        builder.AppendLine();
        builder.AppendLine("- This remains a synthetic-only stream benchmark suite.");
        builder.AppendLine("- The engineered regime constructions are simple OFDM-like / correlated-process transitions, not LTE-fidelity modeling.");
        builder.AppendLine("- The current deterministic compression-backend caveat remains in force.");
        builder.AppendLine("- No SDR-facing or deployment-readiness claim is made here.");
        builder.AppendLine("- M7b is stream-level change-point usefulness mapping, not deployment proof or a large segmentation framework.");

        return builder.ToString();
    }

    private static bool HasDistinctHitVsBestBaseline(IGrouping<(int Seed, double SnrDb, int WindowLength), M7BBoundaryComparisonRow> condition, string detectorId)
    {
        var target = condition.Single(row => string.Equals(row.DetectorId, detectorId, StringComparison.OrdinalIgnoreCase));
        var bestBaselineHit = condition
            .Where(row => BaselineDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase))
            .Max(row => row.OnsetHitRate);

        return string.Equals(detectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase)
            && target.OnsetHitRate > 0d
            && bestBaselineHit <= 0d;
    }

    private static bool IsBestOrTiedBest(IGrouping<(int Seed, double SnrDb, int WindowLength), M7BBoundaryComparisonRow> condition, string detectorId)
    {
        var ordered = condition
            .OrderByDescending(row => row.OnsetHitRate)
            .ThenBy(row => row.MedianFalsePositiveCount)
            .ThenBy(row => row.MedianOnsetLocalizationError ?? double.MaxValue)
            .ToArray();
        var best = ordered.First();
        var candidate = ordered.Single(row => string.Equals(row.DetectorId, detectorId, StringComparison.OrdinalIgnoreCase));

        return Math.Abs(candidate.OnsetHitRate - best.OnsetHitRate) < 1e-9
            && Math.Abs(candidate.MedianFalsePositiveCount - best.MedianFalsePositiveCount) < 1e-9
            && Math.Abs((candidate.MedianOnsetLocalizationError ?? double.MaxValue) - (best.MedianOnsetLocalizationError ?? double.MaxValue)) < 1e-9;
    }

    private static string BuildComparisonNote(
        string detectorId,
        IReadOnlyList<M7BBoundaryComparisonRow> detectorRows,
        IReadOnlyList<IGrouping<(int Seed, double SnrDb, int WindowLength), M7BBoundaryComparisonRow>> groupedConditions)
    {
        if (detectorRows.Count == 0)
        {
            return "no rows recorded";
        }

        var onsetMedian = Median(detectorRows.Select(row => row.OnsetHitRate));
        var falsePositiveMedian = Median(detectorRows.Select(row => row.MedianFalsePositiveCount));

        if (string.Equals(detectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase))
        {
            var distinctHitConditions = groupedConditions.Count(condition => HasDistinctHitVsBestBaseline(condition, detectorId));
            if (distinctHitConditions > 0)
            {
                return $"secondary overall, but it contributed distinct onset hits in {distinctHitConditions} condition(s) where the best baseline missed.";
            }

            return onsetMedian >= 0.5d
                ? "useful on part of the stream suite, but still not the dominant standalone boundary cue overall."
                : "mostly secondary to ED/CAV in this stream suite.";
        }

        return falsePositiveMedian <= 1d && onsetMedian >= 0.5d
            ? "stronger practical boundary cue in this compact suite."
            : "usable, but not uniformly dominant once localization and false positives are included.";
    }

    private static string GetTaskRead(M7BTaskSummaryRow bestBaseline, M7BTaskSummaryRow normalized)
    {
        if (normalized.MedianOnsetHitRate > bestBaseline.MedianOnsetHitRate + 0.10d)
        {
            return "RMS-normalized mean looked genuinely stronger than the best baseline on this family within the tested grid.";
        }

        if (normalized.MedianOnsetHitRate >= bestBaseline.MedianOnsetHitRate - 0.05d
            && normalized.DistinctHitVsBestBaselineConditionCount > 0)
        {
            return "RMS-normalized mean was competitive on hit rate and showed some distinct complementary boundary behavior, even though it was not a universal winner.";
        }

        if (normalized.MedianOnsetHitRate >= bestBaseline.MedianOnsetHitRate - 0.05d)
        {
            return "RMS-normalized mean was in the same rough band as the baselines here, but without a strong standalone advantage.";
        }

        return "ED/CAV remained clearly stronger here, and RMS-normalized mean looked secondary rather than replacement-grade.";
    }

    private static string GetOverallRoleRead(IReadOnlyList<M7BTaskSummaryRow> normalizedRows, IReadOnlyList<M7BTaskSummaryRow> allRows)
    {
        var distinctHitConditions = normalizedRows.Sum(row => row.DistinctHitVsBestBaselineConditionCount);
        var taskWins = normalizedRows.Count(row =>
        {
            var familyBest = allRows.Where(candidate => candidate.TaskFamilyId == row.TaskFamilyId)
                .OrderByDescending(candidate => candidate.MedianOnsetHitRate)
                .ThenBy(candidate => candidate.MedianFalsePositiveCount)
                .First();
            return string.Equals(familyBest.DetectorId, row.DetectorId, StringComparison.OrdinalIgnoreCase);
        });

        if (taskWins >= 1)
        {
            return "within this synthetic stream suite, the compression-derived proxy looked more natural as a change-point cue on a narrow subset of families than it had as a static classifier, while still remaining secondary overall.";
        }

        if (distinctHitConditions > 0)
        {
            return "the proxy remained secondary overall, but it showed some distinct boundary behavior that could matter as a lightweight segmentation helper rather than a replacement detector.";
        }

        return "the proxy did not overturn the ED/CAV ranking here; at best it looks like a limited helper signal on this compact synthetic stream suite.";
    }

    private static string GetOverallConclusion(IReadOnlyList<M7BTaskSummaryRow> normalizedRows, IReadOnlyList<M7BTaskSummaryRow> allRows)
    {
        var distinctHitConditions = normalizedRows.Sum(row => row.DistinctHitVsBestBaselineConditionCount);
        var bestFamilyIds = allRows
            .GroupBy(row => row.TaskFamilyId)
            .Select(group => group.OrderByDescending(row => row.MedianOnsetHitRate)
                .ThenBy(row => row.MedianFalsePositiveCount)
                .ThenBy(row => row.MedianOnsetLocalizationError ?? double.MaxValue)
                .First())
            .ToArray();

        if (bestFamilyIds.Any(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase)))
        {
            var winningFamilies = string.Join(", ", bestFamilyIds
                .Where(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase))
                .Select(row => row.TaskFamilyId)
                .Select(name => $"`{name}`"));
            return $"Within this synthetic stream suite, RMS-normalized mean compressed byte value was more useful as a change-point cue than it had been as a static classifier, especially on {winningFamilies}, but it still did not displace ED/CAV as the overall default choice.";
        }

        if (distinctHitConditions > 0)
        {
            return "Within this synthetic stream suite, RMS-normalized mean compressed byte value remained secondary overall, but it produced distinct boundary behavior in a small number of conditions and therefore looks more plausible as a segmentation helper than as a replacement classifier.";
        }

        return "The tested synthetic stream families did not show a meaningful transition-detection advantage for RMS-normalized mean compressed byte value over ED/CAV; any value here appears limited and complementary at best.";
    }

    private static bool HasOffset(IReadOnlyList<M7BStreamBoundaryMetrics> metrics)
        => metrics.Any(metric => metric.OffsetHit.HasValue);

    private static string FormatNullable(double? value)
        => value.HasValue ? value.Value.ToString("F6", CultureInfo.InvariantCulture) : string.Empty;

    private static string Escape(string value)
        => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private static string FormatMetric(double? value)
        => value.HasValue ? value.Value.ToString("F3", CultureInfo.InvariantCulture) : "n/a";

    private static double Round(double value)
        => Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        if (ordered.Length == 0)
        {
            return 0d;
        }

        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2d
            : ordered[middle];
    }

    private static double? NullableMedian(IEnumerable<double?> values)
    {
        var present = values.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        return present.Length == 0 ? null : Round(Median(present));
    }
}
