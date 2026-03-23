using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M5A1ScoreDecompositionReportBuilder
{
    public const string ArtifactPrefix = "m5a1";

    private static readonly string[] RequiredDetectorNames =
    [
        DetectorCatalog.LzmsaPaperDetectorName,
        DetectorCatalog.LzmsaCompressedLengthDetectorName,
        DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName,
        DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
    ];

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

    public static M5A1ComparisonReport Build(ExperimentConfig config, ExperimentResult result)
    {
        if (!IsEnabled(config) || result.Summary.Groups.Count == 0)
        {
            throw new InvalidOperationException("M5a1 comparison reporting requires an evaluation run containing the four compression-derived detector identities.");
        }

        var summaryByCondition = result.Summary.Groups
            .Where(summary => summary.TaskName is not null && summary.ConditionSnrDb.HasValue && summary.WindowLength.HasValue && summary.Auc.HasValue)
            .ToDictionary(
                summary => (summary.TaskName!, summary.ConditionSnrDb!.Value, summary.WindowLength!.Value, summary.DetectorName),
                summary => summary,
                new ConditionDetectorKeyComparer());

        var rows = new List<M5A1ComparisonRow>();

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
            var compressedLength = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaCompressedLengthDetectorName);
            var normalized = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName);
            var mean = GetAuc(summaryByCondition, key.TaskName, key.ConditionSnrDb, key.WindowLength, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName);

            rows.Add(new M5A1ComparisonRow(
                TaskName: key.TaskName,
                ConditionSnrDb: key.ConditionSnrDb,
                WindowLength: key.WindowLength,
                PaperAuc: paper,
                CompressedLengthAuc: compressedLength,
                NormalizedCompressedLengthAuc: normalized,
                MeanCompressedByteValueAuc: mean,
                PaperMinusCompressedLength: RoundDelta(paper - compressedLength),
                PaperMinusNormalizedCompressedLength: RoundDelta(paper - normalized),
                PaperMinusMeanCompressedByteValue: RoundDelta(paper - mean)));
        }

        var aggregateDeltaRows = BuildAggregateDeltaRows(rows);
        return new M5A1ComparisonReport(rows, aggregateDeltaRows, BuildFindingsMarkdown(config, result, rows, aggregateDeltaRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M5A1ComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskName,conditionSnrDb,windowLength,aucLzmsaPaper,aucLzmsaCompressedLength,aucLzmsaNormalizedCompressedLength,aucLzmsaMeanCompressedByteValue,deltaPaperMinusCompressedLength,deltaPaperMinusNormalizedCompressedLength,deltaPaperMinusMeanCompressedByteValue");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskName,
                row.ConditionSnrDb.ToString("F6", CultureInfo.InvariantCulture),
                row.WindowLength.ToString(CultureInfo.InvariantCulture),
                row.PaperAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.CompressedLengthAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.NormalizedCompressedLengthAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.MeanCompressedByteValueAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusCompressedLength.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusNormalizedCompressedLength.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusMeanCompressedByteValue.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteAggregateDeltaCsv(string path, IReadOnlyList<M5A1AggregateDeltaRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("alternativeDetectorName,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.AlternativeDetectorName,
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    private static IReadOnlyList<M5A1AggregateDeltaRow> BuildAggregateDeltaRows(IReadOnlyList<M5A1ComparisonRow> rows)
    {
        return
        [
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaCompressedLengthDetectorName, rows.Select(row => Math.Abs(row.PaperMinusCompressedLength)).ToArray()),
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName, rows.Select(row => Math.Abs(row.PaperMinusNormalizedCompressedLength)).ToArray()),
            BuildAggregateDeltaRow(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, rows.Select(row => Math.Abs(row.PaperMinusMeanCompressedByteValue)).ToArray()),
        ];
    }

    private static M5A1AggregateDeltaRow BuildAggregateDeltaRow(string detectorName, IReadOnlyList<double> deltas)
    {
        if (deltas.Count == 0)
        {
            return new M5A1AggregateDeltaRow(detectorName, 0d, 0d);
        }

        var ordered = deltas.OrderBy(value => value).ToArray();
        var median = ordered.Length % 2 == 0
            ? (ordered[(ordered.Length / 2) - 1] + ordered[ordered.Length / 2]) / 2d
            : ordered[ordered.Length / 2];

        return new M5A1AggregateDeltaRow(
            detectorName,
            RoundDelta(median),
            RoundDelta(ordered.Max()));
    }

    private static string BuildFindingsMarkdown(ExperimentConfig config, ExperimentResult result, IReadOnlyList<M5A1ComparisonRow> rows, IReadOnlyList<M5A1AggregateDeltaRow> aggregateDeltaRows)
    {
        var closeness = SummarizeCloseness(rows);
        var sb = new StringBuilder();
        sb.AppendLine("# M5a1 Compressed-Stream Decomposition Findings");
        sb.AppendLine();
        sb.AppendLine("## Scope");
        sb.AppendLine();
        sb.AppendLine($"- Tasks run: {string.Join(", ", config.Evaluation!.Tasks.Select(task => task.Name))}");
        sb.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        sb.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        sb.AppendLine($"- Trial count per condition and class: {config.Evaluation.TrialCountPerCondition}");
        sb.AppendLine($"- Detector identities compared: {string.Join(", ", RequiredDetectorNames)}");
        sb.AppendLine($"- Mean-byte-value orientation: {DetectorCatalog.GetScoreOrientation(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName)} (chosen explicitly because byte-sum = compressed-length × mean-byte-value, so higher mean byte value increases the paper-style byte-sum when the compressed-length factor is held fixed)");
        sb.AppendLine($"- Seed: {config.Seed}");
        sb.AppendLine($"- Config provenance: {config.ExperimentId} / {config.ExperimentName}");
        sb.AppendLine();
        sb.AppendLine("## Main Comparison Statement");
        sb.AppendLine();
        sb.AppendLine($"- Within the current synthetic benchmark, mean compressed byte value tracked the paper-style byte-sum score {closeness.MainComparisonPhrase}.");
        sb.AppendLine();
        sb.AppendLine("## Condition Summary");
        sb.AppendLine();
        sb.AppendLine($"- The mean-byte-value variant was closer to `lzmsa-paper` than raw compressed length in {closeness.PreferredConditionSummary}.");
        sb.AppendLine($"- `lzmsa-compressed-length` and `lzmsa-normalized-compressed-length` {closeness.LengthVariantSummary}.");
        sb.AppendLine();
        sb.AppendLine("## Comparison Table");
        sb.AppendLine();
        sb.AppendLine("| Task | SNR dB | Window | AUC paper | AUC compressed length | AUC normalized length | AUC mean byte value | Δ paper-length | Δ paper-normalized | Δ paper-mean |");
        sb.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var row in rows)
        {
            sb.AppendLine($"| {row.TaskName} | {row.ConditionSnrDb.ToString("0.###", CultureInfo.InvariantCulture)} | {row.WindowLength} | {row.PaperAuc:F6} | {row.CompressedLengthAuc:F6} | {row.NormalizedCompressedLengthAuc:F6} | {row.MeanCompressedByteValueAuc:F6} | {row.PaperMinusCompressedLength:F6} | {row.PaperMinusNormalizedCompressedLength:F6} | {row.PaperMinusMeanCompressedByteValue:F6} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Aggregate Delta Summary");
        sb.AppendLine();
        sb.AppendLine("| Alternative detector | Median | Max |");
        sb.AppendLine("| --- | ---: | ---: |");
        foreach (var row in aggregateDeltaRows)
        {
            sb.AppendLine($"| {row.AlternativeDetectorName} | {row.MedianAbsoluteAucDeltaFromPaper:F6} | {row.MaxAbsoluteAucDeltaFromPaper:F6} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Cautious Interpretation");
        sb.AppendLine();
        sb.AppendLine($"- {closeness.Interpretation}");
        sb.AppendLine();
        sb.AppendLine("## Caveats");
        sb.AppendLine();
        sb.AppendLine("- This artifact set is limited to the repository's current synthetic benchmark tasks and evaluation conditions.");
        sb.AppendLine("- The OFDM-like task is a structured synthetic proxy, not LTE fidelity or a standards-faithful waveform.");
        sb.AppendLine("- The current deterministic serialization + Brotli compression backend remains fixed.");
        sb.AppendLine("- No SDR capture, over-the-air, or hardware claims are supported by this artifact set.");
        sb.AppendLine("- This decomposition pass does not fully resolve mechanism; it only checks whether mean compressed byte value is a closer simple neighbor of byte-sum than length-based scoring is.");
        sb.AppendLine();
        sb.AppendLine("## Artifact Notes");
        sb.AppendLine();
        sb.AppendLine($"- Per-trial score rows: {result.Trials.Count}");
        sb.AppendLine($"- Per-condition summary rows: {result.Summary.Groups.Count}");
        sb.AppendLine($"- ROC point rows: {result.Evaluation?.RocPoints.Count ?? 0}");

        return sb.ToString();
    }

    private static ClosenessSummary SummarizeCloseness(IReadOnlyList<M5A1ComparisonRow> rows)
    {
        if (rows.Count == 0)
        {
            return new ClosenessSummary(
                "about the same as compressed length",
                "no tested conditions",
                "did not produce evaluable AUC rows",
                "This does not yet narrow the mechanism question because the requested comparison did not produce evaluable rows.");
        }

        var meanCloserCount = rows.Count(row => Math.Abs(row.PaperMinusMeanCompressedByteValue) < Math.Abs(row.PaperMinusCompressedLength));
        var sameCount = rows.Count(row => Math.Abs(row.PaperMinusMeanCompressedByteValue) == Math.Abs(row.PaperMinusCompressedLength));
        var lengthCloserCount = rows.Count - meanCloserCount - sameCount;

        var mainPhrase = meanCloserCount > lengthCloserCount
            ? "more closely than compressed length did"
            : meanCloserCount == lengthCloserCount
                ? "about the same as compressed length did"
                : "less closely than compressed length did";

        var conditionSummary = meanCloserCount == rows.Count
            ? "all conditions"
            : meanCloserCount >= Math.Ceiling(rows.Count / 2d)
                ? $"most conditions ({meanCloserCount} of {rows.Count})"
                : meanCloserCount == 0
                    ? "no conditions"
                    : $"only some conditions ({meanCloserCount} of {rows.Count})";

        var lengthGapMax = rows.Max(row => Math.Abs(row.CompressedLengthAuc - row.NormalizedCompressedLengthAuc));
        var lengthSummary = lengthGapMax == 0d
            ? "matched exactly in every tested condition"
            : $"stayed close but did not match exactly in every condition (maximum AUC gap {lengthGapMax:F6})";

        var interpretation = mainPhrase switch
        {
            "more closely than compressed length did" => "Within the current synthetic benchmark, mean compressed byte value tracked the paper-style byte-sum score more closely than compressed length did. This narrows the local mechanism question, but it does not fully resolve it.",
            "about the same as compressed length did" => "Within the current synthetic benchmark, mean compressed byte value tracked the paper-style byte-sum score about the same as compressed length did. This does not yet narrow the mechanism question much beyond M4/M4a.",
            _ => "Within the current synthetic benchmark, byte-sum remained distinct even after separating out mean compressed byte value. This does not yet narrow the mechanism question.",
        };

        return new ClosenessSummary(mainPhrase, conditionSummary, lengthSummary, interpretation);
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

    private sealed record ClosenessSummary(
        string MainComparisonPhrase,
        string PreferredConditionSummary,
        string LengthVariantSummary,
        string Interpretation);
}
