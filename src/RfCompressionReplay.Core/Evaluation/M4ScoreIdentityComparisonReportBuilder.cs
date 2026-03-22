using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M4ScoreIdentityComparisonReportBuilder
{
    public static string GetArtifactPrefix(ExperimentConfig config) => GetMilestoneDescriptor(config).ArtifactPrefix;

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

    public static M4ComparisonReport Build(ExperimentConfig config, ExperimentResult result)
    {
        if (!IsEnabled(config) || result.Summary.Groups.Count == 0)
        {
            throw new InvalidOperationException("M4 comparison reporting requires an evaluation run containing the three compression-derived detector identities.");
        }

        var summaryByCondition = result.Summary.Groups
            .Where(summary => summary.TaskName is not null && summary.ConditionSnrDb.HasValue && summary.WindowLength.HasValue && summary.Auc.HasValue)
            .ToDictionary(
                summary => (summary.TaskName!, summary.ConditionSnrDb!.Value, summary.WindowLength!.Value, summary.DetectorName),
                summary => summary,
                new ConditionDetectorKeyComparer());

        var rows = new List<M4ComparisonRow>();

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

            rows.Add(new M4ComparisonRow(
                TaskName: key.TaskName,
                ConditionSnrDb: key.ConditionSnrDb,
                WindowLength: key.WindowLength,
                PaperAuc: paper,
                CompressedLengthAuc: compressedLength,
                NormalizedCompressedLengthAuc: normalized,
                PaperMinusCompressedLength: RoundDelta(paper - compressedLength),
                PaperMinusNormalizedCompressedLength: RoundDelta(paper - normalized),
                CompressedLengthMinusNormalizedCompressedLength: RoundDelta(compressedLength - normalized)));
        }

        return new M4ComparisonReport(rows, BuildFindingsMarkdown(config, result, rows));
    }

    public static void WriteCsv(string path, IReadOnlyList<M4ComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("taskName,conditionSnrDb,windowLength,aucLzmsaPaper,aucLzmsaCompressedLength,aucLzmsaNormalizedCompressedLength,deltaPaperMinusCompressedLength,deltaPaperMinusNormalizedCompressedLength,deltaCompressedLengthMinusNormalizedCompressedLength");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.TaskName,
                row.ConditionSnrDb.ToString("F6", CultureInfo.InvariantCulture),
                row.WindowLength.ToString(CultureInfo.InvariantCulture),
                row.PaperAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.CompressedLengthAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.NormalizedCompressedLengthAuc.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusCompressedLength.ToString("F6", CultureInfo.InvariantCulture),
                row.PaperMinusNormalizedCompressedLength.ToString("F6", CultureInfo.InvariantCulture),
                row.CompressedLengthMinusNormalizedCompressedLength.ToString("F6", CultureInfo.InvariantCulture)));
        }
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

    private static string BuildFindingsMarkdown(ExperimentConfig config, ExperimentResult result, IReadOnlyList<M4ComparisonRow> rows)
    {
        var descriptor = GetMilestoneDescriptor(config);
        var stability = SummarizeStability(rows);
        var sb = new StringBuilder();
        sb.AppendLine($"# {descriptor.HeadingPrefix} Score-Identity Comparison Findings");
        sb.AppendLine();
        sb.AppendLine("## Scope");
        sb.AppendLine();
        sb.AppendLine($"- Tasks run: {string.Join(", ", config.Evaluation!.Tasks.Select(task => task.Name))}");
        sb.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        sb.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        sb.AppendLine($"- Trial count per condition and class: {config.Evaluation.TrialCountPerCondition}");
        sb.AppendLine($"- Detector identities compared: {DetectorCatalog.LzmsaPaperDetectorName}, {DetectorCatalog.LzmsaCompressedLengthDetectorName}, {DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName}");
        sb.AppendLine($"- Seed: {config.Seed}");
        sb.AppendLine($"- Config provenance: {config.ExperimentId} / {config.ExperimentName}");
        sb.AppendLine();
        sb.AppendLine("## Stability Summary");
        sb.AppendLine();
        sb.AppendLine($"- `{DetectorCatalog.LzmsaPaperDetectorName}` had the highest AUC in {stability.PaperRankingSummary}.");
        sb.AppendLine($"- `{DetectorCatalog.LzmsaCompressedLengthDetectorName}` and `{DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName}` {stability.LengthVariantSummary}.");
        sb.AppendLine($"- The largest paper-vs-length-based AUC gap was {stability.LargestGap:F6} ({stability.LargestGapQualitative}), and the median paper-vs-length-based AUC gap was {stability.MedianGap:F6} ({stability.MedianGapQualitative}).");
        sb.AppendLine();
        sb.AppendLine("## Comparison Table");
        sb.AppendLine();
        sb.AppendLine("| Task | SNR dB | Window | AUC paper | AUC compressed length | AUC normalized length | Δ paper-length | Δ paper-normalized | Δ length-normalized |");
        sb.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var row in rows)
        {
            sb.AppendLine($"| {row.TaskName} | {row.ConditionSnrDb.ToString("0.###", CultureInfo.InvariantCulture)} | {row.WindowLength} | {row.PaperAuc:F6} | {row.CompressedLengthAuc:F6} | {row.NormalizedCompressedLengthAuc:F6} | {row.PaperMinusCompressedLength:F6} | {row.PaperMinusNormalizedCompressedLength:F6} | {row.CompressedLengthMinusNormalizedCompressedLength:F6} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Cautious Conclusion");
        sb.AppendLine();
        sb.AppendLine($"- {BuildConclusion(descriptor.HeadingPrefix, stability)}");
        sb.AppendLine();
        sb.AppendLine("## Supporting Notes");
        sb.AppendLine();
        foreach (var bullet in BuildFindingsBullets(rows, descriptor.HeadingPrefix))
        {
            sb.AppendLine($"- {bullet}");
        }

        sb.AppendLine();
        sb.AppendLine("## Caveats");
        sb.AppendLine();
        sb.AppendLine($"- This {descriptor.HeadingPrefix} comparison is limited to the repository's synthetic benchmark tasks and conditions.");
        sb.AppendLine("- The OFDM-like task is a structured synthetic proxy, not LTE fidelity or a standards-faithful waveform.");
        sb.AppendLine("- The current deterministic serialization + Brotli compression backend remains fixed; M4 only varies score identity on top of that path.");
        sb.AppendLine("- No SDR capture, over-the-air, or hardware claims are supported by this artifact set.");
        sb.AppendLine();
        sb.AppendLine("## Artifact Notes");
        sb.AppendLine();
        sb.AppendLine($"- Per-trial score rows: {result.Trials.Count}");
        sb.AppendLine($"- Per-condition summary rows: {result.Summary.Groups.Count}");
        sb.AppendLine($"- ROC point rows: {result.Evaluation?.RocPoints.Count ?? 0}");

        return sb.ToString();
    }

    private static IReadOnlyList<string> BuildFindingsBullets(IReadOnlyList<M4ComparisonRow> rows, string headingPrefix)
    {
        var bullets = new List<string>();
        var compressedVsNormalizedWorstGap = rows.Count == 0
            ? 0d
            : rows.Max(row => Math.Abs(row.CompressedLengthMinusNormalizedCompressedLength));
        var allPairwiseMaxDelta = rows
            .Select(row => new[]
            {
                Math.Abs(row.PaperMinusCompressedLength),
                Math.Abs(row.PaperMinusNormalizedCompressedLength),
                Math.Abs(row.CompressedLengthMinusNormalizedCompressedLength),
            }.Max())
            .ToArray();

        var overallMaxDelta = allPairwiseMaxDelta.Length == 0 ? 0d : allPairwiseMaxDelta.Max();
        var overallAverageMaxDelta = allPairwiseMaxDelta.Length == 0 ? 0d : allPairwiseMaxDelta.Average();
        bullets.Add($"Across {rows.Count} task/SNR/window conditions, the maximum pairwise AUC gap among the three compression-derived score identities was {overallMaxDelta:F6}, with an average per-condition worst-case gap of {overallAverageMaxDelta:F6}.");

        if (compressedVsNormalizedWorstGap == 0d)
        {
            bullets.Add("Across the tested synthetic sweep, `lzmsa-compressed-length` and `lzmsa-normalized-compressed-length` produced identical AUCs in every condition, so the observed mechanism split is between byte-sum scoring and compression-length-based scoring rather than between the two length-based variants.");
        }

        var paperBetterCount = rows.Count(row => row.PaperAuc > row.CompressedLengthAuc && row.PaperAuc > row.NormalizedCompressedLengthAuc);
        if (paperBetterCount == rows.Count && rows.Count > 0)
        {
            bullets.Add($"Within this synthetic benchmark, `{DetectorCatalog.LzmsaPaperDetectorName}` achieved the highest AUC in every tested condition rather than tracking closely with the length-based score identities.");
        }

        foreach (var taskGroup in rows.GroupBy(row => row.TaskName).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var worstRow = taskGroup
                .Select(row => new
                {
                    Row = row,
                    WorstGap = new[]
                    {
                        Math.Abs(row.PaperMinusCompressedLength),
                        Math.Abs(row.PaperMinusNormalizedCompressedLength),
                        Math.Abs(row.CompressedLengthMinusNormalizedCompressedLength),
                    }.Max(),
                })
                .OrderByDescending(item => item.WorstGap)
                .ThenBy(item => item.Row.ConditionSnrDb)
                .ThenBy(item => item.Row.WindowLength)
                .First();

            var averageWorstGap = taskGroup
                .Select(row => new[]
                {
                    Math.Abs(row.PaperMinusCompressedLength),
                    Math.Abs(row.PaperMinusNormalizedCompressedLength),
                    Math.Abs(row.CompressedLengthMinusNormalizedCompressedLength),
                }.Max())
                .Average();

            bullets.Add($"On {taskGroup.Key}, the average worst-case per-condition AUC gap was {averageWorstGap:F6}; the largest gap appeared at SNR {worstRow.Row.ConditionSnrDb.ToString("0.###", CultureInfo.InvariantCulture)} dB and window length {worstRow.Row.WindowLength}, where the worst pairwise difference reached {worstRow.WorstGap:F6}.");
        }

        var materiallyDivergentRows = rows
            .Where(row => new[]
            {
                Math.Abs(row.PaperMinusCompressedLength),
                Math.Abs(row.PaperMinusNormalizedCompressedLength),
                Math.Abs(row.CompressedLengthMinusNormalizedCompressedLength),
            }.Max() >= 0.05d)
            .OrderBy(row => row.TaskName, StringComparer.Ordinal)
            .ThenBy(row => row.ConditionSnrDb)
            .ThenBy(row => row.WindowLength)
            .ToArray();

        if (materiallyDivergentRows.Length == 0)
        {
            bullets.Add("Within this synthetic benchmark sweep, the compression-derived detection effect appears robust to score substitution at the tested resolution: no condition produced a pairwise AUC gap of 0.05 or larger.");
        }
        else
        {
            var examples = string.Join("; ", materiallyDivergentRows.Take(3).Select(row => $"{row.TaskName} @ {row.ConditionSnrDb.ToString("0.###", CultureInfo.InvariantCulture)} dB / window {row.WindowLength} (worst gap {new[] { Math.Abs(row.PaperMinusCompressedLength), Math.Abs(row.PaperMinusNormalizedCompressedLength), Math.Abs(row.CompressedLengthMinusNormalizedCompressedLength) }.Max():F6})"));
            bullets.Add($"Some synthetic conditions diverged materially under score substitution (threshold 0.05 AUC): {examples}.");
        }

        bullets.Add($"{headingPrefix} stays within the same scientific question as M4: same synthetic tasks, same serialization/compression path, same score formulas, and same ROC/AUC method.");

        return bullets;
    }

    private static string BuildConclusion(string headingPrefix, StabilitySummary stability)
    {
        if (stability.PaperHighestInAllConditions && stability.LengthVariantsNearlyMatch)
        {
            return $"The M4 ranking remained stable under the stronger {headingPrefix} rerun: `lzmsa-paper` stayed on top throughout the tested synthetic matrix, while the two length-based variants remained interchangeable within the fixed-window conditions exercised here.";
        }

        if (!stability.PaperHighestInMostConditions)
        {
            return $"The original M4 ranking weakened under the stronger {headingPrefix} rerun, so the score-identity conclusion should be treated as less stable than the initial pass suggested.";
        }

        return $"The original M4 ranking was only partially stable under the stronger {headingPrefix} rerun: `lzmsa-paper` remained strongest in most, but not all, tested conditions, while the length-based variants stayed close to one another.";
    }

    private static StabilitySummary SummarizeStability(IReadOnlyList<M4ComparisonRow> rows)
    {
        if (rows.Count == 0)
        {
            return new StabilitySummary("no conditions", "did not produce evaluable AUC rows", 0d, "small", 0d, "small", false, false, false, false);
        }

        var paperHighestCount = rows.Count(row => row.PaperAuc > row.CompressedLengthAuc && row.PaperAuc > row.NormalizedCompressedLengthAuc);
        var paperRankingSummary = paperHighestCount == rows.Count
            ? "all tested conditions"
            : paperHighestCount >= Math.Ceiling(rows.Count / 2d)
                ? $"most tested conditions ({paperHighestCount} of {rows.Count})"
                : $"only some tested conditions ({paperHighestCount} of {rows.Count})";

        var lengthGaps = rows.Select(row => Math.Abs(row.CompressedLengthMinusNormalizedCompressedLength)).ToArray();
        var maxLengthGap = lengthGaps.Max();
        var lengthVariantSummary = maxLengthGap == 0d
            ? "matched exactly in every tested condition"
            : maxLengthGap <= 0.01d
                ? $"nearly matched throughout the tested conditions (maximum AUC gap {maxLengthGap:F6})"
                : $"diverged in at least some tested conditions (maximum AUC gap {maxLengthGap:F6})";

        var paperVsLengthGaps = rows
            .SelectMany(row => new[]
            {
                Math.Abs(row.PaperMinusCompressedLength),
                Math.Abs(row.PaperMinusNormalizedCompressedLength),
            })
            .OrderBy(value => value)
            .ToArray();

        var largestGap = paperVsLengthGaps.Max();
        var medianGap = paperVsLengthGaps[paperVsLengthGaps.Length / 2];

        return new StabilitySummary(
            paperRankingSummary,
            lengthVariantSummary,
            largestGap,
            DescribeGap(largestGap),
            medianGap,
            DescribeGap(medianGap),
            paperHighestCount == rows.Count,
            paperHighestCount >= Math.Ceiling(rows.Count / 2d),
            maxLengthGap <= 0.01d,
            maxLengthGap == 0d);
    }

    private static string DescribeGap(double value)
    {
        return value switch
        {
            >= 0.1d => "large",
            >= 0.03d => "modest",
            _ => "small",
        };
    }

    private static MilestoneDescriptor GetMilestoneDescriptor(ExperimentConfig config)
    {
        var milestone = config.ManifestMetadata.Tags is not null
            && config.ManifestMetadata.Tags.TryGetValue("milestone", out var taggedMilestone)
            ? taggedMilestone
            : config.ManifestMetadata.VersionTag;

        if (milestone.StartsWith("m4a", StringComparison.OrdinalIgnoreCase))
        {
            return new MilestoneDescriptor("m4a", "M4a");
        }

        return new MilestoneDescriptor("m4", "M4");
    }

    private static double RoundDelta(double value) => Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static readonly HashSet<string> RequiredDetectorNames =
    [
        DetectorCatalog.LzmsaPaperDetectorName,
        DetectorCatalog.LzmsaCompressedLengthDetectorName,
        DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName,
    ];

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

    private sealed record StabilitySummary(
        string PaperRankingSummary,
        string LengthVariantSummary,
        double LargestGap,
        string LargestGapQualitative,
        double MedianGap,
        string MedianGapQualitative,
        bool PaperHighestInAllConditions,
        bool PaperHighestInMostConditions,
        bool LengthVariantsNearlyMatch,
        bool LengthVariantsExactlyMatch);

    private sealed record MilestoneDescriptor(string ArtifactPrefix, string HeadingPrefix);
}
