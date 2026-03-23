using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M5B1RepresentationPerturbationReportBuilder
{
    public const string ArtifactPrefix = "m5b1";

    private const string WholeStreamFamily = "whole-stream-mean";
    private const string HistogramFamily = "coarse-histogram";
    private const string PositionalFamily = "coarse-positional";

    private static readonly DetectorDescriptor[] AlternativeDetectors =
    [
        new(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, WholeStreamFamily),
        new(DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, HistogramFamily),
        new(DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, PositionalFamily),
    ];

    public static M5B1ExplorationArtifacts Build(
        M5B1ExplorationConfig config,
        IReadOnlyList<(string PerturbationId, int Seed, ExperimentResult Result)> runs)
    {
        var comparisonRows = runs
            .SelectMany(run => BuildRowsForRun(config, run.PerturbationId, run.Seed, run.Result))
            .OrderBy(row => row.PerturbationId, StringComparer.Ordinal)
            .ThenBy(row => row.Seed)
            .ThenBy(row => row.TaskName, StringComparer.Ordinal)
            .ThenBy(row => row.ConditionSnrDb)
            .ThenBy(row => row.WindowLength)
            .ToArray();

        var deltaSummaryRows = config.Perturbations
            .SelectMany(perturbation => AlternativeDetectors.Select(detector =>
                BuildDeltaSummaryRow(perturbation.Id, detector, comparisonRows.Where(row => string.Equals(row.PerturbationId, perturbation.Id, StringComparison.Ordinal)).ToArray())))
            .OrderBy(row => row.PerturbationId, StringComparer.Ordinal)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .ToArray();

        var overallStabilityRows = AlternativeDetectors
            .Select(detector => BuildStabilitySummaryRow("overall", null, detector, comparisonRows))
            .ToArray();

        var groupedStabilityRows = config.Perturbations
            .SelectMany(perturbation => AlternativeDetectors.Select(detector =>
                BuildStabilitySummaryRow("per-perturbation", perturbation.Id, detector, comparisonRows.Where(row => string.Equals(row.PerturbationId, perturbation.Id, StringComparison.Ordinal)).ToArray())))
            .ToArray();

        var stabilitySummaryRows = overallStabilityRows
            .Concat(groupedStabilityRows)
            .OrderBy(row => row.GroupingScope, StringComparer.Ordinal)
            .ThenBy(row => row.PerturbationId, StringComparer.Ordinal)
            .ThenByDescending(row => row.ClosestNeighborCount)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .ToArray();

        return new M5B1ExplorationArtifacts(
            comparisonRows,
            deltaSummaryRows,
            stabilitySummaryRows,
            BuildFindingsMarkdown(config, comparisonRows, deltaSummaryRows, stabilitySummaryRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M5B1AucComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("perturbationId,seed,taskName,conditionSnrDb,windowLength,aucLzmsaPaper,aucLzmsaMeanCompressedByteValue,aucLzmsaCompressedByteBucket64To127Proportion,aucLzmsaSuffixThirdMeanCompressedByteValue,absoluteDeltaFromPaperMeanCompressedByteValue,absoluteDeltaFromPaperCompressedByteBucket64To127Proportion,absoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.PerturbationId,
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

    public static void WriteDeltaSummaryCsv(string path, IReadOnlyList<M5B1DeltaSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("perturbationId,alternativeDetectorName,featureFamily,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.PerturbationId,
                row.AlternativeDetectorName,
                row.FeatureFamily,
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteStabilitySummaryCsv(string path, IReadOnlyList<M5B1StabilitySummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("groupingScope,perturbationId,alternativeDetectorName,featureFamily,closestNeighborCount,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper,medianClosenessRank");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.GroupingScope,
                row.PerturbationId ?? string.Empty,
                row.AlternativeDetectorName,
                row.FeatureFamily,
                row.ClosestNeighborCount.ToString(CultureInfo.InvariantCulture),
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MedianClosenessRank.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    private static IReadOnlyList<M5B1AucComparisonRow> BuildRowsForRun(M5B1ExplorationConfig config, string perturbationId, int seed, ExperimentResult result)
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

                return new M5B1AucComparisonRow(
                    perturbationId,
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

    private static double GetAuc(
        IReadOnlyDictionary<(string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName), double> summaryByCondition,
        string taskName,
        double conditionSnrDb,
        int windowLength,
        string detectorName)
    {
        return summaryByCondition[(taskName, conditionSnrDb, windowLength, detectorName)];
    }

    private static M5B1DeltaSummaryRow BuildDeltaSummaryRow(string perturbationId, DetectorDescriptor descriptor, IReadOnlyList<M5B1AucComparisonRow> rows)
    {
        var deltas = rows.Select(row => GetDelta(row, descriptor.DetectorName)).OrderBy(value => value).ToArray();
        return new M5B1DeltaSummaryRow(
            perturbationId,
            descriptor.DetectorName,
            descriptor.FeatureFamily,
            deltas.Length == 0 ? 0d : Round(Median(deltas)),
            deltas.Length == 0 ? 0d : Round(deltas.Max()));
    }

    private static M5B1StabilitySummaryRow BuildStabilitySummaryRow(string groupingScope, string? perturbationId, DetectorDescriptor descriptor, IReadOnlyList<M5B1AucComparisonRow> rows)
    {
        var deltas = rows.Select(row => GetDelta(row, descriptor.DetectorName)).ToArray();
        var ranks = rows.Select(row => GetRank(row, descriptor.DetectorName)).ToArray();
        var closestCount = rows.Count(row =>
        {
            var minDelta = AlternativeDetectors.Min(detector => GetDelta(row, detector.DetectorName));
            return NearlyEqual(GetDelta(row, descriptor.DetectorName), minDelta);
        });

        return new M5B1StabilitySummaryRow(
            groupingScope,
            perturbationId,
            descriptor.DetectorName,
            descriptor.FeatureFamily,
            closestCount,
            deltas.Length == 0 ? 0d : Round(Median(deltas)),
            deltas.Length == 0 ? 0d : Round(deltas.Max()),
            ranks.Length == 0 ? 0d : Round(Median(ranks)));
    }

    private static double GetDelta(M5B1AucComparisonRow row, string detectorName)
    {
        return detectorName switch
        {
            var value when string.Equals(value, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperMeanCompressedByteValue,
            var value when string.Equals(value, DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperBucket64To127Proportion,
            var value when string.Equals(value, DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue,
            _ => throw new InvalidOperationException($"Unsupported M5b1 detector '{detectorName}'."),
        };
    }

    private static double GetRank(M5B1AucComparisonRow row, string detectorName)
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
        M5B1ExplorationConfig config,
        IReadOnlyList<M5B1AucComparisonRow> comparisonRows,
        IReadOnlyList<M5B1DeltaSummaryRow> deltaSummaryRows,
        IReadOnlyList<M5B1StabilitySummaryRow> stabilitySummaryRows)
    {
        var overallRows = stabilitySummaryRows.Where(row => string.Equals(row.GroupingScope, "overall", StringComparison.Ordinal)).ToArray();
        var overallWinner = overallRows
            .OrderByDescending(row => row.ClosestNeighborCount)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MedianClosenessRank)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();

        var perPerturbationClosestWinners = config.Perturbations
            .Select(perturbation =>
            {
                var rows = stabilitySummaryRows
                    .Where(row => string.Equals(row.GroupingScope, "per-perturbation", StringComparison.Ordinal)
                        && string.Equals(row.PerturbationId, perturbation.Id, StringComparison.Ordinal))
                    .OrderByDescending(row => row.ClosestNeighborCount)
                    .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
                    .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
                    .ThenBy(row => row.MedianClosenessRank)
                    .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
                    .ToArray();

                return (Perturbation: perturbation, Winner: rows.First(), Rows: rows);
            })
            .ToArray();

        var perPerturbationMedianLeaders = config.Perturbations
            .Select(perturbation => deltaSummaryRows
                .Where(row => string.Equals(row.PerturbationId, perturbation.Id, StringComparison.Ordinal))
                .OrderBy(row => row.MedianAbsoluteAucDeltaFromPaper)
                .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
                .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
                .First())
            .ToArray();

        var winnerFamilies = perPerturbationMedianLeaders
            .Select(entry => entry.FeatureFamily)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var mainRead = winnerFamilies.Length == 1
            ? $"Within the tested perturbations, the best median-delta representative stayed within one family: `{winnerFamilies[0]}`."
            : "Within the tested perturbations, the single-feature median-delta leader changed, but the tested nearest-neighbor set did not collapse outside the existing M5a neighborhood.";

        var familyInterpretation = overallWinner.FeatureFamily switch
        {
            HistogramFamily => "The checked perturbation set still left the histogram/positional neighborhood closest overall to `lzmsa-paper`, with the histogram representative taking the best overall summary slot.",
            PositionalFamily => "The checked perturbation set still left the histogram/positional neighborhood closest overall to `lzmsa-paper`, with the positional representative taking the best overall summary slot.",
            _ => "The checked perturbation set moved the overall closest-neighbor counts toward whole-stream mean compressed byte value, while the baseline median-delta view still kept the current histogram representative competitive rather than collapsing the neighborhood entirely."
        };

        var sb = new StringBuilder();
        sb.AppendLine("# M5b1 Representation Perturbation Exploration Findings");
        sb.AppendLine();
        sb.AppendLine("## Scope");
        sb.AppendLine();
        sb.AppendLine($"- Tasks run: {string.Join(", ", config.Evaluation.Tasks.Select(task => task.Name))}");
        sb.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        sb.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        sb.AppendLine($"- Seeds used: {string.Join(", ", config.SeedPanel)}");
        sb.AppendLine($"- Trial count per condition and class: {config.Evaluation.TrialCountPerCondition}");
        sb.AppendLine($"- Perturbations used: {string.Join("; ", config.Perturbations.Select(perturbation => $"{perturbation.Id} = {perturbation.Description}"))}");
        sb.AppendLine($"- Feature panel used: {DetectorCatalog.LzmsaPaperDetectorName}, {DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName}, {DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName}, {DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName}");
        sb.AppendLine("- Representative feature choice note: `lzmsa-compressed-byte-bucket-64-127-proportion` is the histogram representative because the checked-in M5a2r compact run reported it as the closest simple neighbor, while `lzmsa-suffix-third-mean-compressed-byte-value` is the positional representative because prior M5a2/M5a3 evidence kept the suffix-third family in the nearest-neighbor set.");
        sb.AppendLine($"- Retention mode used: {config.ArtifactRetentionMode} (used as the nearest compact retention mode already supported by the repository policy).");
        sb.AppendLine($"- Config provenance: {config.ExperimentId} / {config.ExperimentName}");
        sb.AppendLine();
        sb.AppendLine("## Main Perturbation Read");
        sb.AppendLine();
        sb.AppendLine($"- {mainRead}");
        sb.AppendLine($"- Overall closest tested feature by closest-neighbor wins was `{overallWinner.AlternativeDetectorName}` ({overallWinner.FeatureFamily}), with {overallWinner.ClosestNeighborCount} wins, median absolute AUC delta {overallWinner.MedianAbsoluteAucDeltaFromPaper:F6}, and max absolute AUC delta {overallWinner.MaxAbsoluteAucDeltaFromPaper:F6}.");
        sb.AppendLine($"- By per-perturbation median absolute AUC delta, the leaders were: {string.Join("; ", config.Perturbations.Zip(perPerturbationMedianLeaders, (perturbation, leader) => $"{perturbation.Id} -> {leader.AlternativeDetectorName} ({leader.FeatureFamily}, median {leader.MedianAbsoluteAucDeltaFromPaper:F6})"))}.");
        sb.AppendLine();
        sb.AppendLine("## Per-Perturbation Read");
        sb.AppendLine();
        sb.AppendLine("| Perturbation | Best median-delta feature | Family | Median | Max | Closest-neighbor wins |");
        sb.AppendLine("| --- | --- | --- | ---: | ---: | ---: |");
        foreach (var perturbation in config.Perturbations)
        {
            var medianLeader = perPerturbationMedianLeaders.Single(row => string.Equals(row.PerturbationId, perturbation.Id, StringComparison.Ordinal));
            var closestWinner = perPerturbationClosestWinners.Single(entry => string.Equals(entry.Perturbation.Id, perturbation.Id, StringComparison.Ordinal)).Winner;
            var closestCount = stabilitySummaryRows.Single(row =>
                string.Equals(row.GroupingScope, "per-perturbation", StringComparison.Ordinal)
                && string.Equals(row.PerturbationId, perturbation.Id, StringComparison.Ordinal)
                && string.Equals(row.AlternativeDetectorName, medianLeader.AlternativeDetectorName, StringComparison.Ordinal)).ClosestNeighborCount;

            sb.AppendLine($"| {perturbation.Id} | {medianLeader.AlternativeDetectorName} | {medianLeader.FeatureFamily} | {medianLeader.MedianAbsoluteAucDeltaFromPaper:F6} | {medianLeader.MaxAbsoluteAucDeltaFromPaper:F6} | {closestCount} {(string.Equals(closestWinner.AlternativeDetectorName, medianLeader.AlternativeDetectorName, StringComparison.Ordinal) ? string.Empty : $"(closest-win leader: {closestWinner.AlternativeDetectorName})")} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Family-Level Interpretation");
        sb.AppendLine();
        sb.AppendLine($"- {familyInterpretation}");
        sb.AppendLine($"- Whole-stream mean representative (`{DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName}`) overall median absolute AUC delta: {overallRows.Single(row => string.Equals(row.AlternativeDetectorName, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, StringComparison.Ordinal)).MedianAbsoluteAucDeltaFromPaper:F6}.");
        sb.AppendLine($"- Histogram representative (`{DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName}`) overall median absolute AUC delta: {overallRows.Single(row => string.Equals(row.AlternativeDetectorName, DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, StringComparison.Ordinal)).MedianAbsoluteAucDeltaFromPaper:F6}.");
        sb.AppendLine($"- Positional representative (`{DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName}`) overall median absolute AUC delta: {overallRows.Single(row => string.Equals(row.AlternativeDetectorName, DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, StringComparison.Ordinal)).MedianAbsoluteAucDeltaFromPaper:F6}.");
        sb.AppendLine();
        sb.AppendLine("## Delta Summary by Perturbation");
        sb.AppendLine();
        sb.AppendLine("| Perturbation | Feature | Family | Median | Max |");
        sb.AppendLine("| --- | --- | --- | ---: | ---: |");
        foreach (var row in deltaSummaryRows
                     .OrderBy(row => row.PerturbationId, StringComparer.Ordinal)
                     .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
                     .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal))
        {
            sb.AppendLine($"| {row.PerturbationId} | {row.AlternativeDetectorName} | {row.FeatureFamily} | {row.MedianAbsoluteAucDeltaFromPaper:F6} | {row.MaxAbsoluteAucDeltaFromPaper:F6} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Caveats");
        sb.AppendLine();
        sb.AppendLine("- This is still a synthetic-only benchmark and should not be read as an SDR or deployment claim.");
        sb.AppendLine("- The OFDM-like task is a structured synthetic proxy, not LTE fidelity.");
        sb.AppendLine("- The deterministic Brotli compression backend caveat remains unchanged.");
        sb.AppendLine("- No SDR capture, OTA, or hardware claims are supported here.");
        sb.AppendLine("- M5b1 is exploratory and compact-summary-first; it checks modest representation robustness only, not full mechanism resolution.");
        sb.AppendLine("- The numeric scaling perturbation applies a deterministic multiplicative factor before serialization, with no extra clipping or normalization beyond the selected IEEE float cast.");
        sb.AppendLine();
        sb.AppendLine("## Artifact Notes");
        sb.AppendLine();
        sb.AppendLine($"- Comparison combinations retained: {comparisonRows.Count}");
        sb.AppendLine($"- Delta summary rows retained: {deltaSummaryRows.Count}");
        sb.AppendLine($"- Stability summary rows retained: {stabilitySummaryRows.Count}");

        return sb.ToString();
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
