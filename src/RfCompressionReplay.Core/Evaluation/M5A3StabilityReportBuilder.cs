using System.Globalization;
using System.Text;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Evaluation;

public static class M5A3StabilityReportBuilder
{
    public const string ArtifactPrefix = "m5a3";

    private const string WholeStreamFamily = "whole-stream";
    private const string HistogramFamily = "coarse-histogram";
    private const string PositionalFamily = "coarse-positional";

    private static readonly DetectorDescriptor[] AlternativeDetectors =
    [
        new(DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, WholeStreamFamily),
        new(DetectorCatalog.LzmsaCompressedByteVarianceDetectorName, WholeStreamFamily),
        new(DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorName, HistogramFamily),
        new(DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, HistogramFamily),
        new(DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorName, HistogramFamily),
        new(DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorName, HistogramFamily),
        new(DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorName, PositionalFamily),
        new(DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, PositionalFamily),
    ];

    public static M5A3StabilityArtifacts Build(M5A3StabilityConfig config, IReadOnlyList<(int Seed, ExperimentResult Result)> seedResults)
    {
        if (seedResults.Count == 0)
        {
            throw new InvalidOperationException("M5a3 stability reporting requires at least one seeded result.");
        }

        var comparisonRows = seedResults
            .SelectMany(seedResult => BuildRowsForSeed(config, seedResult.Seed, seedResult.Result))
            .OrderBy(row => row.Seed)
            .ThenBy(row => row.TaskName, StringComparer.Ordinal)
            .ThenBy(row => row.ConditionSnrDb)
            .ThenBy(row => row.WindowLength)
            .ToArray();

        var perFeatureStats = AlternativeDetectors
            .Select(detector => BuildFeatureStats(comparisonRows, detector))
            .OrderBy(stat => stat.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(stat => stat.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(stat => stat.AlternativeDetectorName, StringComparer.Ordinal)
            .ToArray();

        var deltaSummaryRows = perFeatureStats
            .Select(stat => new M5A3DeltaSummaryRow(
                stat.AlternativeDetectorName,
                stat.FeatureFamily,
                stat.MedianAbsoluteAucDeltaFromPaper,
                stat.MaxAbsoluteAucDeltaFromPaper))
            .ToArray();

        var stabilitySummaryRows = perFeatureStats
            .Select(stat => new M5A3StabilitySummaryRow(
                stat.AlternativeDetectorName,
                stat.FeatureFamily,
                stat.ClosestNeighborCount,
                stat.MedianAbsoluteAucDeltaFromPaper,
                stat.MaxAbsoluteAucDeltaFromPaper,
                stat.MedianClosenessRank))
            .ToArray();

        return new M5A3StabilityArtifacts(
            comparisonRows,
            deltaSummaryRows,
            stabilitySummaryRows,
            BuildFindingsMarkdown(config, comparisonRows, stabilitySummaryRows));
    }

    public static void WriteComparisonCsv(string path, IReadOnlyList<M5A3AucComparisonRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("seed,taskName,conditionSnrDb,windowLength,aucLzmsaPaper,aucLzmsaMeanCompressedByteValue,aucLzmsaCompressedByteVariance,aucLzmsaCompressedByteBucket0To63Proportion,aucLzmsaCompressedByteBucket64To127Proportion,aucLzmsaCompressedByteBucket128To191Proportion,aucLzmsaCompressedByteBucket192To255Proportion,aucLzmsaPrefixThirdMeanCompressedByteValue,aucLzmsaSuffixThirdMeanCompressedByteValue,absoluteDeltaFromPaperMeanCompressedByteValue,absoluteDeltaFromPaperCompressedByteVariance,absoluteDeltaFromPaperBucket0To63Proportion,absoluteDeltaFromPaperBucket64To127Proportion,absoluteDeltaFromPaperBucket128To191Proportion,absoluteDeltaFromPaperBucket192To255Proportion,absoluteDeltaFromPaperPrefixThirdMeanCompressedByteValue,absoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.Seed.ToString(CultureInfo.InvariantCulture),
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
                row.AbsoluteDeltaFromPaperMeanCompressedByteValue.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperCompressedByteVariance.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperBucket0To63Proportion.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperBucket64To127Proportion.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperBucket128To191Proportion.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperBucket192To255Proportion.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperPrefixThirdMeanCompressedByteValue.ToString("F6", CultureInfo.InvariantCulture),
                row.AbsoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteDeltaSummaryCsv(string path, IReadOnlyList<M5A3DeltaSummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("alternativeDetectorName,featureFamily,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.AlternativeDetectorName,
                row.FeatureFamily,
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    public static void WriteStabilitySummaryCsv(string path, IReadOnlyList<M5A3StabilitySummaryRow> rows)
    {
        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine("alternativeDetectorName,featureFamily,closestNeighborCount,medianAbsoluteAucDeltaFromPaper,maxAbsoluteAucDeltaFromPaper,medianClosenessRank");

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',',
                row.AlternativeDetectorName,
                row.FeatureFamily,
                row.ClosestNeighborCount.ToString(CultureInfo.InvariantCulture),
                row.MedianAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MaxAbsoluteAucDeltaFromPaper.ToString("F6", CultureInfo.InvariantCulture),
                row.MedianClosenessRank.ToString("F6", CultureInfo.InvariantCulture)));
        }
    }

    private static IReadOnlyList<M5A3AucComparisonRow> BuildRowsForSeed(M5A3StabilityConfig config, int seed, ExperimentResult result)
    {
        var report = M5A2ScoreDecompositionReportBuilder.Build(config.ToSeededExperimentConfig(seed), result);

        return report.Rows.Select(row => new M5A3AucComparisonRow(
            seed,
            row.TaskName,
            row.ConditionSnrDb,
            row.WindowLength,
            row.PaperAuc,
            row.MeanCompressedByteValueAuc,
            row.CompressedByteVarianceAuc,
            row.Bucket0To63ProportionAuc,
            row.Bucket64To127ProportionAuc,
            row.Bucket128To191ProportionAuc,
            row.Bucket192To255ProportionAuc,
            row.PrefixThirdMeanCompressedByteValueAuc,
            row.SuffixThirdMeanCompressedByteValueAuc,
            Math.Abs(row.PaperMinusMeanCompressedByteValue),
            Math.Abs(row.PaperMinusCompressedByteVariance),
            Math.Abs(row.PaperMinusBucket0To63Proportion),
            Math.Abs(row.PaperMinusBucket64To127Proportion),
            Math.Abs(row.PaperMinusBucket128To191Proportion),
            Math.Abs(row.PaperMinusBucket192To255Proportion),
            Math.Abs(row.PaperMinusPrefixThirdMeanCompressedByteValue),
            Math.Abs(row.PaperMinusSuffixThirdMeanCompressedByteValue))).ToArray();
    }

    private static FeatureStats BuildFeatureStats(IReadOnlyList<M5A3AucComparisonRow> rows, DetectorDescriptor descriptor)
    {
        var deltas = rows.Select(row => GetDelta(row, descriptor.DetectorName)).ToArray();
        var ranks = rows.Select(row => GetRank(row, descriptor.DetectorName)).ToArray();
        var closestCount = rows.Count(row =>
        {
            var minDelta = AlternativeDetectors.Min(detector => GetDelta(row, detector.DetectorName));
            return NearlyEqual(GetDelta(row, descriptor.DetectorName), minDelta);
        });

        return new FeatureStats(
            descriptor.DetectorName,
            descriptor.FeatureFamily,
            closestCount,
            Round(Median(deltas)),
            Round(deltas.Max()),
            Round(Median(ranks)));
    }

    private static double GetDelta(M5A3AucComparisonRow row, string detectorName)
    {
        return detectorName switch
        {
            var value when string.Equals(value, DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperMeanCompressedByteValue,
            var value when string.Equals(value, DetectorCatalog.LzmsaCompressedByteVarianceDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperCompressedByteVariance,
            var value when string.Equals(value, DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperBucket0To63Proportion,
            var value when string.Equals(value, DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperBucket64To127Proportion,
            var value when string.Equals(value, DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperBucket128To191Proportion,
            var value when string.Equals(value, DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperBucket192To255Proportion,
            var value when string.Equals(value, DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperPrefixThirdMeanCompressedByteValue,
            var value when string.Equals(value, DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName, StringComparison.Ordinal) => row.AbsoluteDeltaFromPaperSuffixThirdMeanCompressedByteValue,
            _ => throw new InvalidOperationException($"Unsupported M5a3 detector '{detectorName}'."),
        };
    }

    private static double GetRank(M5A3AucComparisonRow row, string detectorName)
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

    private static string BuildFindingsMarkdown(M5A3StabilityConfig config, IReadOnlyList<M5A3AucComparisonRow> comparisonRows, IReadOnlyList<M5A3StabilitySummaryRow> stabilityRows)
    {
        var bestFeature = stabilityRows
            .OrderByDescending(row => row.ClosestNeighborCount)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MedianClosenessRank)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();

        var runnerUp = stabilityRows
            .Where(row => !string.Equals(row.AlternativeDetectorName, bestFeature.AlternativeDetectorName, StringComparison.Ordinal))
            .OrderByDescending(row => row.ClosestNeighborCount)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MedianClosenessRank)
            .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal)
            .First();

        var familyLeader = stabilityRows
            .GroupBy(row => row.FeatureFamily, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(row => row.ClosestNeighborCount)
                .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
                .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
                .ThenBy(row => row.MedianClosenessRank)
                .First())
            .OrderByDescending(row => row.ClosestNeighborCount)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.FeatureFamily, StringComparer.Ordinal)
            .First();

        var totalCombinations = comparisonRows.Count;
        var topContenders = stabilityRows
            .OrderByDescending(row => row.ClosestNeighborCount)
            .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
            .ThenBy(row => row.MedianClosenessRank)
            .Take(4)
            .ToArray();
        var contenderFamilies = topContenders
            .Select(row => row.FeatureFamily)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var stableSingleWinner = bestFeature.ClosestNeighborCount > totalCombinations / 2
            && bestFeature.ClosestNeighborCount >= runnerUp.ClosestNeighborCount + 4;
        var nearTied = !stableSingleWinner
            && bestFeature.ClosestNeighborCount <= runnerUp.ClosestNeighborCount + 3
            && Math.Abs(bestFeature.MedianAbsoluteAucDeltaFromPaper - runnerUp.MedianAbsoluteAucDeltaFromPaper) <= 0.01d;

        var stabilityStatement = stableSingleWinner
            ? $"`{bestFeature.AlternativeDetectorName}` was the closest simple neighbor in {bestFeature.ClosestNeighborCount} of {totalCombinations} seed-condition combinations, giving this run a cautious single-feature winner."
            : nearTied
                ? $"The top simple features were near-tied across the {totalCombinations} seed-condition combinations; `{bestFeature.AlternativeDetectorName}` led with {bestFeature.ClosestNeighborCount} closest-neighbor wins, but `{runnerUp.AlternativeDetectorName}` stayed close at {runnerUp.ClosestNeighborCount}."
                : $"`{bestFeature.AlternativeDetectorName}` led the current feature set, but not by enough to treat the winner as stable across the {totalCombinations} seed-condition combinations.";

        var mainConclusion = stableSingleWinner
            ? $"Within the current synthetic benchmark, `{bestFeature.AlternativeDetectorName}` remained the most stable closest simple neighbor to `lzmsa-paper` across the tested seeds and conditions."
            : contenderFamilies.Length == 1
                ? $"Within the current synthetic benchmark, no single M5a2 feature was a clearly stable winner, but the leading candidates stayed within the `{familyLeader.FeatureFamily}` family."
                : $"Within the current synthetic benchmark, no single M5a2 feature was a clearly stable winner; the nearest-neighbor set remained split across {string.Join(", ", contenderFamilies)} summaries rather than collapsing to one feature or one family.";

        var sb = new StringBuilder();
        sb.AppendLine("# M5a3 Stability Confirmation Findings");
        sb.AppendLine();
        sb.AppendLine("## Scope");
        sb.AppendLine();
        sb.AppendLine($"- Tasks run: {string.Join(", ", config.Evaluation.Tasks.Select(task => task.Name))}");
        sb.AppendLine($"- SNR values (dB): {string.Join(", ", config.Evaluation.SnrDbValues.Select(value => value.ToString("0.###", CultureInfo.InvariantCulture)))}");
        sb.AppendLine($"- Window lengths: {string.Join(", ", config.Evaluation.WindowLengths)}");
        sb.AppendLine($"- Trial count per condition and class: {config.Evaluation.TrialCountPerCondition}");
        sb.AppendLine($"- Seeds used: {string.Join(", ", config.SeedPanel)}");
        sb.AppendLine($"- Included features: {DetectorCatalog.LzmsaPaperDetectorName}, {string.Join(", ", AlternativeDetectors.Select(detector => detector.DetectorName))}");
        sb.AppendLine($"- Config provenance: {config.ExperimentId} / {config.ExperimentName}");
        sb.AppendLine();
        sb.AppendLine("## Stability Summary");
        sb.AppendLine();
        sb.AppendLine($"- {stabilityStatement}");
        sb.AppendLine($"- `{bestFeature.AlternativeDetectorName}` had median absolute AUC delta {bestFeature.MedianAbsoluteAucDeltaFromPaper:F6}, max absolute AUC delta {bestFeature.MaxAbsoluteAucDeltaFromPaper:F6}, and median closeness rank {bestFeature.MedianClosenessRank:F6}.");
        sb.AppendLine($"- The leading family by best-member stability metrics was `{familyLeader.FeatureFamily}`, but the strongest contenders {(contenderFamilies.Length == 1 ? "stayed within one family." : $"spanned {string.Join(", ", contenderFamilies)}.")}");
        sb.AppendLine();
        sb.AppendLine("## Main Conclusion");
        sb.AppendLine();
        sb.AppendLine($"- {mainConclusion}");
        sb.AppendLine();
        sb.AppendLine("## Stability Table");
        sb.AppendLine();
        sb.AppendLine("| Alternative detector | Family | Closest-neighbor wins | Median | Max | Median rank |");
        sb.AppendLine("| --- | --- | ---: | ---: | ---: | ---: |");
        foreach (var row in stabilityRows
                     .OrderByDescending(row => row.ClosestNeighborCount)
                     .ThenBy(row => row.MedianAbsoluteAucDeltaFromPaper)
                     .ThenBy(row => row.MaxAbsoluteAucDeltaFromPaper)
                     .ThenBy(row => row.MedianClosenessRank)
                     .ThenBy(row => row.AlternativeDetectorName, StringComparer.Ordinal))
        {
            sb.AppendLine($"| {row.AlternativeDetectorName} | {row.FeatureFamily} | {row.ClosestNeighborCount} | {row.MedianAbsoluteAucDeltaFromPaper:F6} | {row.MaxAbsoluteAucDeltaFromPaper:F6} | {row.MedianClosenessRank:F6} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Caveats");
        sb.AppendLine();
        sb.AppendLine("- This remains a synthetic-only benchmark; it does not establish broader real-world sensing behavior.");
        sb.AppendLine("- The OFDM-like task is a structured synthetic proxy and is not LTE fidelity.");
        sb.AppendLine("- The current deterministic serialization + Brotli compression backend caveat remains unchanged.");
        sb.AppendLine("- No SDR capture, OTA, or hardware claims are supported by this artifact set.");
        sb.AppendLine("- The bytestream mechanism is still not fully resolved; this milestone only checks winner stability within the current M5a2 feature family.");

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

    private sealed record FeatureStats(
        string AlternativeDetectorName,
        string FeatureFamily,
        int ClosestNeighborCount,
        double MedianAbsoluteAucDeltaFromPaper,
        double MaxAbsoluteAucDeltaFromPaper,
        double MedianClosenessRank);
}
