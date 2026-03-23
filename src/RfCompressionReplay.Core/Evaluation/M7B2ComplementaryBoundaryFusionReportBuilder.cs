using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M7B2ComplementaryBoundaryFusionReportBuilder
{
    private static readonly string[] StandaloneSignalIds =
    [
        DetectorCatalog.EnergyDetectorName,
        DetectorCatalog.CovarianceAbsoluteValueDetectorName,
        DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
    ];

    public static M7B2ComplementaryBoundaryFusionArtifacts Build(
        M7B2ComplementaryBoundaryFusionConfig config,
        IReadOnlyList<(string TaskName, int Seed, double SnrDb, int WindowLength, string SignalId, IReadOnlyList<M7BStreamBoundaryMetrics> Metrics)> conditionRows)
    {
        var comparisonRows = conditionRows
            .Select(row => new M7B2BoundaryComparisonRow(
                row.TaskName,
                row.Seed,
                row.SnrDb,
                row.WindowLength,
                row.SignalId,
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
            .ThenBy(row => row.SignalId, StringComparer.Ordinal)
            .ToArray();

        var fusedSignalId = config.Evaluation.Fusions.Single().SignalId;
        var summaryRows = config.Benchmark.Tasks
            .Select(task => BuildFusionSummaryRow(task.Name, fusedSignalId, comparisonRows))
            .OrderBy(row => row.TaskFamilyId, StringComparer.Ordinal)
            .ToArray();

        return new M7B2ComplementaryBoundaryFusionArtifacts(
            comparisonRows,
            summaryRows,
            BuildFindingsMarkdown(config, comparisonRows, summaryRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M7B2BoundaryComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskFamilyId,seed,snrDb,windowLength,signalId,onsetHitRate,offsetHitRate,medianOnsetLocalizationError,medianOffsetLocalizationError,medianFalsePositiveCount,medianOnsetDetectionDelay,medianOffsetDetectionDelay,streamCount");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskFamilyId,
                row.Seed.ToString(CultureInfo.InvariantCulture),
                row.SnrDb.ToString("0.###", CultureInfo.InvariantCulture),
                row.WindowLength.ToString(CultureInfo.InvariantCulture),
                row.SignalId,
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

    public static void WriteFusionSummaryCsv(string path, IReadOnlyList<M7B2FusionSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskFamilyId,bestSingleSignalId,edMedianOnsetHitRate,edMedianOffsetHitRate,edMedianOnsetLocalizationError,edMedianOffsetLocalizationError,edMedianFalsePositiveCount,cavMedianOnsetHitRate,cavMedianOffsetHitRate,cavMedianOnsetLocalizationError,cavMedianOffsetLocalizationError,cavMedianFalsePositiveCount,normalizedMeanMedianOnsetHitRate,normalizedMeanMedianOffsetHitRate,normalizedMeanMedianOnsetLocalizationError,normalizedMeanMedianOffsetLocalizationError,normalizedMeanMedianFalsePositiveCount,fusedSignalId,fusedMedianOnsetHitRate,fusedMedianOffsetHitRate,fusedMedianOnsetLocalizationError,fusedMedianOffsetLocalizationError,fusedMedianFalsePositiveCount,fusedMinusBestSingleMedianOnsetHitRate,fusedMinusBestSingleMedianOffsetHitRate,fusedMinusBestSingleMedianOnsetLocalizationError,fusedMinusBestSingleMedianOffsetLocalizationError,fusedMinusBestSingleMedianFalsePositiveCount,fusionBestOrTiedBestConditionCount,fusionRecoveredAgainstBestBaselineConditionCount,fusionRecoveredAgainstAllSinglesConditionCount,familyRead");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskFamilyId,
                row.BestSingleSignalId,
                row.EdMedianOnsetHitRate.ToString("F6", CultureInfo.InvariantCulture),
                FormatNullable(row.EdMedianOffsetHitRate),
                FormatNullable(row.EdMedianOnsetLocalizationError),
                FormatNullable(row.EdMedianOffsetLocalizationError),
                row.EdMedianFalsePositiveCount.ToString("F6", CultureInfo.InvariantCulture),
                row.CavMedianOnsetHitRate.ToString("F6", CultureInfo.InvariantCulture),
                FormatNullable(row.CavMedianOffsetHitRate),
                FormatNullable(row.CavMedianOnsetLocalizationError),
                FormatNullable(row.CavMedianOffsetLocalizationError),
                row.CavMedianFalsePositiveCount.ToString("F6", CultureInfo.InvariantCulture),
                row.NormalizedMeanMedianOnsetHitRate.ToString("F6", CultureInfo.InvariantCulture),
                FormatNullable(row.NormalizedMeanMedianOffsetHitRate),
                FormatNullable(row.NormalizedMeanMedianOnsetLocalizationError),
                FormatNullable(row.NormalizedMeanMedianOffsetLocalizationError),
                row.NormalizedMeanMedianFalsePositiveCount.ToString("F6", CultureInfo.InvariantCulture),
                row.FusedSignalId,
                row.FusedMedianOnsetHitRate.ToString("F6", CultureInfo.InvariantCulture),
                FormatNullable(row.FusedMedianOffsetHitRate),
                FormatNullable(row.FusedMedianOnsetLocalizationError),
                FormatNullable(row.FusedMedianOffsetLocalizationError),
                row.FusedMedianFalsePositiveCount.ToString("F6", CultureInfo.InvariantCulture),
                row.FusedMinusBestSingleMedianOnsetHitRate.ToString("F6", CultureInfo.InvariantCulture),
                FormatNullable(row.FusedMinusBestSingleMedianOffsetHitRate),
                FormatNullable(row.FusedMinusBestSingleMedianOnsetLocalizationError),
                FormatNullable(row.FusedMinusBestSingleMedianOffsetLocalizationError),
                row.FusedMinusBestSingleMedianFalsePositiveCount.ToString("F6", CultureInfo.InvariantCulture),
                row.FusionBestOrTiedBestConditionCount.ToString(CultureInfo.InvariantCulture),
                row.FusionRecoveredAgainstBestBaselineConditionCount.ToString(CultureInfo.InvariantCulture),
                row.FusionRecoveredAgainstAllSinglesConditionCount.ToString(CultureInfo.InvariantCulture),
                Escape(row.FamilyRead)));
        }
    }

    private static M7B2FusionSummaryRow BuildFusionSummaryRow(
        string taskFamilyId,
        string fusedSignalId,
        IReadOnlyList<M7B2BoundaryComparisonRow> comparisonRows)
    {
        var taskRows = comparisonRows.Where(row => string.Equals(row.TaskFamilyId, taskFamilyId, StringComparison.Ordinal)).ToArray();
        var ed = BuildSignalAggregate(taskRows, DetectorCatalog.EnergyDetectorName);
        var cav = BuildSignalAggregate(taskRows, DetectorCatalog.CovarianceAbsoluteValueDetectorName);
        var normalized = BuildSignalAggregate(taskRows, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName);
        var fused = BuildSignalAggregate(taskRows, fusedSignalId);
        var singles = new[] { ed, cav, normalized };
        var bestSingle = singles
            .OrderByDescending(row => row.MedianOnsetHitRate)
            .ThenBy(row => row.MedianFalsePositiveCount)
            .ThenBy(row => row.MedianOnsetLocalizationError ?? double.MaxValue)
            .ThenBy(row => row.SignalId, StringComparer.Ordinal)
            .First();

        var groupedConditions = taskRows
            .GroupBy(row => (row.Seed, row.SnrDb, row.WindowLength))
            .ToArray();

        return new M7B2FusionSummaryRow(
            taskFamilyId,
            bestSingle.SignalId,
            ed.MedianOnsetHitRate,
            ed.MedianOffsetHitRate,
            ed.MedianOnsetLocalizationError,
            ed.MedianOffsetLocalizationError,
            ed.MedianFalsePositiveCount,
            cav.MedianOnsetHitRate,
            cav.MedianOffsetHitRate,
            cav.MedianOnsetLocalizationError,
            cav.MedianOffsetLocalizationError,
            cav.MedianFalsePositiveCount,
            normalized.MedianOnsetHitRate,
            normalized.MedianOffsetHitRate,
            normalized.MedianOnsetLocalizationError,
            normalized.MedianOffsetLocalizationError,
            normalized.MedianFalsePositiveCount,
            fused.SignalId,
            fused.MedianOnsetHitRate,
            fused.MedianOffsetHitRate,
            fused.MedianOnsetLocalizationError,
            fused.MedianOffsetLocalizationError,
            fused.MedianFalsePositiveCount,
            Round(fused.MedianOnsetHitRate - bestSingle.MedianOnsetHitRate),
            NullableSubtract(fused.MedianOffsetHitRate, bestSingle.MedianOffsetHitRate),
            NullableSubtract(fused.MedianOnsetLocalizationError, bestSingle.MedianOnsetLocalizationError),
            NullableSubtract(fused.MedianOffsetLocalizationError, bestSingle.MedianOffsetLocalizationError),
            Round(fused.MedianFalsePositiveCount - bestSingle.MedianFalsePositiveCount),
            groupedConditions.Count(condition => IsBestOrTiedBest(condition, fused.SignalId)),
            groupedConditions.Count(condition => FusionRecoveredAgainstBestBaseline(condition, fused.SignalId)),
            groupedConditions.Count(condition => FusionRecoveredAgainstAllSingles(condition, fused.SignalId)),
            BuildFamilyRead(bestSingle, normalized, fused, groupedConditions));
    }

    private static SignalAggregate BuildSignalAggregate(IReadOnlyList<M7B2BoundaryComparisonRow> taskRows, string signalId)
    {
        var rows = taskRows.Where(row => string.Equals(row.SignalId, signalId, StringComparison.OrdinalIgnoreCase)).ToArray();
        return new SignalAggregate(
            signalId,
            rows.Length == 0 ? 0d : Round(Median(rows.Select(row => row.OnsetHitRate))),
            rows.All(row => row.OffsetHitRate is null) ? null : NullableMedian(rows.Select(row => row.OffsetHitRate)),
            NullableMedian(rows.Select(row => row.MedianOnsetLocalizationError)),
            NullableMedian(rows.Select(row => row.MedianOffsetLocalizationError)),
            rows.Length == 0 ? 0d : Round(Median(rows.Select(row => row.MedianFalsePositiveCount))));
    }

    private static string BuildFindingsMarkdown(
        M7B2ComplementaryBoundaryFusionConfig config,
        IReadOnlyList<M7B2BoundaryComparisonRow> comparisonRows,
        IReadOnlyList<M7B2FusionSummaryRow> summaryRows)
    {
        var builder = new StringBuilder();
        var fusion = config.Evaluation.Fusions.Single();
        var totalConditions = comparisonRows.GroupBy(row => (row.TaskFamilyId, row.Seed, row.SnrDb, row.WindowLength)).Count();
        var fusionConditionWins = comparisonRows
            .GroupBy(row => (row.TaskFamilyId, row.Seed, row.SnrDb, row.WindowLength))
            .Count(group =>
            {
                var best = group
                    .OrderByDescending(row => row.OnsetHitRate)
                    .ThenBy(row => row.MedianFalsePositiveCount)
                    .ThenBy(row => row.MedianOnsetLocalizationError ?? double.MaxValue)
                    .ThenBy(row => row.SignalId, StringComparer.Ordinal)
                    .First();
                var candidate = group.Single(row => string.Equals(row.SignalId, fusion.SignalId, StringComparison.OrdinalIgnoreCase));
                return Math.Abs(candidate.OnsetHitRate - best.OnsetHitRate) < 1e-9
                    && Math.Abs(candidate.MedianFalsePositiveCount - best.MedianFalsePositiveCount) < 1e-9
                    && Math.Abs((candidate.MedianOnsetLocalizationError ?? double.MaxValue) - (best.MedianOnsetLocalizationError ?? double.MaxValue)) < 1e-9;
            });
        var recoveredVsAllSingles = summaryRows.Sum(row => row.FusionRecoveredAgainstAllSinglesConditionCount);
        var recoveredVsBestBaseline = summaryRows.Sum(row => row.FusionRecoveredAgainstBestBaselineConditionCount);

        builder.AppendLine("# M7b2 Complementary Boundary Fusion Findings");
        builder.AppendLine();
        builder.AppendLine("## Scope");
        builder.AppendLine();
        builder.AppendLine($"- Stream task families: {string.Join(", ", config.Benchmark.Tasks.Select(task => $"`{task.Name}`"))}");
        builder.AppendLine($"- Standalone signals: {string.Join(", ", StandaloneSignalIds.Select(signalId => $"`{signalId}`"))}");
        builder.AppendLine($"- Fusion signal: `{fusion.SignalId}`");
        builder.AppendLine($"- Fusion rule: `{fusion.Rule}` = compute each detector's absolute adjacent-window change trace, normalize each trace to [0, 1] within stream via min-max scaling, then average the normalized traces pointwise before the same peak-picking boundary rule.");
        builder.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        builder.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        builder.AppendLine($"- Seeds: {string.Join(", ", config.SeedPanel)}");
        builder.AppendLine($"- Stream count per seed/condition: {config.Evaluation.StreamCountPerCondition}");
        builder.AppendLine($"- Retention mode used: `{config.ArtifactRetentionMode}` (repository milestone mode used as the nearest compact summary-first retention path: manifest + boundary comparison CSV + fusion summary CSV + findings markdown)");
        builder.AppendLine($"- Config provenance: `{config.ExperimentId}` / `{config.ManifestMetadata.VersionTag}`");
        builder.AppendLine();
        builder.AppendLine("## Task-Family Read");
        builder.AppendLine();

        foreach (var row in summaryRows)
        {
            builder.AppendLine($"### `{row.TaskFamilyId}`");
            builder.AppendLine();
            builder.AppendLine($"- Best single signal: `{row.BestSingleSignalId}` with median onset hit rate {GetBestSingleHitRate(row):F3}, median onset error {FormatMetric(GetBestSingleOnsetError(row))}, and median false positives {GetBestSingleFalsePositives(row):F3}.");
            builder.AppendLine($"- Fused signal `{row.FusedSignalId}`: median onset hit rate {row.FusedMedianOnsetHitRate:F3}, median onset error {FormatMetric(row.FusedMedianOnsetLocalizationError)}, median false positives {row.FusedMedianFalsePositiveCount:F3}, fused-minus-best-single onset hit delta {row.FusedMinusBestSingleMedianOnsetHitRate:F3}, false-positive delta {row.FusedMinusBestSingleMedianFalsePositiveCount:F3}.");
            builder.AppendLine($"- Fusion best/tied-best conditions: {row.FusionBestOrTiedBestConditionCount}; recovery vs best baseline: {row.FusionRecoveredAgainstBestBaselineConditionCount}; recovery vs all singles: {row.FusionRecoveredAgainstAllSinglesConditionCount}.");
            builder.AppendLine($"- Cautious read: {row.FamilyRead}");
            builder.AppendLine();
        }

        builder.AppendLine("## Overall Role Read");
        builder.AppendLine();
        builder.AppendLine($"- Fusion was best or tied-best on {fusionConditionWins} of {totalConditions} evaluated `(task, seed, SNR, windowLength)` conditions.");
        builder.AppendLine($"- Fusion recovered onset-hit conditions missed by the best baseline in {recoveredVsBestBaseline} family-condition summaries, and by all three standalone signals in {recoveredVsAllSingles} family-condition summaries.");
        builder.AppendLine($"- Cautious practical read: {BuildOverallRoleRead(summaryRows)}");
        builder.AppendLine();
        builder.AppendLine("## Overall Conclusion");
        builder.AppendLine();
        builder.AppendLine($"- {BuildOverallConclusion(summaryRows)}");
        builder.AppendLine();
        builder.AppendLine("## Caveats");
        builder.AppendLine();
        builder.AppendLine("- This remains a synthetic-only stream benchmark suite.");
        builder.AppendLine("- The engineered regime constructions are simple OFDM-like / correlated-process transitions, not LTE-fidelity modeling.");
        builder.AppendLine("- The current deterministic compression-backend caveat remains in force.");
        builder.AppendLine("- No SDR-facing or deployment-readiness claim is made here.");
        builder.AppendLine("- M7b2 is compact stream-level usefulness mapping for complementary boundary fusion, not deployment proof.");

        return builder.ToString();
    }

    private static bool IsBestOrTiedBest(IGrouping<(int Seed, double SnrDb, int WindowLength), M7B2BoundaryComparisonRow> condition, string signalId)
    {
        var ordered = condition
            .OrderByDescending(row => row.OnsetHitRate)
            .ThenBy(row => row.MedianFalsePositiveCount)
            .ThenBy(row => row.MedianOnsetLocalizationError ?? double.MaxValue)
            .ThenBy(row => row.SignalId, StringComparer.Ordinal)
            .ToArray();
        var best = ordered.First();
        var candidate = ordered.Single(row => string.Equals(row.SignalId, signalId, StringComparison.OrdinalIgnoreCase));

        return Math.Abs(candidate.OnsetHitRate - best.OnsetHitRate) < 1e-9
            && Math.Abs(candidate.MedianFalsePositiveCount - best.MedianFalsePositiveCount) < 1e-9
            && Math.Abs((candidate.MedianOnsetLocalizationError ?? double.MaxValue) - (best.MedianOnsetLocalizationError ?? double.MaxValue)) < 1e-9;
    }

    private static bool FusionRecoveredAgainstBestBaseline(IGrouping<(int Seed, double SnrDb, int WindowLength), M7B2BoundaryComparisonRow> condition, string fusedSignalId)
    {
        var fusion = condition.Single(row => string.Equals(row.SignalId, fusedSignalId, StringComparison.OrdinalIgnoreCase));
        var bestBaselineHit = condition
            .Where(row => string.Equals(row.SignalId, DetectorCatalog.EnergyDetectorName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.SignalId, DetectorCatalog.CovarianceAbsoluteValueDetectorName, StringComparison.OrdinalIgnoreCase))
            .Max(row => row.OnsetHitRate);

        return fusion.OnsetHitRate > 0d && bestBaselineHit <= 0d;
    }

    private static bool FusionRecoveredAgainstAllSingles(IGrouping<(int Seed, double SnrDb, int WindowLength), M7B2BoundaryComparisonRow> condition, string fusedSignalId)
    {
        var fusion = condition.Single(row => string.Equals(row.SignalId, fusedSignalId, StringComparison.OrdinalIgnoreCase));
        var bestSingleHit = condition
            .Where(row => StandaloneSignalIds.Contains(row.SignalId, StringComparer.OrdinalIgnoreCase))
            .Max(row => row.OnsetHitRate);

        return fusion.OnsetHitRate > 0d && bestSingleHit <= 0d;
    }

    private static string BuildFamilyRead(
        SignalAggregate bestSingle,
        SignalAggregate normalizedMean,
        SignalAggregate fused,
        IReadOnlyList<IGrouping<(int Seed, double SnrDb, int WindowLength), M7B2BoundaryComparisonRow>> groupedConditions)
    {
        var fusionWins = groupedConditions.Count(condition => IsBestOrTiedBest(condition, fused.SignalId));
        var recoveredAgainstAllSingles = groupedConditions.Count(condition => FusionRecoveredAgainstAllSingles(condition, fused.SignalId));
        var recoveredAgainstBestBaseline = groupedConditions.Count(condition => FusionRecoveredAgainstBestBaseline(condition, fused.SignalId));

        if (fused.MedianOnsetHitRate > bestSingle.MedianOnsetHitRate + 0.10d
            && fused.MedianFalsePositiveCount <= bestSingle.MedianFalsePositiveCount + 0.25d)
        {
            return $"Fusion improved over the best standalone signal on this family, with the gain looking material rather than cosmetic; the RMS-normalized mean cue appears operationally helpful here as part of the fused boundary score ({fusionWins} best/tied-best conditions).";
        }

        if (recoveredAgainstAllSingles > 0 || recoveredAgainstBestBaseline > 0)
        {
            return $"Fusion showed complementary value on this family by recovering some onset-hit cases that the singles missed, but the overall lift remained modest; RMS-normalized mean looks helpful as a secondary boundary cue rather than a dominant standalone detector. (normalized-mean median onset hit rate {normalizedMean.MedianOnsetHitRate:F3})";
        }

        if (fused.MedianOnsetHitRate >= bestSingle.MedianOnsetHitRate - 0.05d)
        {
            return "Fusion was roughly competitive with the best single signal on this family, but the gain looked marginal rather than clearly practical.";
        }

        return "Fusion did not improve this family in a meaningful way; the compression-derived cue remains more scientifically interesting than operationally decisive here.";
    }

    private static string BuildOverallRoleRead(IReadOnlyList<M7B2FusionSummaryRow> rows)
    {
        var improvedFamilies = rows.Count(row => row.FusedMinusBestSingleMedianOnsetHitRate > 0.05d && row.FusedMinusBestSingleMedianFalsePositiveCount <= 0.25d);
        var recoveryFamilies = rows.Count(row => row.FusionRecoveredAgainstBestBaselineConditionCount > 0 || row.FusionRecoveredAgainstAllSinglesConditionCount > 0);

        if (improvedFamilies >= 1)
        {
            return "within this synthetic stream suite, the compression-derived cue looks operationally useful in simple fusion on at least part of the harder regime-transition panel, though not as a universal replacement for the best standalone baseline.";
        }

        if (recoveryFamilies >= 1)
        {
            return "the compression-derived cue looks only weakly complementary in simple fusion: it sometimes helps recover missed boundaries, but the aggregate lift stays modest.";
        }

        return "the compression-derived cue still looks more scientifically interesting than practically helpful in this simple fusion setting.";
    }

    private static string BuildOverallConclusion(IReadOnlyList<M7B2FusionSummaryRow> rows)
    {
        var improvedFamilies = rows
            .Where(row => row.FusedMinusBestSingleMedianOnsetHitRate > 0.05d && row.FusedMinusBestSingleMedianFalsePositiveCount <= 0.25d)
            .Select(row => row.TaskFamilyId)
            .ToArray();
        var recoveryFamilies = rows
            .Where(row => row.FusionRecoveredAgainstBestBaselineConditionCount > 0 || row.FusionRecoveredAgainstAllSinglesConditionCount > 0)
            .Select(row => row.TaskFamilyId)
            .ToArray();

        if (improvedFamilies.Length > 0)
        {
            return $"Within this synthetic stream suite, simple fusion of ED/CAV with RMS-normalized mean improved boundary detection on {string.Join(", ", improvedFamilies.Select(name => $"`{name}`"))}, while remaining mixed elsewhere.";
        }

        if (recoveryFamilies.Length > 0)
        {
            return $"Fusion produced only marginal gains overall, so the compression-derived cue remains secondary but occasionally helpful, especially via condition-level recoveries on {string.Join(", ", recoveryFamilies.Select(name => $"`{name}`"))}.";
        }

        return "The compression-derived cue did not materially improve this simple boundary fusion in the tested synthetic stream families.";
    }

    private static double GetBestSingleHitRate(M7B2FusionSummaryRow row)
        => row.BestSingleSignalId switch
        {
            DetectorCatalog.EnergyDetectorName => row.EdMedianOnsetHitRate,
            DetectorCatalog.CovarianceAbsoluteValueDetectorName => row.CavMedianOnsetHitRate,
            DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName => row.NormalizedMeanMedianOnsetHitRate,
            _ => 0d,
        };

    private static double? GetBestSingleOnsetError(M7B2FusionSummaryRow row)
        => row.BestSingleSignalId switch
        {
            DetectorCatalog.EnergyDetectorName => row.EdMedianOnsetLocalizationError,
            DetectorCatalog.CovarianceAbsoluteValueDetectorName => row.CavMedianOnsetLocalizationError,
            DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName => row.NormalizedMeanMedianOnsetLocalizationError,
            _ => null,
        };

    private static double GetBestSingleFalsePositives(M7B2FusionSummaryRow row)
        => row.BestSingleSignalId switch
        {
            DetectorCatalog.EnergyDetectorName => row.EdMedianFalsePositiveCount,
            DetectorCatalog.CovarianceAbsoluteValueDetectorName => row.CavMedianFalsePositiveCount,
            DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName => row.NormalizedMeanMedianFalsePositiveCount,
            _ => 0d,
        };

    private static bool HasOffset(IReadOnlyList<M7BStreamBoundaryMetrics> metrics)
        => metrics.Any(metric => metric.OffsetHit.HasValue);

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

    private static double? NullableSubtract(double? left, double? right)
        => left.HasValue && right.HasValue ? Round(left.Value - right.Value) : null;

    private static string FormatNullable(double? value)
        => value.HasValue ? value.Value.ToString("F6", CultureInfo.InvariantCulture) : string.Empty;

    private static string Escape(string value)
        => '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';

    private static string FormatMetric(double? value)
        => value.HasValue ? value.Value.ToString("F3", CultureInfo.InvariantCulture) : "n/a";

    private sealed record SignalAggregate(
        string SignalId,
        double MedianOnsetHitRate,
        double? MedianOffsetHitRate,
        double? MedianOnsetLocalizationError,
        double? MedianOffsetLocalizationError,
        double MedianFalsePositiveCount);
}
