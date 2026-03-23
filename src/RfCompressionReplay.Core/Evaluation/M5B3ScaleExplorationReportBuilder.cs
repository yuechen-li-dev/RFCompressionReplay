using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M5B3ScaleExplorationReportBuilder
{
    private const string WholeStreamFamily = "whole-stream-mean";
    private const string HistogramFamily = "coarse-histogram";
    private const string PositionalFamily = "coarse-positional";

    private static readonly DetectorDescriptor[] AlternativeDetectors =
    [
        new(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, WholeStreamFamily),
        new(DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, HistogramFamily),
        new(DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, PositionalFamily),
    ];

    public static M5B3ExplorationArtifacts Build(
        M5B3ExplorationConfig config,
        IReadOnlyList<(M5B3RepresentationFamilyConfig Family, double ScaleValue, int Seed, ExperimentResult Result)> runs)
    {
        var comparisonRows = runs
            .SelectMany(run => BuildRowsForRun(run.Family.Id, run.ScaleValue, run.Seed, run.Result))
            .OrderBy(row => row.RepresentationFamilyId, StringComparer.Ordinal)
            .ThenBy(row => row.ScaleValue)
            .ThenBy(row => row.Seed)
            .ThenBy(row => row.TaskName, StringComparer.Ordinal)
            .ThenBy(row => row.ConditionSnrDb)
            .ThenBy(row => row.WindowLength)
            .ToArray();

        var deltaSummaryRows = config.RepresentationFamilies
            .SelectMany(family => BuildDeltaSummaryRows(
                family.Id,
                comparisonRows.Where(row => string.Equals(row.RepresentationFamilyId, family.Id, StringComparison.Ordinal)).ToArray()))
            .OrderBy(row => row.RepresentationFamilyId, StringComparer.Ordinal)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .ToArray();

        var scaleSummaryRows = config.RepresentationFamilies
            .SelectMany(family => config.ScaleValues.SelectMany(scaleValue =>
                BuildScaleSummaryRows(
                    family.Id,
                    scaleValue,
                    comparisonRows.Where(row => string.Equals(row.RepresentationFamilyId, family.Id, StringComparison.Ordinal) && NearlyEqual(row.ScaleValue, scaleValue)).ToArray())))
            .OrderBy(row => row.RepresentationFamilyId, StringComparer.Ordinal)
            .ThenBy(row => row.ScaleValue)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .ToArray();

        return new M5B3ExplorationArtifacts(
            comparisonRows,
            deltaSummaryRows,
            scaleSummaryRows,
            BuildFindingsMarkdown(config, comparisonRows, deltaSummaryRows, scaleSummaryRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M5B3AucComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("representationFamilyId,scaleValue,seed,taskName,conditionSnrDb,windowLength,aucLzmsaPaper,aucLzmsaMeanCompressedByteValue,aucLzmsaCompressedByteBucket64To127Proportion,aucLzmsaSuffixThirdMeanCompressedByteValue,absoluteDeltaFromPaperMeanCompressedByteValue,absoluteDeltaFromPaperCompressedByteBucket64To127Proportion,absoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.RepresentationFamilyId,
                row.ScaleValue.ToString("0.###", CultureInfo.InvariantCulture),
                row.Seed.ToString(CultureInfo.InvariantCulture),
                row.TaskName,
                row.ConditionSnrDb.ToString("F6", CultureInfo.InvariantCulture),
                row.WindowLength.ToString(CultureInfo.InvariantCulture),
                row.PaperAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.MeanCompressedByteValueAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.Bucket64To127ProportionAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.SuffixThirdMeanCompressedByteValueAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperMeanCompressedByteValue.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperBucket64To127Proportion.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteDeltaSummaryCsv(string path, IReadOnlyList<M5B3DeltaSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("representationFamilyId,alternativeDetectorName,featureFamily,closestNeighborCount,combinationCount,winRate,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper,medianClosenessRank");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.RepresentationFamilyId,
                row.AlternativeDetectorName,
                row.FeatureFamily,
                row.ClosestNeighborCount.ToString(CultureInfo.InvariantCulture),
                row.CombinationCount.ToString(CultureInfo.InvariantCulture),
                row.WinRate.ToString("F6", CultureInfo.InvariantCulture),
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MedianClosenessRank.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteScaleSummaryCsv(string path, IReadOnlyList<M5B3ScaleSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("representationFamilyId,scaleValue,alternativeDetectorName,featureFamily,closestNeighborCount,combinationCount,winRate,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper,medianClosenessRank,scaleMedianLeader,scaleClosestLeader,trendLabel");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.RepresentationFamilyId,
                row.ScaleValue.ToString("0.###", CultureInfo.InvariantCulture),
                row.AlternativeDetectorName,
                row.FeatureFamily,
                row.ClosestNeighborCount.ToString(CultureInfo.InvariantCulture),
                row.CombinationCount.ToString(CultureInfo.InvariantCulture),
                row.WinRate.ToString("F6", CultureInfo.InvariantCulture),
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MedianClosenessRank.ToString("F6", CultureInfo.InvariantCulture),
                row.ScaleMedianLeader,
                row.ScaleClosestLeader,
                row.TrendLabel));
        }
    }

    private static IReadOnlyList<M5B3AucComparisonRow> BuildRowsForRun(string representationFamilyId, double scaleValue, int seed, ExperimentResult result)
    {
        var summaryByCondition = result.Summary.Groups
            .Where(summary => summary.TaskName is not null && summary.ConditionSnrDb.HasValue && summary.WindowLength.HasValue && summary.Auc.HasValue)
            .ToDictionary(
                summary => (summary.TaskName!, summary.ConditionSnrDb!.Value, summary.WindowLength!.Value, summary.DetectorName),
                summary => summary.Auc!.Value,
                new ConditionDetectorKeyComparer());

        return result.Summary.Groups
            .Where(summary => string.Equals(summary.DetectorName, DetectorCatalog.LzmsaPaperDetectorName, StringComparison.OrdinalIgnoreCase)
                && summary.TaskName is not null
                && summary.ConditionSnrDb.HasValue
                && summary.WindowLength.HasValue
                && summary.Auc.HasValue)
            .Select(summary =>
            {
                var taskName = summary.TaskName!;
                var snrDb = summary.ConditionSnrDb!.Value;
                var windowLength = summary.WindowLength!.Value;
                var paperAuc = summary.Auc!.Value;
                var meanAuc = GetAuc(summaryByCondition, taskName, snrDb, windowLength, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName);
                var histogramAuc = GetAuc(summaryByCondition, taskName, snrDb, windowLength, DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName);
                var positionalAuc = GetAuc(summaryByCondition, taskName, snrDb, windowLength, DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName);

                return new M5B3AucComparisonRow(
                    representationFamilyId,
                    scaleValue,
                    seed,
                    taskName,
                    snrDb,
                    windowLength,
                    paperAuc,
                    meanAuc,
                    histogramAuc,
                    positionalAuc,
                    Round(Math.Abs(paperAuc - meanAuc)),
                    Round(Math.Abs(paperAuc - histogramAuc)),
                    Round(Math.Abs(paperAuc - positionalAuc)));
            })
            .OrderBy(row => row.TaskName, StringComparer.Ordinal)
            .ThenBy(row => row.ConditionSnrDb)
            .ThenBy(row => row.WindowLength)
            .ToArray();
    }

    private static IReadOnlyList<M5B3DeltaSummaryRow> BuildDeltaSummaryRows(string familyId, IReadOnlyList<M5B3AucComparisonRow> rows)
    {
        return AlternativeDetectors
            .Select(detector =>
            {
                var deltas = rows.Select(row => GetDelta(row, detector.DetectorName)).OrderBy(value => value).ToArray();
                var ranks = rows.Select(row => GetRank(row, detector.DetectorName)).OrderBy(value => value).ToArray();
                var closestCount = rows.Count(row => IsClosest(row, detector.DetectorName));

                return new M5B3DeltaSummaryRow(
                    familyId,
                    detector.DetectorName,
                    detector.FeatureFamily,
                    closestCount,
                    rows.Count,
                    rows.Count == 0 ? 0d : Round((double)closestCount / rows.Count),
                    deltas.Length == 0 ? 0d : Round(Median(deltas)),
                    deltas.Length == 0 ? 0d : Round(deltas.Max()),
                    ranks.Length == 0 ? 0d : Round(Median(ranks)));
            })
            .ToArray();
    }

    private static IReadOnlyList<M5B3ScaleSummaryRow> BuildScaleSummaryRows(string familyId, double scaleValue, IReadOnlyList<M5B3AucComparisonRow> rows)
    {
        var medianLeader = AlternativeDetectors
            .Select(detector => new
            {
                detector.DetectorName,
                Median = rows.Count == 0 ? 0d : Median(rows.Select(row => GetDelta(row, detector.DetectorName)).OrderBy(value => value).ToArray()),
                Max = rows.Count == 0 ? 0d : rows.Max(row => GetDelta(row, detector.DetectorName))
            })
            .OrderBy(entry => entry.Median)
            .ThenBy(entry => entry.Max)
            .ThenBy(entry => entry.DetectorName, StringComparer.Ordinal)
            .First();

        var closestLeader = AlternativeDetectors
            .Select(detector => new
            {
                detector.DetectorName,
                Count = rows.Count(row => IsClosest(row, detector.DetectorName)),
                MedianRank = rows.Count == 0 ? 0d : Median(rows.Select(row => GetRank(row, detector.DetectorName)).OrderBy(value => value).ToArray())
            })
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.MedianRank)
            .ThenBy(entry => entry.DetectorName, StringComparer.Ordinal)
            .First();

        return AlternativeDetectors
            .Select(detector =>
            {
                var deltas = rows.Select(row => GetDelta(row, detector.DetectorName)).OrderBy(value => value).ToArray();
                var ranks = rows.Select(row => GetRank(row, detector.DetectorName)).OrderBy(value => value).ToArray();
                var closestCount = rows.Count(row => IsClosest(row, detector.DetectorName));

                return new M5B3ScaleSummaryRow(
                    familyId,
                    scaleValue,
                    detector.DetectorName,
                    detector.FeatureFamily,
                    closestCount,
                    rows.Count,
                    rows.Count == 0 ? 0d : Round((double)closestCount / rows.Count),
                    deltas.Length == 0 ? 0d : Round(Median(deltas)),
                    deltas.Length == 0 ? 0d : Round(deltas.Max()),
                    ranks.Length == 0 ? 0d : Round(Median(ranks)),
                    medianLeader.DetectorName,
                    closestLeader.DetectorName,
                    BuildTrendLabel(detector.DetectorName, medianLeader.DetectorName, closestLeader.DetectorName));
            })
            .ToArray();
    }

    private static string BuildFindingsMarkdown(
        M5B3ExplorationConfig config,
        IReadOnlyList<M5B3AucComparisonRow> comparisonRows,
        IReadOnlyList<M5B3DeltaSummaryRow> deltaSummaryRows,
        IReadOnlyList<M5B3ScaleSummaryRow> scaleSummaryRows)
    {
        var rawLeaders = GetScaleLeaders(scaleSummaryRows, "raw-scaled", config.ScaleValues);
        var normalizedLeaders = GetScaleLeaders(scaleSummaryRows, "normalized-rms", config.ScaleValues);

        var rawWinnerPattern = DescribeWinnerPattern(rawLeaders);
        var normalizedWinnerPattern = DescribeWinnerPattern(normalizedLeaders);
        var rawReshuffleCount = CountWinnerTransitions(rawLeaders);
        var normalizedReshuffleCount = CountWinnerTransitions(normalizedLeaders);

        var rawOverallLeader = deltaSummaryRows
            .Where(row => string.Equals(row.RepresentationFamilyId, "raw-scaled", StringComparison.Ordinal))
            .OrderBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();
        var normalizedOverallLeader = deltaSummaryRows
            .Where(row => string.Equals(row.RepresentationFamilyId, "normalized-rms", StringComparison.Ordinal))
            .OrderBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();

        var rawMoreUnstable = rawReshuffleCount > normalizedReshuffleCount;
        var normalizationRead = rawReshuffleCount == normalizedReshuffleCount
            ? "Within this tested panel, the RMS-normalized variant did not reduce winner reshuffling relative to raw scaling at the coarse count level."
            : rawMoreUnstable
                ? "Within this tested panel, the RMS-normalized variant reduced winner reshuffling relative to raw scaling."
                : "Within this tested panel, the RMS-normalized variant did not look more stable than raw scaling and may have introduced comparable or greater winner reshuffling.";

        var familyRead = "Across both representation families, the closest practical neighbors remained inside the same coarse compressed-byte value / distribution / position neighborhood established earlier; M5b3 refines scale handling rather than changing that family-level story.";

        var sb = new StringBuilder();
        sb.AppendLine("# M5b3 Scale-Handling Refinement Findings");
        sb.AppendLine();
        sb.AppendLine("## Scope");
        sb.AppendLine();
        sb.AppendLine($"- Tasks run: {string.Join(", ", config.Evaluation.Tasks.Select(task => task.Name))}");
        sb.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        sb.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        sb.AppendLine($"- Seeds used: {string.Join(", ", config.SeedPanel)}");
        sb.AppendLine($"- Trial count per condition and class: {config.Evaluation.TrialCountPerCondition}");
        sb.AppendLine($"- Scale values tested: {string.Join(", ", config.ScaleValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture) + "x"))}");
        sb.AppendLine($"- Normalization rule used: per-window RMS normalization to target RMS {GetNormalizationTarget(config):0.###} before serialization for `normalized-rms`; `raw-scaled` uses no normalization.");
        sb.AppendLine($"- Feature panel used: {DetectorCatalog.LzmsaPaperDetectorName}, {DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName}, {DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName}, {DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName}");
        sb.AppendLine($"- Retention mode used: {config.ArtifactRetentionMode} (the repository's nearest compact retention mode for this exploration).");
        sb.AppendLine($"- Config provenance: {config.ExperimentId} / {config.ExperimentName}");
        sb.AppendLine();
        sb.AppendLine("## Scale-Sensitivity Read");
        sb.AppendLine();
        sb.AppendLine($"- Raw-scaled scale-trend winners by median absolute AUC delta: {FormatLeaderTrend(rawLeaders)}.");
        sb.AppendLine($"- RMS-normalized scale-trend winners by median absolute AUC delta: {FormatLeaderTrend(normalizedLeaders)}.");
        sb.AppendLine($"- Raw-scaled pattern read: {rawWinnerPattern}");
        sb.AppendLine($"- RMS-normalized pattern read: {normalizedWinnerPattern}");
        sb.AppendLine($"- Overall raw-scaled nearest practical leader across the tested panel: `{rawOverallLeader.AlternativeDetectorName}` ({rawOverallLeader.FeatureFamily}, median absolute AUC delta {rawOverallLeader.MedianAbsoluteAucDeltaFromPaper:F6}, closest-neighbor wins {rawOverallLeader.ClosestNeighborCount}/{rawOverallLeader.CombinationCount}).");
        sb.AppendLine($"- Overall RMS-normalized nearest practical leader across the tested panel: `{normalizedOverallLeader.AlternativeDetectorName}` ({normalizedOverallLeader.FeatureFamily}, median absolute AUC delta {normalizedOverallLeader.MedianAbsoluteAucDeltaFromPaper:F6}, closest-neighbor wins {normalizedOverallLeader.ClosestNeighborCount}/{normalizedOverallLeader.CombinationCount}).");
        sb.AppendLine();
        sb.AppendLine("## Normalization Read");
        sb.AppendLine();
        sb.AppendLine($"- Winner transitions across adjacent scales: raw-scaled = {rawReshuffleCount}, normalized-rms = {normalizedReshuffleCount}.");
        sb.AppendLine($"- {normalizationRead}");
        sb.AppendLine("- The normalization comparison materially changes the single-feature winner story only if the scale-leader sequence or overall family win counts move; inspect `m5b3_scale_summary.csv` for the exact counts and deltas.");
        sb.AppendLine();
        sb.AppendLine("## Family-Level Interpretation");
        sb.AppendLine();
        sb.AppendLine($"- {familyRead}");
        sb.AppendLine($"- Within the tested scale panel, the raw-scaled overall leader was `{rawOverallLeader.AlternativeDetectorName}` and the normalized overall leader was `{normalizedOverallLeader.AlternativeDetectorName}`.");
        sb.AppendLine();
        sb.AppendLine("## Scale Trend Table");
        sb.AppendLine();
        sb.AppendLine("| Representation family | Scale | Median-delta leader | Family | Median | Max | Closest-win leader | Closest wins |");
        sb.AppendLine("| --- | ---: | --- | --- | ---: | ---: | --- | ---: |");
        foreach (var familyId in config.RepresentationFamilies.Select(family => family.Id))
        {
            foreach (var scaleValue in config.ScaleValues.OrderBy(value => value))
            {
                var leader = GetScaleMedianLeader(scaleSummaryRows, familyId, scaleValue);
                var closest = GetScaleClosestLeader(scaleSummaryRows, familyId, scaleValue);
                sb.AppendLine($"| {familyId} | {scaleValue:0.###}x | {leader.AlternativeDetectorName} | {leader.FeatureFamily} | {leader.MedianAbsoluteAucDeltaFromPaper:F6} | {leader.MaxAbsoluteAucDeltaFromPaper:F6} | {closest.AlternativeDetectorName} | {closest.ClosestNeighborCount}/{closest.CombinationCount} |");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Overall Delta Summary by Representation Family");
        sb.AppendLine();
        sb.AppendLine("| Representation family | Feature | Family | Closest wins | Median | Max | Median rank |");
        sb.AppendLine("| --- | --- | --- | ---: | ---: | ---: | ---: |");
        foreach (var row in deltaSummaryRows.OrderBy(row => row.RepresentationFamilyId, StringComparer.Ordinal).ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper))
        {
            sb.AppendLine($"| {row.RepresentationFamilyId} | {row.AlternativeDetectorName} | {row.FeatureFamily} | {row.ClosestNeighborCount}/{row.CombinationCount} | {row.MedianAbsoluteAucDeltaFromPaper:F6} | {row.MaxAbsoluteAucDeltaFromPaper:F6} | {row.MedianClosenessRank:F6} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Caveats");
        sb.AppendLine();
        sb.AppendLine("- This remains a synthetic-only benchmark and is not an SDR, OTA, or deployment claim.");
        sb.AppendLine("- The OFDM-like task is a structured synthetic proxy, not LTE fidelity.");
        sb.AppendLine("- The deterministic Brotli compression backend caveat remains unchanged.");
        sb.AppendLine("- No SDR capture or hardware claims are supported here.");
        sb.AppendLine("- M5b3 is exploratory and compact-summary-first; it refines scale handling but does not resolve mechanism identity beyond the tested neighborhood.");
        sb.AppendLine("- The normalization comparison uses one simple per-window RMS rule only; no broader normalization family sweep was attempted.");
        sb.AppendLine();
        sb.AppendLine("## Artifact Notes");
        sb.AppendLine();
        sb.AppendLine($"- Comparison combinations retained: {comparisonRows.Count}");
        sb.AppendLine($"- Delta summary rows retained: {deltaSummaryRows.Count}");
        sb.AppendLine($"- Scale summary rows retained: {scaleSummaryRows.Count}");

        return sb.ToString();
    }

    private static double GetNormalizationTarget(M5B3ExplorationConfig config)
    {
        return config.RepresentationFamilies
            .Single(family => string.Equals(family.Id, "normalized-rms", StringComparison.Ordinal))
            .Representation
            .NormalizationTarget;
    }

    private static IReadOnlyList<(double ScaleValue, M5B3ScaleSummaryRow Leader)> GetScaleLeaders(IReadOnlyList<M5B3ScaleSummaryRow> rows, string familyId, IReadOnlyList<double> scaleValues)
    {
        return scaleValues
            .OrderBy(value => value)
            .Select(scaleValue => (scaleValue, GetScaleMedianLeader(rows, familyId, scaleValue)))
            .ToArray();
    }

    private static string FormatLeaderTrend(IReadOnlyList<(double ScaleValue, M5B3ScaleSummaryRow Leader)> leaders)
    {
        return string.Join("; ", leaders.Select(entry => $"{entry.ScaleValue:0.###}x -> `{entry.Leader.AlternativeDetectorName}`"));
    }

    private static string DescribeWinnerPattern(IReadOnlyList<(double ScaleValue, M5B3ScaleSummaryRow Leader)> leaders)
    {
        var distinctLeaders = leaders.Select(entry => entry.Leader.AlternativeDetectorName).Distinct(StringComparer.Ordinal).ToArray();
        if (distinctLeaders.Length == 1)
        {
            return $"the same winner held across all tested scales (`{distinctLeaders[0]}`), so this looks stable rather than monotone-reshuffling.";
        }

        var orderedLeaders = leaders.Select(entry => entry.Leader.AlternativeDetectorName).ToArray();
        if (orderedLeaders.Distinct(StringComparer.Ordinal).Count() == orderedLeaders.Length)
        {
            return "the winner changed at each tested scale, so this looks like a progressive reshuffle rather than a one-off flip.";
        }

        return "the winner changed across part of the scale panel but not every step, so this looks like localized reshuffling rather than a clean monotone handoff.";
    }

    private static int CountWinnerTransitions(IReadOnlyList<(double ScaleValue, M5B3ScaleSummaryRow Leader)> leaders)
    {
        var count = 0;
        for (var index = 1; index < leaders.Count; index++)
        {
            if (!string.Equals(leaders[index - 1].Leader.AlternativeDetectorName, leaders[index].Leader.AlternativeDetectorName, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    private static M5B3ScaleSummaryRow GetScaleMedianLeader(IReadOnlyList<M5B3ScaleSummaryRow> rows, string familyId, double scaleValue)
    {
        return rows
            .Where(row => string.Equals(row.RepresentationFamilyId, familyId, StringComparison.Ordinal) && NearlyEqual(row.ScaleValue, scaleValue))
            .OrderBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();
    }

    private static M5B3ScaleSummaryRow GetScaleClosestLeader(IReadOnlyList<M5B3ScaleSummaryRow> rows, string familyId, double scaleValue)
    {
        return rows
            .Where(row => string.Equals(row.RepresentationFamilyId, familyId, StringComparison.Ordinal) && NearlyEqual(row.ScaleValue, scaleValue))
            .OrderByDescending(row => row.ClosestNeighborCount)
            .ThenBy(row => row.MedianClosenessRank)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();
    }

    private static string BuildTrendLabel(string detectorName, string medianLeader, string closestLeader)
    {
        if (string.Equals(detectorName, medianLeader, StringComparison.Ordinal) && string.Equals(detectorName, closestLeader, StringComparison.Ordinal))
        {
            return "median+closest leader";
        }

        if (string.Equals(detectorName, medianLeader, StringComparison.Ordinal))
        {
            return "median leader";
        }

        if (string.Equals(detectorName, closestLeader, StringComparison.Ordinal))
        {
            return "closest-win leader";
        }

        return "non-leader";
    }

    private static double GetAuc(
        IReadOnlyDictionary<(string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName), double> summaryByCondition,
        string taskName,
        double conditionSnrDb,
        int windowLength,
        string detectorName)
    {
        return summaryByCondition[(taskName, conditionSnrDb, windowLength, detectorName)];
    }

    private static bool IsClosest(M5B3AucComparisonRow row, string detectorName)
    {
        var minDelta = AlternativeDetectors.Min(detector => GetDelta(row, detector.DetectorName));
        return NearlyEqual(GetDelta(row, detectorName), minDelta);
    }

    private static double GetDelta(M5B3AucComparisonRow row, string detectorName)
    {
        return detectorName switch
        {
            var value when string.Equals(value, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperMeanCompressedByteValue,
            var value when string.Equals(value, DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperBucket64To127Proportion,
            var value when string.Equals(value, DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue,
            _ => throw new InvalidOperationException($"Unsupported M5b3 detector '{detectorName}'."),
        };
    }

    private static double GetRank(M5B3AucComparisonRow row, string detectorName)
    {
        var ordered = AlternativeDetectors
            .Select(detector => (detector.DetectorName, Delta: GetDelta(row, detector.DetectorName)))
            .OrderBy(pair => pair.Delta)
            .ThenBy(pair => pair.DetectorName, StringComparer.Ordinal)
            .ToArray();

        for (var index = 0; index < ordered.Length; index++)
        {
            if (string.Equals(ordered[index].DetectorName, detectorName, StringComparison.Ordinal))
            {
                return index + 1;
            }
        }

        throw new InvalidOperationException($"Could not rank detector '{detectorName}'.");
    }

    private static double Median(IReadOnlyList<double> orderedValues)
    {
        if (orderedValues.Count == 0)
        {
            return 0d;
        }

        var middle = orderedValues.Count / 2;
        return orderedValues.Count % 2 == 0
            ? (orderedValues[middle - 1] + orderedValues[middle]) / 2d
            : orderedValues[middle];
    }

    private static double Round(double value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static bool NearlyEqual(double x, double y) => Math.Abs(x - y) <= 1e-9;

    private sealed record DetectorDescriptor(string DetectorName, string FeatureFamily);

    private sealed class ConditionDetectorKeyComparer : IEqualityComparer<(string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName)>
    {
        public bool Equals((string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName) x, (string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName) y)
        {
            return string.Equals(x.TaskName, y.TaskName, StringComparison.Ordinal)
                && NearlyEqual(x.ConditionSnrDb, y.ConditionSnrDb)
                && x.WindowLength == y.WindowLength
                && string.Equals(x.DetectorName, y.DetectorName, StringComparison.Ordinal);
        }

        public int GetHashCode((string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName) obj)
        {
            return HashCode.Combine(obj.TaskName, Math.Round(obj.ConditionSnrDb, 6), obj.WindowLength, obj.DetectorName);
        }
    }
}
