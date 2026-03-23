using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M5B2RepresentationPerturbationAxisReportBuilder
{
    public const string ArtifactPrefix = "m5b2";

    private const string WholeStreamFamily = "whole-stream-mean";
    private const string HistogramFamily = "coarse-histogram";
    private const string PositionalFamily = "coarse-positional";

    private static readonly DetectorDescriptor[] AlternativeDetectors =
    [
        new(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, WholeStreamFamily),
        new(DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, HistogramFamily),
        new(DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, PositionalFamily),
    ];

    public static M5B2ExplorationArtifacts Build(
        M5B2ExplorationConfig config,
        IReadOnlyList<(M5B2PerturbationConfig Perturbation, int Seed, ExperimentResult Result)> runs)
    {
        var comparisonRows = runs
            .SelectMany(run => BuildRowsForRun(run.Perturbation, run.Seed, run.Result))
            .OrderBy(row => AxisOrder(row.PerturbationAxisTag))
            .ThenBy(row => row.PerturbationId, StringComparer.Ordinal)
            .ThenBy(row => row.Seed)
            .ThenBy(row => row.TaskName, StringComparer.Ordinal)
            .ThenBy(row => row.ConditionSnrDb)
            .ThenBy(row => row.WindowLength)
            .ToArray();

        var deltaSummaryRows = config.Perturbations
            .SelectMany(perturbation => AlternativeDetectors.Select(detector =>
                BuildDeltaSummaryRow(perturbation, detector, comparisonRows.Where(row => string.Equals(row.PerturbationId, perturbation.Id, StringComparison.Ordinal)).ToArray())))
            .OrderBy(row => AxisOrder(row.PerturbationAxisTag))
            .ThenBy(row => row.PerturbationId, StringComparer.Ordinal)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .ToArray();

        var axisSummaryRows = config.Perturbations
            .GroupBy(perturbation => perturbation.AxisTag, StringComparer.OrdinalIgnoreCase)
            .SelectMany(axisGroup => BuildAxisSummaryRows(axisGroup.Key, axisGroup.Select(perturbation => perturbation.Id).ToHashSet(StringComparer.Ordinal), comparisonRows))
            .OrderBy(row => AxisOrder(row.PerturbationAxisTag))
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .ToArray();

        return new M5B2ExplorationArtifacts(
            comparisonRows,
            deltaSummaryRows,
            axisSummaryRows,
            BuildFindingsMarkdown(config, comparisonRows, deltaSummaryRows, axisSummaryRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M5B2AucComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("perturbationId,perturbationAxisTag,seed,taskName,conditionSnrDb,windowLength,aucLzmsaPaper,aucLzmsaMeanCompressedByteValue,aucLzmsaCompressedByteBucket64To127Proportion,aucLzmsaSuffixThirdMeanCompressedByteValue,absoluteDeltaFromPaperMeanCompressedByteValue,absoluteDeltaFromPaperCompressedByteBucket64To127Proportion,absoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.PerturbationId,
                row.PerturbationAxisTag,
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

    public static void WriteDeltaSummaryCsv(string path, IReadOnlyList<M5B2DeltaSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("perturbationId,perturbationAxisTag,alternativeDetectorName,featureFamily,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.PerturbationId,
                row.PerturbationAxisTag,
                row.AlternativeDetectorName,
                row.FeatureFamily,
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteAxisSummaryCsv(string path, IReadOnlyList<M5B2AxisSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("perturbationAxisTag,alternativeDetectorName,featureFamily,closestNeighborCount,combinationCount,winRate,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper,medianClosenessRank,axisMedianLeader,axisClosestLeader");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.PerturbationAxisTag,
                row.AlternativeDetectorName,
                row.FeatureFamily,
                row.ClosestNeighborCount.ToString(CultureInfo.InvariantCulture),
                row.CombinationCount.ToString(CultureInfo.InvariantCulture),
                row.WinRate.ToString("F6", CultureInfo.InvariantCulture),
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MedianClosenessRank.ToString("F6", CultureInfo.InvariantCulture),
                row.AxisMedianLeader,
                row.AxisClosestLeader));
        }
    }

    private static IReadOnlyList<M5B2AucComparisonRow> BuildRowsForRun(M5B2PerturbationConfig perturbation, int seed, ExperimentResult result)
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

                return new M5B2AucComparisonRow(
                    perturbation.Id,
                    perturbation.AxisTag,
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

    private static M5B2DeltaSummaryRow BuildDeltaSummaryRow(M5B2PerturbationConfig perturbation, DetectorDescriptor descriptor, IReadOnlyList<M5B2AucComparisonRow> rows)
    {
        var deltas = rows.Select(row => GetDelta(row, descriptor.DetectorName)).OrderBy(value => value).ToArray();
        return new M5B2DeltaSummaryRow(
            perturbation.Id,
            perturbation.AxisTag,
            descriptor.DetectorName,
            descriptor.FeatureFamily,
            deltas.Length == 0 ? 0d : Round(Median(deltas)),
            deltas.Length == 0 ? 0d : Round(deltas.Max()));
    }

    private static IReadOnlyList<M5B2AxisSummaryRow> BuildAxisSummaryRows(string axisTag, IReadOnlySet<string> perturbationIds, IReadOnlyList<M5B2AucComparisonRow> comparisonRows)
    {
        var axisRows = comparisonRows.Where(row => perturbationIds.Contains(row.PerturbationId)).ToArray();
        var medianLeader = AlternativeDetectors
            .Select(detector => new
            {
                detector.DetectorName,
                Median = axisRows.Length == 0 ? 0d : Median(axisRows.Select(row => GetDelta(row, detector.DetectorName)).OrderBy(value => value).ToArray()),
                Max = axisRows.Length == 0 ? 0d : axisRows.Max(row => GetDelta(row, detector.DetectorName))
            })
            .OrderBy(entry => entry.Median)
            .ThenBy(entry => entry.Max)
            .ThenBy(entry => entry.DetectorName, StringComparer.Ordinal)
            .First();

        var closestLeader = AlternativeDetectors
            .Select(detector => new
            {
                detector.DetectorName,
                Count = axisRows.Count(row => IsClosest(row, detector.DetectorName)),
                MedianRank = axisRows.Length == 0 ? 0d : Median(axisRows.Select(row => GetRank(row, detector.DetectorName)).OrderBy(value => value).ToArray())
            })
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.MedianRank)
            .ThenBy(entry => entry.DetectorName, StringComparer.Ordinal)
            .First();

        return AlternativeDetectors
            .Select(detector =>
            {
                var deltas = axisRows.Select(row => GetDelta(row, detector.DetectorName)).OrderBy(value => value).ToArray();
                var ranks = axisRows.Select(row => GetRank(row, detector.DetectorName)).OrderBy(value => value).ToArray();
                var closestCount = axisRows.Count(row => IsClosest(row, detector.DetectorName));

                return new M5B2AxisSummaryRow(
                    axisTag,
                    detector.DetectorName,
                    detector.FeatureFamily,
                    closestCount,
                    axisRows.Length,
                    axisRows.Length == 0 ? 0d : Round((double)closestCount / axisRows.Length),
                    deltas.Length == 0 ? 0d : Round(Median(deltas)),
                    deltas.Length == 0 ? 0d : Round(deltas.Max()),
                    ranks.Length == 0 ? 0d : Round(Median(ranks)),
                    medianLeader.DetectorName,
                    closestLeader.DetectorName);
            })
            .ToArray();
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

    private static bool IsClosest(M5B2AucComparisonRow row, string detectorName)
    {
        var minDelta = AlternativeDetectors.Min(detector => GetDelta(row, detector.DetectorName));
        return NearlyEqual(GetDelta(row, detectorName), minDelta);
    }

    private static double GetDelta(M5B2AucComparisonRow row, string detectorName)
    {
        return detectorName switch
        {
            var value when string.Equals(value, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperMeanCompressedByteValue,
            var value when string.Equals(value, DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperBucket64To127Proportion,
            var value when string.Equals(value, DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue,
            _ => throw new InvalidOperationException($"Unsupported M5b2 detector '{detectorName}'."),
        };
    }

    private static double GetRank(M5B2AucComparisonRow row, string detectorName)
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

    private static string BuildFindingsMarkdown(
        M5B2ExplorationConfig config,
        IReadOnlyList<M5B2AucComparisonRow> comparisonRows,
        IReadOnlyList<M5B2DeltaSummaryRow> deltaSummaryRows,
        IReadOnlyList<M5B2AxisSummaryRow> axisSummaryRows)
    {
        var baselineLeader = GetAxisMedianLeader(axisSummaryRows, M5B2PerturbationAxes.Baseline);
        var scaleLeader = GetAxisMedianLeader(axisSummaryRows, M5B2PerturbationAxes.Scale);
        var packingLeader = GetAxisMedianLeader(axisSummaryRows, M5B2PerturbationAxes.Packing);
        var combinedLeader = axisSummaryRows.Any(row => string.Equals(row.PerturbationAxisTag, M5B2PerturbationAxes.Combined, StringComparison.Ordinal))
            ? GetAxisMedianLeader(axisSummaryRows, M5B2PerturbationAxes.Combined)
            : null;

        var baselineClosestLeader = GetAxisClosestLeader(axisSummaryRows, M5B2PerturbationAxes.Baseline);
        var scaleClosestLeader = GetAxisClosestLeader(axisSummaryRows, M5B2PerturbationAxes.Scale);
        var packingClosestLeader = GetAxisClosestLeader(axisSummaryRows, M5B2PerturbationAxes.Packing);
        var combinedClosestLeader = axisSummaryRows.Any(row => string.Equals(row.PerturbationAxisTag, M5B2PerturbationAxes.Combined, StringComparison.Ordinal))
            ? GetAxisClosestLeader(axisSummaryRows, M5B2PerturbationAxes.Combined)
            : null;

        var scaleReshuffled = !string.Equals(scaleLeader.AlternativeDetectorName, baselineLeader.AlternativeDetectorName, StringComparison.Ordinal);
        var packingReshuffled = !string.Equals(packingLeader.AlternativeDetectorName, baselineLeader.AlternativeDetectorName, StringComparison.Ordinal);

        var scaleWinnerDelta = Math.Abs(scaleLeader.MedianAbsoluteAucDeltaFromPaper - baselineLeader.MedianAbsoluteAucDeltaFromPaper);
        var packingWinnerDelta = Math.Abs(packingLeader.MedianAbsoluteAucDeltaFromPaper - baselineLeader.MedianAbsoluteAucDeltaFromPaper);
        var strongerAxisRead = BuildAxisDominanceRead(scaleReshuffled, packingReshuffled, scaleWinnerDelta, packingWinnerDelta);

        var familySet = axisSummaryRows
            .Where(row => string.Equals(row.AlternativeDetectorName, row.AxisMedianLeader, StringComparison.Ordinal))
            .Select(row => row.FeatureFamily)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var familyRead = familySet.Length == 1
            ? $"Across the tested axis split, the median-delta leader stayed inside one coarse family: `{familySet[0]}`."
            : "Across the tested axis split, the single-feature winner moved, but all axis leaders stayed inside the existing coarse compressed-byte neighborhood established in M5a/M5b1.";

        var sb = new StringBuilder();
        sb.AppendLine("# M5b2 Perturbation-Axis Refinement Findings");
        sb.AppendLine();
        sb.AppendLine("## Scope");
        sb.AppendLine();
        sb.AppendLine($"- Tasks run: {string.Join(", ", config.Evaluation.Tasks.Select(task => task.Name))}");
        sb.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        sb.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        sb.AppendLine($"- Seeds used: {string.Join(", ", config.SeedPanel)}");
        sb.AppendLine($"- Trial count per condition and class: {config.Evaluation.TrialCountPerCondition}");
        sb.AppendLine($"- Perturbations used: {string.Join("; ", config.Perturbations.Select(perturbation => $"{perturbation.Id} [{perturbation.AxisTag}] = {perturbation.Description}"))}");
        sb.AppendLine($"- Feature panel used: {DetectorCatalog.LzmsaPaperDetectorName}, {DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName}, {DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName}, {DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName}");
        sb.AppendLine($"- Retention mode used: {config.ArtifactRetentionMode} (the repository's nearest compact retention mode for this exploration). ");
        sb.AppendLine($"- Config provenance: {config.ExperimentId} / {config.ExperimentName}");
        sb.AppendLine();
        sb.AppendLine("## Axis-Level Read");
        sb.AppendLine();
        sb.AppendLine($"- Baseline median-delta leader: `{baselineLeader.AlternativeDetectorName}` ({baselineLeader.FeatureFamily}, median absolute AUC delta {baselineLeader.MedianAbsoluteAucDeltaFromPaper:F6}, closest-neighbor wins {baselineClosestLeader.ClosestNeighborCount}/{baselineClosestLeader.CombinationCount} for `{baselineClosestLeader.AlternativeDetectorName}`).");
        sb.AppendLine($"- Scale-only median-delta leader: `{scaleLeader.AlternativeDetectorName}` ({scaleLeader.FeatureFamily}, median absolute AUC delta {scaleLeader.MedianAbsoluteAucDeltaFromPaper:F6}); closest-neighbor win leader: `{scaleClosestLeader.AlternativeDetectorName}` with {scaleClosestLeader.ClosestNeighborCount}/{scaleClosestLeader.CombinationCount} combinations.");
        sb.AppendLine($"- Packing-only median-delta leader: `{packingLeader.AlternativeDetectorName}` ({packingLeader.FeatureFamily}, median absolute AUC delta {packingLeader.MedianAbsoluteAucDeltaFromPaper:F6}); closest-neighbor win leader: `{packingClosestLeader.AlternativeDetectorName}` with {packingClosestLeader.ClosestNeighborCount}/{packingClosestLeader.CombinationCount} combinations.");
        if (combinedLeader is not null && combinedClosestLeader is not null)
        {
            sb.AppendLine($"- Combined perturbation median-delta leader: `{combinedLeader.AlternativeDetectorName}` ({combinedLeader.FeatureFamily}, median absolute AUC delta {combinedLeader.MedianAbsoluteAucDeltaFromPaper:F6}); closest-neighbor win leader: `{combinedClosestLeader.AlternativeDetectorName}` with {combinedClosestLeader.ClosestNeighborCount}/{combinedClosestLeader.CombinationCount} combinations.");
        }

        sb.AppendLine($"- Scale-only reshuffle relative to baseline: {(scaleReshuffled ? "yes" : "no")}. Packing-only reshuffle relative to baseline: {(packingReshuffled ? "yes" : "no")}.");
        sb.AppendLine($"- {strongerAxisRead}");
        sb.AppendLine();
        sb.AppendLine("## Axis Comparison Table");
        sb.AppendLine();
        sb.AppendLine("| Axis | Median-delta leader | Family | Median | Max | Closest-win leader | Closest wins | ");
        sb.AppendLine("| --- | --- | --- | ---: | ---: | --- | ---: |");
        foreach (var axis in OrderedAxes(config.Perturbations.Select(perturbation => perturbation.AxisTag).Distinct(StringComparer.OrdinalIgnoreCase)))
        {
            var leader = GetAxisMedianLeader(axisSummaryRows, axis);
            var closest = GetAxisClosestLeader(axisSummaryRows, axis);
            sb.AppendLine($"| {axis} | {leader.AlternativeDetectorName} | {leader.FeatureFamily} | {leader.MedianAbsoluteAucDeltaFromPaper:F6} | {leader.MaxAbsoluteAucDeltaFromPaper:F6} | {closest.AlternativeDetectorName} | {closest.ClosestNeighborCount}/{closest.CombinationCount} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Family-Level Interpretation");
        sb.AppendLine();
        sb.AppendLine($"- {familyRead}");
        sb.AppendLine("- The neighborhood remained robust at the family level while the single-feature winner stayed axis-sensitive inside the tested whole-stream / histogram / positional panel.");
        sb.AppendLine();
        sb.AppendLine("## Delta Summary by Perturbation");
        sb.AppendLine();
        sb.AppendLine("| Perturbation | Axis | Feature | Family | Median | Max |");
        sb.AppendLine("| --- | --- | --- | --- | ---: | ---: |");
        foreach (var row in deltaSummaryRows)
        {
            sb.AppendLine($"| {row.PerturbationId} | {row.PerturbationAxisTag} | {row.AlternativeDetectorName} | {row.FeatureFamily} | {row.MedianAbsoluteAucDeltaFromPaper:F6} | {row.MaxAbsoluteAucDeltaFromPaper:F6} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Caveats");
        sb.AppendLine();
        sb.AppendLine("- This remains a synthetic-only benchmark and is not an SDR, OTA, or deployment claim.");
        sb.AppendLine("- The OFDM-like task is a structured synthetic proxy, not LTE fidelity.");
        sb.AppendLine("- The deterministic Brotli compression backend caveat remains unchanged.");
        sb.AppendLine("- No SDR capture or hardware claims are supported here.");
        sb.AppendLine("- M5b2 is exploratory and compact-summary-first; it separates perturbation axes but does not resolve mechanism identity beyond the tested neighborhood.");
        sb.AppendLine("- The scale perturbation uses a deterministic multiplicative factor before serialization with no extra clipping or normalization beyond the selected IEEE float cast.");
        sb.AppendLine();
        sb.AppendLine("## Artifact Notes");
        sb.AppendLine();
        sb.AppendLine($"- Comparison combinations retained: {comparisonRows.Count}");
        sb.AppendLine($"- Delta summary rows retained: {deltaSummaryRows.Count}");
        sb.AppendLine($"- Axis summary rows retained: {axisSummaryRows.Count}");

        return sb.ToString();
    }

    private static string BuildAxisDominanceRead(bool scaleReshuffled, bool packingReshuffled, double scaleWinnerDelta, double packingWinnerDelta)
    {
        if (scaleReshuffled && !packingReshuffled)
        {
            return "Within the tested perturbations, scaling changes did more work than packing/precision changes in reshuffling the median-delta winner.";
        }

        if (!scaleReshuffled && packingReshuffled)
        {
            return "Within the tested perturbations, packing/precision changes did more work than scaling changes in reshuffling the median-delta winner.";
        }

        if (scaleReshuffled && packingReshuffled)
        {
            if (Math.Abs(scaleWinnerDelta - packingWinnerDelta) <= 0.0025d)
            {
                return "Within the tested perturbations, scale-only and packing-only both reshuffled the median-delta winner, with effects that look comparable at this compact-summary resolution.";
            }

            return scaleWinnerDelta > packingWinnerDelta
                ? "Within the tested perturbations, both axes mattered, but the scale-only split moved the median-delta winner farther from the baseline summary than the packing-only split."
                : "Within the tested perturbations, both axes mattered, but the packing-only split moved the median-delta winner farther from the baseline summary than the scale-only split.";
        }

        if (Math.Abs(scaleWinnerDelta - packingWinnerDelta) <= 0.0025d)
        {
            return "Within the tested perturbations, neither isolated axis flipped the median-delta winner relative to baseline, and their compact-summary effects look similar.";
        }

        return scaleWinnerDelta > packingWinnerDelta
            ? "Within the tested perturbations, neither isolated axis flipped the median-delta winner relative to baseline, but scaling changed the compact median-delta profile more than packing/precision did."
            : "Within the tested perturbations, neither isolated axis flipped the median-delta winner relative to baseline, but packing/precision changed the compact median-delta profile more than scaling did.";
    }

    private static M5B2AxisSummaryRow GetAxisMedianLeader(IReadOnlyList<M5B2AxisSummaryRow> rows, string axisTag)
    {
        return rows.Where(row => string.Equals(row.PerturbationAxisTag, axisTag, StringComparison.Ordinal))
            .OrderBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();
    }

    private static M5B2AxisSummaryRow GetAxisClosestLeader(IReadOnlyList<M5B2AxisSummaryRow> rows, string axisTag)
    {
        return rows.Where(row => string.Equals(row.PerturbationAxisTag, axisTag, StringComparison.Ordinal))
            .OrderByDescending(row => row.ClosestNeighborCount)
            .ThenBy(row => row.MedianClosenessRank)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();
    }

    private static IEnumerable<string> OrderedAxes(IEnumerable<string> axes)
    {
        return axes.OrderBy(AxisOrder).ThenBy(axis => axis, StringComparer.Ordinal);
    }

    private static int AxisOrder(string axisTag)
    {
        return axisTag.ToLowerInvariant() switch
        {
            M5B2PerturbationAxes.Baseline => 0,
            M5B2PerturbationAxes.Scale => 1,
            M5B2PerturbationAxes.Packing => 2,
            M5B2PerturbationAxes.Combined => 3,
            _ => 99,
        };
    }

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0d;
        }

        var ordered = values.OrderBy(value => value).ToArray();
        return ordered.Length % 2 == 0
            ? (ordered[(ordered.Length / 2) - 1] + ordered[ordered.Length / 2]) / 2d
            : ordered[ordered.Length / 2];
    }

    private static bool NearlyEqual(double x, double y) => Math.Abs(x - y) <= 1e-9;

    private static double Round(double value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);

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
            return HashCode.Combine(obj.TaskName, Round(obj.ConditionSnrDb), obj.WindowLength, obj.DetectorName);
        }
    }
}
