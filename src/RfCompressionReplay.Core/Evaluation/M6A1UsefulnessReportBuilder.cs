using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M6A1UsefulnessReportBuilder
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

    public static M6A1UsefulnessArtifacts Build(
        M6A1UsefulnessConfig config,
        IReadOnlyList<(int Seed, ExperimentResult Result)> seedResults)
    {
        var comparisonRows = seedResults
            .SelectMany(run => run.Result.Summary.Groups
                .Where(summary => summary.TaskName is not null && summary.ConditionSnrDb.HasValue && summary.WindowLength.HasValue && summary.Auc.HasValue)
                .Select(summary => new M6A1AucComparisonRow(
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

        var taskSummaryRows = config.Evaluation.Tasks
            .SelectMany(task => config.Evaluation.Detectors.Select(detector => BuildTaskSummaryRow(task.Name, detector.Name, comparisonRows)))
            .OrderBy(row => row.TaskFamilyId, StringComparer.Ordinal)
            .ThenByDescending(row => row.MedianAuc)
            .ThenByDescending(row => row.BestOrTiedBestConditionCount)
            .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
            .ToArray();

        return new M6A1UsefulnessArtifacts(
            comparisonRows,
            taskSummaryRows,
            BuildFindingsMarkdown(config, comparisonRows, taskSummaryRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M6A1AucComparisonRow> rows)
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

    public static void WriteTaskSummaryCsv(string path, IReadOnlyList<M6A1TaskSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskFamilyId,detectorId,medianAuc,maxAuc,bestOrTiedBestConditionCount,conditionCount,medianGapToBestBaselineAuc,maxGapToBestBaselineAuc,medianGapToLzmsaPaperAuc,medianGapToRmsNormalizedMeanAuc,comparisonNote");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskFamilyId,
                row.DetectorId,
                row.MedianAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.BestOrTiedBestConditionCount.ToString(CultureInfo.InvariantCulture),
                row.ConditionCount.ToString(CultureInfo.InvariantCulture),
                row.MedianGapToBestBaselineAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxGapToBestBaselineAuc.ToString("F6", CultureInfo.InvariantCulture),
                FormatNullable(row.MedianGapToLzmsaPaperAuc),
                FormatNullable(row.MedianGapToRmsNormalizedMeanAuc),
                Escape(row.ComparisonNote)));
        }
    }

    private static M6A1TaskSummaryRow BuildTaskSummaryRow(string taskName, string detectorId, IReadOnlyList<M6A1AucComparisonRow> comparisonRows)
    {
        var detectorRows = comparisonRows
            .Where(row => string.Equals(row.TaskFamilyId, taskName, StringComparison.Ordinal)
                && string.Equals(row.DetectorId, detectorId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var groupedConditions = comparisonRows
            .Where(row => string.Equals(row.TaskFamilyId, taskName, StringComparison.Ordinal))
            .GroupBy(row => (row.Seed, row.SnrDb, row.WindowLength))
            .ToArray();

        var aucValues = detectorRows.Select(row => row.Auc).OrderBy(value => value).ToArray();
        var baselineGaps = groupedConditions
            .Select(condition => GetConditionGapToBestBaseline(condition, detectorId))
            .OrderBy(value => value)
            .ToArray();
        var bestOrTieCount = groupedConditions.Count(condition => IsBestOrTiedBest(condition, detectorId));
        var paperGaps = GetPairwiseGaps(taskName, detectorId, DetectorCatalog.LzmsaPaperDetectorName, comparisonRows);
        var normalizedGaps = GetPairwiseGaps(taskName, detectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, comparisonRows);

        return new M6A1TaskSummaryRow(
            taskName,
            detectorId,
            aucValues.Length == 0 ? 0d : Round(Median(aucValues)),
            aucValues.Length == 0 ? 0d : Round(aucValues.Max()),
            bestOrTieCount,
            groupedConditions.Length,
            baselineGaps.Length == 0 ? 0d : Round(Median(baselineGaps)),
            baselineGaps.Length == 0 ? 0d : Round(baselineGaps.Max()),
            detectorId.Equals(DetectorCatalog.LzmsaPaperDetectorName, StringComparison.OrdinalIgnoreCase) ? null : NullableMedian(paperGaps),
            detectorId.Equals(DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase) ? null : NullableMedian(normalizedGaps),
            BuildComparisonNote(detectorId, groupedConditions, aucValues, baselineGaps, paperGaps, normalizedGaps));
    }

    private static string BuildFindingsMarkdown(
        M6A1UsefulnessConfig config,
        IReadOnlyList<M6A1AucComparisonRow> comparisonRows,
        IReadOnlyList<M6A1TaskSummaryRow> taskSummaryRows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# M6a1 Usefulness-Mapping Findings");
        builder.AppendLine();
        builder.AppendLine("## Scope");
        builder.AppendLine();
        builder.AppendLine($"- Task families: {string.Join(", ", config.Evaluation.Tasks.Select(task => $"`{task.Name}`"))}");
        builder.AppendLine($"- Detector panel: {string.Join(", ", config.Evaluation.Detectors.Select(detector => $"`{detector.Name}`"))}");
        builder.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        builder.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        builder.AppendLine($"- Seeds: {string.Join(", ", config.SeedPanel)}");
        builder.AppendLine($"- Trial count per class per seed/condition: {config.Evaluation.TrialCountPerCondition}");
        builder.AppendLine($"- Retention mode used: `{config.ArtifactRetentionMode}` (with only manifest + M6a1 summary artifacts retained at the top level for this milestone)");
        builder.AppendLine($"- Config provenance: `{config.ExperimentId}` / `{config.ManifestMetadata.VersionTag}`");
        builder.AppendLine();
        builder.AppendLine("## Task-Family Read");
        builder.AppendLine();

        foreach (var task in config.Evaluation.Tasks)
        {
            var rows = taskSummaryRows.Where(row => string.Equals(row.TaskFamilyId, task.Name, StringComparison.Ordinal)).ToArray();
            var bestOverall = rows
                .OrderByDescending(row => row.MedianAuc)
                .ThenByDescending(row => row.BestOrTiedBestConditionCount)
                .ThenByDescending(row => row.MaxAuc)
                .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
                .First();
            var bestBaseline = rows
                .Where(row => BaselineDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(row => row.MedianAuc)
                .ThenByDescending(row => row.BestOrTiedBestConditionCount)
                .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
                .First();
            var bestCompression = rows
                .Where(row => CompressionDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(row => row.MedianAuc)
                .ThenByDescending(row => row.BestOrTiedBestConditionCount)
                .ThenBy(row => row.DetectorId, StringComparer.Ordinal)
                .First();
            var normalized = rows.Single(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase));
            var paper = rows.Single(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaPaperDetectorName, StringComparison.OrdinalIgnoreCase));

            builder.AppendLine($"### `{task.Name}`");
            builder.AppendLine();
            builder.AppendLine($"- Best overall by median AUC: `{bestOverall.DetectorId}` at {bestOverall.MedianAuc:F3} median AUC with {bestOverall.BestOrTiedBestConditionCount}/{bestOverall.ConditionCount} best-or-tied-best condition wins.");
            builder.AppendLine($"- Best baseline: `{bestBaseline.DetectorId}` at {bestBaseline.MedianAuc:F3} median AUC.");
            builder.AppendLine($"- Best compression-derived detector: `{bestCompression.DetectorId}` at {bestCompression.MedianAuc:F3} median AUC.");
            builder.AppendLine($"- RMS-normalized mean vs `lzmsa-paper`: normalized mean median AUC {normalized.MedianAuc:F3} vs paper {paper.MedianAuc:F3}; median pairwise AUC gap (normalized minus paper) {(normalized.MedianGapToLzmsaPaperAuc ?? 0d):F3}.");
            builder.AppendLine($"- Cautious read: {GetTaskRead(bestBaseline, bestCompression, normalized, paper)}");
            builder.AppendLine();
        }

        var normalizedRows = taskSummaryRows.Where(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var paperRows = taskSummaryRows.Where(row => string.Equals(row.DetectorId, DetectorCatalog.LzmsaPaperDetectorName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var baselineRows = taskSummaryRows.Where(row => BaselineDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase)).ToArray();

        builder.AppendLine("## Practical Candidate Read");
        builder.AppendLine();
        builder.AppendLine($"- RMS-normalized mean compressed byte value reached the highest compression-family median AUC on {normalizedRows.Count(row => row.MedianAuc >= paperRows.Single(paper => paper.TaskFamilyId == row.TaskFamilyId).MedianAuc)} of {normalizedRows.Length} task families in this suite.");
        builder.AppendLine($"- Against the best baseline within each task family, its median AUC gap ranged from {normalizedRows.Min(row => row.MedianGapToBestBaselineAuc):F3} to {normalizedRows.Max(row => row.MedianGapToBestBaselineAuc):F3}.");
        builder.AppendLine($"- Cautious practical read: {GetPracticalCandidateRead(normalizedRows, baselineRows, paperRows)}");
        builder.AppendLine();
        builder.AppendLine("## Overall Conclusion");
        builder.AppendLine();
        builder.AppendLine($"- {GetOverallConclusion(taskSummaryRows)}");
        builder.AppendLine();
        builder.AppendLine("## Caveats");
        builder.AppendLine();
        builder.AppendLine("- This remains a synthetic-only benchmark suite.");
        builder.AppendLine("- The structured tasks use simple OFDM-like / correlated-process constructions, not LTE-faithful channels or captures.");
        builder.AppendLine("- The current deterministic compression-backend caveat remains in force.");
        builder.AppendLine("- No SDR-facing or deployment-readiness claim is made here.");
        builder.AppendLine("- M6a1 is usefulness mapping inside the current harness, not final validation.");

        return builder.ToString();
    }

    private static string GetTaskRead(
        M6A1TaskSummaryRow bestBaseline,
        M6A1TaskSummaryRow bestCompression,
        M6A1TaskSummaryRow normalized,
        M6A1TaskSummaryRow paper)
    {
        if (bestCompression.MedianAuc > bestBaseline.MedianAuc + 0.05d)
        {
            return "compression-derived features materially outperformed the strongest baseline on this task family inside the tested grid.";
        }

        if (bestCompression.MedianAuc > bestBaseline.MedianAuc + 0.01d)
        {
            return "compression-derived features were competitive and sometimes stronger than the strongest baseline, but the margin stayed modest.";
        }

        if (normalized.MedianAuc >= paper.MedianAuc - 0.01d)
        {
            return "ED/CAV remained at least as strong on median AUC, while the normalized-mean proxy stayed close to lzmsa-paper as the simpler compression summary.";
        }

        return "ED/CAV remained stronger here, and lzmsa-paper kept some measurable edge over the normalized-mean proxy.";
    }

    private static string GetPracticalCandidateRead(
        IReadOnlyList<M6A1TaskSummaryRow> normalizedRows,
        IReadOnlyList<M6A1TaskSummaryRow> baselineRows,
        IReadOnlyList<M6A1TaskSummaryRow> paperRows)
    {
        var strongestNormalizedGap = normalizedRows.Max(row => row.MedianGapToBestBaselineAuc);
        var closestToPaperCount = normalizedRows.Count(row => Math.Abs(row.MedianGapToLzmsaPaperAuc ?? 0d) <= 0.02d);
        var baselineTaskWins = baselineRows
            .GroupBy(row => row.TaskFamilyId)
            .Count(group => group.Max(row => row.MedianAuc) >= normalizedRows.Single(normalized => normalized.TaskFamilyId == group.Key).MedianAuc);

        if (strongestNormalizedGap > 0.05d)
        {
            return "it looks useful as a standalone lightweight detector on at least one task family and also as a complement to ED/CAV elsewhere.";
        }

        if (closestToPaperCount == normalizedRows.Count && baselineTaskWins >= 1)
        {
            return "it looks most useful as a complement to the naive baselines rather than a universal winner, while staying close to lzmsa-paper across this suite.";
        }

        if (paperRows.Any(row => row.MedianAuc > normalizedRows.Single(normalized => normalized.TaskFamilyId == row.TaskFamilyId).MedianAuc + 0.03d))
        {
            return "it showed some value, but lzmsa-paper still bought a noticeable edge on part of the tested suite.";
        }

        return "it did not clearly beat the naive baselines on median AUC, but it remained a simple compression-derived complement that was usually close to lzmsa-paper.";
    }

    private static string GetOverallConclusion(IReadOnlyList<M6A1TaskSummaryRow> rows)
    {
        var equalEnergyNormalized = rows.Single(row => row.TaskFamilyId == BenchmarkTaskCatalog.EqualEnergyStructuredVsUnstructured
            && row.DetectorId == DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName);
        var equalEnergyBaseline = rows
            .Where(row => row.TaskFamilyId == BenchmarkTaskCatalog.EqualEnergyStructuredVsUnstructured
                && BaselineDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(row => row.MedianAuc)
            .First();

        if (equalEnergyNormalized.MedianAuc > equalEnergyBaseline.MedianAuc + 0.05d)
        {
            return "Within this synthetic task suite, compression-derived byte-structure features were most useful on equal-energy structured-vs-unstructured discrimination, where RMS-normalized mean compressed byte value exceeded the strongest baseline on median AUC.";
        }

        if (rows.Where(row => CompressionDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase)).Max(row => row.MedianGapToBestBaselineAuc) > 0.01d)
        {
            return "Within this synthetic task suite, compression-derived byte-structure features added selective value on structure-sensitive tasks, while ED/CAV still dominated the easier cases.";
        }

        return "Within this synthetic task suite, ED/CAV covered most of the detectable separation, while the compression-derived detectors mainly served as a lightweight secondary view rather than a dominant replacement.";
    }

    private static string BuildComparisonNote(
        string detectorId,
        IReadOnlyCollection<IGrouping<(int Seed, double SnrDb, int WindowLength), M6A1AucComparisonRow>> groupedConditions,
        IReadOnlyList<double> aucValues,
        IReadOnlyList<double> baselineGaps,
        IReadOnlyList<double> paperGaps,
        IReadOnlyList<double> normalizedGaps)
    {
        if (aucValues.Count == 0)
        {
            return "no rows";
        }

        if (string.Equals(detectorId, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName, StringComparison.OrdinalIgnoreCase))
        {
            return $"normalized mean median AUC {Median(aucValues):F3}; median gap vs best baseline {Median(baselineGaps):F3}; median gap vs paper {Median(paperGaps):F3}.";
        }

        if (string.Equals(detectorId, DetectorCatalog.LzmsaPaperDetectorName, StringComparison.OrdinalIgnoreCase))
        {
            return $"paper median AUC {Median(aucValues):F3}; median gap vs best baseline {Median(baselineGaps):F3}; median gap vs normalized mean {Median(normalizedGaps):F3}.";
        }

        return $"{detectorId} median AUC {Median(aucValues):F3} across {groupedConditions.Count} seed-condition combinations.";
    }

    private static bool IsBestOrTiedBest(IGrouping<(int Seed, double SnrDb, int WindowLength), M6A1AucComparisonRow> condition, string detectorId)
    {
        var best = condition.Max(row => row.Auc);
        var detectorRow = condition.Single(row => string.Equals(row.DetectorId, detectorId, StringComparison.OrdinalIgnoreCase));
        return NearlyEqual(detectorRow.Auc, best);
    }

    private static double GetConditionGapToBestBaseline(IGrouping<(int Seed, double SnrDb, int WindowLength), M6A1AucComparisonRow> condition, string detectorId)
    {
        var detectorAuc = condition.Single(row => string.Equals(row.DetectorId, detectorId, StringComparison.OrdinalIgnoreCase)).Auc;
        var bestBaselineAuc = condition
            .Where(row => BaselineDetectors.Contains(row.DetectorId, StringComparer.OrdinalIgnoreCase))
            .Max(row => row.Auc);
        return Round(detectorAuc - bestBaselineAuc);
    }

    private static double[] GetPairwiseGaps(string taskName, string detectorId, string referenceDetectorId, IReadOnlyList<M6A1AucComparisonRow> rows)
    {
        if (string.Equals(detectorId, referenceDetectorId, StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return rows
            .Where(row => string.Equals(row.TaskFamilyId, taskName, StringComparison.Ordinal)
                && string.Equals(row.DetectorId, detectorId, StringComparison.OrdinalIgnoreCase))
            .Join(
                rows.Where(row => string.Equals(row.TaskFamilyId, taskName, StringComparison.Ordinal)
                    && string.Equals(row.DetectorId, referenceDetectorId, StringComparison.OrdinalIgnoreCase)),
                left => (left.Seed, left.SnrDb, left.WindowLength),
                right => (right.Seed, right.SnrDb, right.WindowLength),
                (left, right) => Round(left.Auc - right.Auc))
            .OrderBy(value => value)
            .ToArray();
    }

    private static double NullableMedian(IReadOnlyList<double> values)
    {
        return values.Count == 0 ? 0d : Round(Median(values));
    }

    private static string FormatNullable(double? value)
    {
        return value.HasValue ? value.Value.ToString("F6", CultureInfo.InvariantCulture) : string.Empty;
    }

    private static string Escape(string text)
    {
        return $"\"{text.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static double Median(IReadOnlyList<double> sortedValues)
    {
        if (sortedValues.Count == 0)
        {
            return 0d;
        }

        var middle = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 0
            ? (sortedValues[middle - 1] + sortedValues[middle]) / 2d
            : sortedValues[middle];
    }

    private static bool NearlyEqual(double left, double right)
    {
        return Math.Abs(left - right) <= 1e-9;
    }

    private static double Round(double value)
    {
        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
