using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M5A2ScoreDecompositionReportBuilder
{
    public const string ArtifactPrefix = "m5a2";

    private const string WholeStreamFamily = "whole-stream";
    private const string HistogramFamily = "coarse-histogram";
    private const string PositionalFamily = "coarse-positional";
    private const string PreviouslyReportedClosestDetector = DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName;
    private const int PreviouslyReportedBeatCount = 7;
    private const string PreviouslyReportedBestFamily = PositionalFamily;

    private static readonly string[] RequiredDetectorNames =
    [
        DetectorCatalog.LzmsaPaperDetectorName,
        DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
        DetectorCatalog.LzmsaCompressedByteVarianceDetectorName,
        DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorName,
        DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName,
        DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorName,
        DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorName,
        DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorName,
        DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName,
    ];

    private static readonly IReadOnlyDictionary<string, string> FeatureFamilyByDetector =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName] = WholeStreamFamily,
            [DetectorCatalog.LzmsaCompressedByteVarianceDetectorName] = WholeStreamFamily,
            [DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorName] = HistogramFamily,
            [DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName] = HistogramFamily,
            [DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorName] = HistogramFamily,
            [DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorName] = HistogramFamily,
            [DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorName] = PositionalFamily,
            [DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName] = PositionalFamily,
        };

    public static bool IsEnabled(ExperimentConfig config)
    {
        if (config.Evaluation is null)
        {
            return false;
        }

        var detectorNames = config.Evaluation.Detectors
            .Select(detector => detector.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return detectorNames.SetEquals(RequiredDetectorNames);
    }

    public static M5A2ComparisonReport Build(ExperimentConfig config, ExperimentResult result)
    {
        if (!IsEnabled(config) || result.Summary.Groups.Count == 0)
        {
            throw new InvalidOperationException("M5a2 comparison reporting requires an evaluation run containing the intended M5a2 compression-derived detector identities.");
        }

        var summaryByCondition = result.Summary.Groups
            .Where(summary => summary.TaskName is not null && summary.ConditionSnrDb.HasValue && summary.WindowLength.HasValue && summary.Auc.HasValue)
            .ToDictionary(
                summary => (summary.TaskName!, summary.ConditionSnrDb!.Value, summary.WindowLength!.Value, summary.DetectorName),
                summary => summary,
                new ConditionDetectorKeyComparer());

        var rows = new List<M5A2ComparisonRow>();

        var conditionKeys = result.Summary.Groups
            .Where(summary => summary.TaskName is not null && summary.ConditionSnrDb.HasValue && summary.WindowLength.HasValue)
            .Select(summary => (TaskName: summary.TaskName!, ConditionSnrDb: summary.ConditionSnrDb!.Value, WindowLength: summary.WindowLength!.Value))
            .Distinct()
            .OrderBy(key => key.TaskName, StringComparer.Ordinal)
            .ThenBy(key => key.ConditionSnrDb)
            .ThenBy(key => key.WindowLength)
            .ToArray();

        foreach (var key in conditionKeys)
        {
            var paper = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaPaperDetectorName);
            var mean = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName);
            var variance = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaCompressedByteVarianceDetectorName);
            var bucket0To63 = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorName);
            var bucket64To127 = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName);
            var bucket128To191 = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorName);
            var bucket192To255 = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorName);
            var prefix = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorName);
            var suffix = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName);

            rows.Add(new M5A2ComparisonRow(
                TaskName: key.TaskName,
                ConditionSnrDb: key.ConditionSnrDb,
                WindowLength: key.WindowLength,
                PaperAuc: paper,
                MeanCompressedByteValueAuc: mean,
                CompressedByteVarianceAuc: variance,
                Bucket0To63ProportionAuc: bucket0To63,
                Bucket64To127ProportionAuc: bucket64To127,
                Bucket128To191ProportionAuc: bucket128To191,
                Bucket192To255ProportionAuc: bucket192To255,
                PrefixThirdMeanCompressedByteValueAuc: prefix,
                SuffixThirdMeanCompressedByteValueAuc: suffix,
                PaperMinusMeanCompressedByteValue: RoundDelta(paper - mean),
                PaperMinusCompressedByteVariance: RoundDelta(paper - variance),
                PaperMinusBucket0To63Proportion: RoundDelta(paper - bucket0To63),
                PaperMinusBucket64To127Proportion: RoundDelta(paper - bucket64To127),
                PaperMinusBucket128To191Proportion: RoundDelta(paper - bucket128To191),
                PaperMinusBucket192To255Proportion: RoundDelta(paper - bucket192To255),
                PaperMinusPrefixThirdMeanCompressedByteValue: RoundDelta(paper - prefix),
                PaperMinusSuffixThirdMeanCompressedByteValue: RoundDelta(paper - suffix)));
        }

        var aggregateDeltaRows = BuildAggregateDeltaRows(rows);
        return new M5A2ComparisonReport(rows, aggregateDeltaRows, BuildFindingsMarkdown(config, result, rows, aggregateDeltaRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M5A2ComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskName,conditionSnrDb,windowLength,aucLzmsaPaper,aucLzmsaMeanCompressedByteValue,aucLzmsaCompressedByteVariance,aucLzmsaCompressedByteBucket0To63Proportion,aucLzmsaCompressedByteBucket64To127Proportion,aucLzmsaCompressedByteBucket128To191Proportion,aucLzmsaCompressedByteBucket192To255Proportion,aucLzmsaPrefixThirdMeanCompressedByteValue,aucLzmsaSuffixThirdMeanCompressedByteValue,deltaPaperMinusMeanCompressedByteValue,deltaPaperMinusCompressedByteVariance,deltaPaperMinusCompressedByteBucket0To63Proportion,deltaPaperMinusCompressedByteBucket64To127Proportion,deltaPaperMinusCompressedByteBucket128To191Proportion,deltaPaperMinusCompressedByteBucket192To255Proportion,deltaPaperMinusPrefixThirdMeanCompressedByteValue,deltaPaperMinusSuffixThirdMeanCompressedByteValue");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskName,
                row.ConditionSnrDb.ToString("F6", CultureInfo.InvariantCulture),
                row.WindowLength.ToString(CultureInfo.InvariantCulture),
                row.PaperAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.MeanCompressedByteValueAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.CompressedByteVarianceAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.Bucket0To63ProportionAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.Bucket64To127ProportionAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.Bucket128To191ProportionAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.Bucket192To255ProportionAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.PrefixThirdMeanCompressedByteValueAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.SuffixThirdMeanCompressedByteValueAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusMeanCompressedByteValue.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusCompressedByteVariance.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusBucket0To63Proportion.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusBucket64To127Proportion.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusBucket128To191Proportion.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusBucket192To255Proportion.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusPrefixThirdMeanCompressedByteValue.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusSuffixThirdMeanCompressedByteValue.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteAggregateDeltaCsv(string path, IReadOnlyList<M5A2AggregateDeltaRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("alternativeDetectorName,featureFamily,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper,closerThanWholeStreamMeanConditionCount");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.AlternativeDetectorName,
                row.FeatureFamily,
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.CloserThanWholeStreamMeanConditionCount.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static IReadOnlyList<M5A2AggregateDeltaRow> BuildAggregateDeltaRows(IReadOnlyList<M5A2ComparisonRow> rows)
    {
        return
        [
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, WholeStreamFamily, rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray(), rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray()),
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaCompressedByteVarianceDetectorName, WholeStreamFamily, rows.Select(row => Math.Abs(row.PaperMinusCompressedByteVariance)).ToArray(), rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray()),
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorName, HistogramFamily, rows.Select(row => Math.Abs(row.PaperMinusBucket0To63Proportion)).ToArray(), rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray()),
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, HistogramFamily, rows.Select(row => Math.Abs(row.PaperMinusBucket64To127Proportion)).ToArray(), rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray()),
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorName, HistogramFamily, rows.Select(row => Math.Abs(row.PaperMinusBucket128To191Proportion)).ToArray(), rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray()),
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorName, HistogramFamily, rows.Select(row => Math.Abs(row.PaperMinusBucket192To255Proportion)).ToArray(), rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray()),
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorName, PositionalFamily, rows.Select(row => Math.Abs(row.PaperMinusPrefixThirdMeanCompressedByteValue)).ToArray(), rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray()),
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, PositionalFamily, rows.Select(row => Math.Abs(row.PaperMinusSuffixThirdMeanCompressedByteValue)).ToArray(), rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray()),
        ];
    }

    private static M5A2AggregateDeltaRow BuildAggregateDeltaRow(string detectorName, string featureFamily, IReadOnlyList<double> deltas, IReadOnlyList<double> meanDeltas)
    {
        if (deltas.Count == 0)
        {
            return new M5A2AggregateDeltaRow(detectorName, featureFamily, 0d, 0d, 0);
        }

        var ordered = deltas.OrderBy(value => value).ToArray();
        var median = ordered.Length % 2 == 0
            ? (ordered[(ordered.Length / 2) - 1] + ordered[ordered.Length / 2]) / 2d
            : ordered[ordered.Length / 2];

        var closerThanMeanCount = deltas.Zip(meanDeltas, static (delta, meanDelta) => delta < meanDelta ? 1 : 0).Sum();

        return new M5A2AggregateDeltaRow(
            detectorName,
            featureFamily,
            RoundDelta(median),
            RoundDelta(ordered.Max()),
            closerThanMeanCount);
    }

    private static string BuildFindingsMarkdown(ExperimentConfig config, ExperimentResult result, IReadOnlyList<M5A2ComparisonRow> rows, IReadOnlyList<M5A2AggregateDeltaRow> aggregateDeltaRows)
    {
        var findings = SummarizeFindings(rows, aggregateDeltaRows);
        var sb = new StringBuilder();
        sb.AppendLine("# M5a2 Compressed-Stream Decomposition Findings");
        sb.AppendLine();
        sb.AppendLine("## Scope");
        sb.AppendLine();
        sb.AppendLine($"- Tasks run: {string.Join(", ", config.Evaluation!.Tasks.Select(task => task.Name))}");
        sb.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        sb.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        sb.AppendLine($"- Trial count per condition and class: {config.Evaluation.TrialCountPerCondition}");
        sb.AppendLine($"- Detector identities compared: {string.Join(", ", RequiredDetectorNames)}");
        sb.AppendLine($"- Seed: {config.Seed}");
        sb.AppendLine($"- Config provenance: {config.ExperimentId} / {config.ExperimentName}");
        sb.AppendLine();
        sb.AppendLine("## Main Comparison Statement");
        sb.AppendLine();
        sb.AppendLine($"- {findings.MainStatement}");
        sb.AppendLine();
        sb.AppendLine("## Condition Summary");
        sb.AppendLine();
        sb.AppendLine($"- `{findings.ClosestDetectorName}` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta ({findings.ClosestMedianDelta:F6}).");
        sb.AppendLine($"- It was closer to `lzmsa-paper` than whole-stream mean compressed byte value in {findings.ClosestBeatsMeanCount} of {rows.Count} tested conditions.");
        sb.AppendLine($"- The most informative tested feature family by best-member median absolute AUC delta was `{findings.BestFamily}`.");
        sb.AppendLine();
        sb.AppendLine("## Comparison Table");
        sb.AppendLine();
        sb.AppendLine("| Task | SNR dB | Window | AUC paper | AUC mean | AUC variance | AUC bucket 0-63 | AUC bucket 64-127 | AUC bucket 128-191 | AUC bucket 192-255 | AUC prefix-third mean | AUC suffix-third mean | Δ paper-mean | Δ paper-variance | Δ paper-bucket 0-63 | Δ paper-bucket 64-127 | Δ paper-bucket 128-191 | Δ paper-bucket 192-255 | Δ paper-prefix-third | Δ paper-suffix-third |");
        sb.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var row in rows)
        {
            sb.AppendLine($"| {row.TaskName} | {row.ConditionSnrDb.ToString("0.###", CultureInfo.InvariantCulture)} | {row.WindowLength} | {row.PaperAuc:F6} | {row.MeanCompressedByteValueAuc:F6} | {row.CompressedByteVarianceAuc:F6} | {row.Bucket0To63ProportionAuc:F6} | {row.Bucket64To127ProportionAuc:F6} | {row.Bucket128To191ProportionAuc:F6} | {row.Bucket192To255ProportionAuc:F6} | {row.PrefixThirdMeanCompressedByteValueAuc:F6} | {row.SuffixThirdMeanCompressedByteValueAuc:F6} | {row.PaperMinusMeanCompressedByteValue:F6} | {row.PaperMinusCompressedByteVariance:F6} | {row.PaperMinusBucket0To63Proportion:F6} | {row.PaperMinusBucket64To127Proportion:F6} | {row.PaperMinusBucket128To191Proportion:F6} | {row.PaperMinusBucket192To255Proportion:F6} | {row.PaperMinusPrefixThirdMeanCompressedByteValue:F6} | {row.PaperMinusSuffixThirdMeanCompressedByteValue:F6} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Aggregate Delta Summary");
        sb.AppendLine();
        sb.AppendLine("| Alternative detector | Family | Median | Max | Conditions closer than whole-stream mean |");
        sb.AppendLine("| --- | --- | ---: | ---: | ---: |");
        foreach (var row in aggregateDeltaRows.OrderBy(row => row.MedianAbsoluteAucDeltaFromPaper).ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal))
        {
            sb.AppendLine($"| {row.AlternativeDetectorName} | {row.FeatureFamily} | {row.MedianAbsoluteAucDeltaFromPaper:F6} | {row.MaxAbsoluteAucDeltaFromPaper:F6} | {row.CloserThanWholeStreamMeanConditionCount} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Cautious Interpretation");
        sb.AppendLine();
        sb.AppendLine($"- {findings.Interpretation}");
        sb.AppendLine();
        sb.AppendLine("## Re-land Comparison Note");
        sb.AppendLine();
        sb.AppendLine($"- {findings.RelandComparisonNote}");
        sb.AppendLine();
        sb.AppendLine("## Caveats");
        sb.AppendLine();
        sb.AppendLine("- This artifact set is limited to the repository's current synthetic benchmark tasks and evaluation conditions.");
        sb.AppendLine("- The OFDM-like task is a structured synthetic proxy, not LTE fidelity or a standards-faithful waveform.");
        sb.AppendLine("- The current deterministic serialization + Brotli compression backend remains fixed.");
        sb.AppendLine("- No SDR capture, over-the-air, or hardware claims are supported by this artifact set.");
        sb.AppendLine("- The coarse summary family comparison is local to this synthetic benchmark and should not be overgeneralized.");
        sb.AppendLine();
        sb.AppendLine("## Artifact Notes");
        sb.AppendLine();
        sb.AppendLine($"- Per-trial score rows: {result.Trials.Count}");
        sb.AppendLine($"- Per-condition summary rows: {result.Summary.Groups.Count}");
        sb.AppendLine($"- ROC point rows: {result.Evaluation?.RocPoints.Count ?? 0}");

        return sb.ToString();
    }

    private static FindingsSummary SummarizeFindings(IReadOnlyList<M5A2ComparisonRow> rows, IReadOnlyList<M5A2AggregateDeltaRow> aggregateDeltaRows)
    {
        if (rows.Count == 0 || aggregateDeltaRows.Count == 0)
        {
            return new FindingsSummary(
                MainStatement: "The requested M5a2 comparison did not produce evaluable rows.",
                ClosestDetectorName: DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
                ClosestMedianDelta: 0d,
                ClosestBeatsMeanCount: 0,
                BestFamily: WholeStreamFamily,
                Interpretation: "This run does not yet support a M5a2 interpretation because no evaluable AUC rows were produced.",
                RelandComparisonNote: "No same-scope re-land comparison can be made because the requested M5a2 run did not produce evaluable AUC rows.");
        }

        var bestOverall = aggregateDeltaRows
            .OrderBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();

        var bestFamily = aggregateDeltaRows
            .GroupBy(row => row.FeatureFamily, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(row => row.MedianAbsoluteAucDeltaFromPaper)
                .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
                .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
                .First())
            .OrderBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.FeatureFamily, StringComparer.Ordinal)
            .First()
            .FeatureFamily;

        var mainStatement = bestOverall.AlternativeDetectorName == DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName
            ? $"Within the current synthetic benchmark, suffix-third mean compressed byte value was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta."
            : $"Within the current synthetic benchmark, `{bestOverall.AlternativeDetectorName}` was the closest tested simple neighbor to `lzmsa-paper` by median absolute AUC delta.";

        var interpretation = bestOverall.CloserThanWholeStreamMeanConditionCount > rows.Count / 2
            ? $"The tested coarse positional summaries retained the strongest simple-neighbor relationship to `lzmsa-paper`, with `{bestOverall.AlternativeDetectorName}` beating whole-stream mean compressed byte value in {bestOverall.CloserThanWholeStreamMeanConditionCount} of {rows.Count} tested conditions. This preserves a cautious local mechanism hint rather than a full explanation."
            : $"No tested simple summary separated decisively from whole-stream mean compressed byte value across the full matrix. This keeps the M5a2 interpretation cautious and local.";

        return new FindingsSummary(
            mainStatement,
            bestOverall.AlternativeDetectorName,
            bestOverall.MedianAbsoluteAucDeltaFromPaper,
            bestOverall.CloserThanWholeStreamMeanConditionCount,
            bestFamily,
            interpretation,
            BuildRelandComparisonNote(bestOverall, bestFamily, rows.Count));
    }

    private static string BuildRelandComparisonNote(M5A2AggregateDeltaRow bestOverall, string bestFamily, int conditionCount)
    {
        var sameClosestDetector = string.Equals(bestOverall.AlternativeDetectorName, PreviouslyReportedClosestDetector, StringComparison.OrdinalIgnoreCase);
        var sameBeatCount = bestOverall.CloserThanWholeStreamMeanConditionCount == PreviouslyReportedBeatCount;
        var sameFamily = string.Equals(bestFamily, PreviouslyReportedBestFamily, StringComparison.OrdinalIgnoreCase);

        if (sameClosestDetector && sameBeatCount && sameFamily)
        {
            return "The same-scope re-land on current main preserved the previously reported cautious M5a2 finding: suffix-third mean remained the closest tested simple neighbor, it again beat whole-stream mean in 7 of 12 conditions, and the coarse positional family remained best.";
        }

        if (sameClosestDetector)
        {
            return $"The same-scope re-land on current main shifted slightly from the previously reported M5a2 result: `{bestOverall.AlternativeDetectorName}` still remained closest to `lzmsa-paper`, but it beat whole-stream mean in {bestOverall.CloserThanWholeStreamMeanConditionCount} of {conditionCount} conditions and the best family was `{bestFamily}`.";
        }

        return $"The same-scope re-land on current main changed materially from the previously reported unmerged M5a2 result: the closest tested simple neighbor was `{bestOverall.AlternativeDetectorName}` rather than `{PreviouslyReportedClosestDetector}`, it beat whole-stream mean in {bestOverall.CloserThanWholeStreamMeanConditionCount} of {conditionCount} conditions rather than {PreviouslyReportedBeatCount}, and the best family was `{bestFamily}` rather than `{PreviouslyReportedBestFamily}`.";
    }

    private static double GetAuc(
        IReadOnlyDictionary<(string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName), SummaryRecord> lookup,
        string taskName,
        double conditionSnrDb,
        int windowLength,
        string detectorName)
    {
        if (!lookup.TryGetValue((taskName, conditionSnrDb, windowLength, detectorName), out var summary) || !summary.Auc.HasValue)
        {
            throw new InvalidOperationException($"Missing AUC summary for task '{taskName}', snr '{conditionSnrDb}', window '{windowLength}', detector '{detectorName}'.");
        }

        return summary.Auc.Value;
    }

    private static double RoundDelta(double value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private sealed class ConditionDetectorKeyComparer : IEqualityComparer<(string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName)>
    {
        public bool Equals((string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName) x, (string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName) y)
        {
            return StringComparer.Ordinal.Equals(x.TaskName, y.TaskName)
                && x.ConditionSnrDb.Equals(y.ConditionSnrDb)
                && x.WindowLength == y.WindowLength
                && StringComparer.OrdinalIgnoreCase.Equals(x.DetectorName, y.DetectorName);
        }

        public int GetHashCode((string TaskName, double ConditionSnrDb, int WindowLength, string DetectorName) obj)
        {
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.TaskName),
                obj.ConditionSnrDb,
                obj.WindowLength,
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.DetectorName));
        }
    }

    private sealed record FindingsSummary(
        string MainStatement,
        string ClosestDetectorName,
        double ClosestMedianDelta,
        int ClosestBeatsMeanCount,
        string BestFamily,
        string Interpretation,
        string RelandComparisonNote);
}
