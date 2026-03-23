using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Models;
using RfCompressionReplay.Core.Signals;

namespace RfCompressionReplay.Core.Evaluation;

public static class M7BChangePointAnalyzer
{
    public static IReadOnlyList<double> ComputeWindowScores(
        double[] stream,
        int windowLength,
        int stride,
        IDetector detector,
        DetectorConfig detectorConfig)
    {
        if (windowLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowLength));
        }

        if (stride <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stride));
        }

        var starts = GetWindowStarts(stream.Length, windowLength, stride);
        var scores = new double[starts.Count];

        for (var index = 0; index < starts.Count; index++)
        {
            var samples = new double[windowLength];
            Array.Copy(stream, starts[index], samples, 0, windowLength);
            var result = detector.Evaluate(
                new DetectorInput(index, [new SignalWindow(index, starts[index], samples)]),
                detectorConfig);
            scores[index] = result.Score;
        }

        return scores;
    }

    public static IReadOnlyList<double> ComputeAdjacentChangeTrace(IReadOnlyList<double> scoreTrace)
    {
        if (scoreTrace.Count < 2)
        {
            return Array.Empty<double>();
        }

        var changes = new double[scoreTrace.Count - 1];
        for (var index = 0; index < changes.Length; index++)
        {
            changes[index] = Math.Abs(scoreTrace[index + 1] - scoreTrace[index]);
        }

        return changes;
    }

    public static IReadOnlyList<double> NormalizeTraceMinMax(IReadOnlyList<double> trace)
    {
        if (trace.Count == 0)
        {
            return Array.Empty<double>();
        }

        var min = trace.Min();
        var max = trace.Max();
        if (max - min <= 1e-12d)
        {
            return Enumerable.Repeat(0d, trace.Count).ToArray();
        }

        var normalized = new double[trace.Count];
        for (var index = 0; index < trace.Count; index++)
        {
            normalized[index] = (trace[index] - min) / (max - min);
        }

        return normalized;
    }

    public static IReadOnlyList<double> ComputeNormalizedAverageFusionChangeTrace(IReadOnlyList<IReadOnlyList<double>> traces)
    {
        if (traces.Count == 0)
        {
            return Array.Empty<double>();
        }

        var length = traces[0].Count;
        if (traces.Any(trace => trace.Count != length))
        {
            throw new InvalidOperationException("All fusion source traces must have the same length.");
        }

        var normalizedTraces = traces.Select(NormalizeTraceMinMax).ToArray();
        var fused = new double[length];
        for (var index = 0; index < length; index++)
        {
            fused[index] = normalizedTraces.Average(trace => trace[index]);
        }

        return fused;
    }

    public static IReadOnlyList<int> ProposeBoundaries(
        IReadOnlyList<double> scoreTrace,
        int streamLength,
        int windowLength,
        int stride,
        double peakThresholdMadMultiplier,
        int minPeakSpacing,
        int maxBoundaryProposals)
    {
        return ProposeBoundariesFromChangeTrace(
            ComputeAdjacentChangeTrace(scoreTrace),
            streamLength,
            windowLength,
            stride,
            peakThresholdMadMultiplier,
            minPeakSpacing,
            maxBoundaryProposals);
    }

    public static IReadOnlyList<int> ProposeBoundariesFromChangeTrace(
        IReadOnlyList<double> changes,
        int streamLength,
        int windowLength,
        int stride,
        double peakThresholdMadMultiplier,
        int minPeakSpacing,
        int maxBoundaryProposals)
    {
        if (changes.Count == 0)
        {
            return Array.Empty<int>();
        }

        var starts = GetWindowStarts(streamLength, windowLength, stride);
        var positions = new int[changes.Count];
        for (var index = 0; index < changes.Count; index++)
        {
            positions[index] = Math.Min(streamLength - 1, starts[index] + stride);
        }

        var threshold = CalculateRobustThreshold(changes, peakThresholdMadMultiplier);
        var localPeaks = new List<(int Position, double Magnitude)>();
        for (var index = 0; index < changes.Count; index++)
        {
            var current = changes[index];
            var left = index == 0 ? double.NegativeInfinity : changes[index - 1];
            var right = index == changes.Count - 1 ? double.NegativeInfinity : changes[index + 1];
            if (current < threshold)
            {
                continue;
            }

            if (current >= left && current >= right)
            {
                localPeaks.Add((positions[index], current));
            }
        }

        var accepted = new List<(int Position, double Magnitude)>();
        foreach (var candidate in localPeaks
                     .OrderByDescending(item => item.Magnitude)
                     .ThenBy(item => item.Position))
        {
            if (accepted.All(existing => Math.Abs(existing.Position - candidate.Position) >= minPeakSpacing))
            {
                accepted.Add(candidate);
                if (accepted.Count >= maxBoundaryProposals)
                {
                    break;
                }
            }
        }

        return accepted
            .OrderBy(item => item.Position)
            .Select(item => item.Position)
            .ToArray();
    }

    public static M7BStreamBoundaryMetrics EvaluateBoundaries(
        IReadOnlyList<int> proposals,
        IReadOnlyList<int> truthBoundaries,
        int toleranceSamples)
    {
        if (truthBoundaries.Count == 0)
        {
            throw new InvalidOperationException("At least one truth boundary is required for M7b evaluation.");
        }

        var unmatchedProposalIndices = new HashSet<int>(Enumerable.Range(0, proposals.Count));
        var matchedProposalIndexByTruth = new int?[truthBoundaries.Count];

        for (var truthIndex = 0; truthIndex < truthBoundaries.Count; truthIndex++)
        {
            var bestProposalIndex = -1;
            var bestDistance = int.MaxValue;

            for (var proposalIndex = 0; proposalIndex < proposals.Count; proposalIndex++)
            {
                if (!unmatchedProposalIndices.Contains(proposalIndex))
                {
                    continue;
                }

                var distance = Math.Abs(proposals[proposalIndex] - truthBoundaries[truthIndex]);
                if (distance > toleranceSamples)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestProposalIndex = proposalIndex;
                }
            }

            if (bestProposalIndex >= 0)
            {
                matchedProposalIndexByTruth[truthIndex] = bestProposalIndex;
                unmatchedProposalIndices.Remove(bestProposalIndex);
            }
        }

        static double? GetError(IReadOnlyList<int> p, IReadOnlyList<int> truth, int? proposalIndex, int truthIndex)
            => proposalIndex.HasValue ? Math.Abs(p[proposalIndex.Value] - truth[truthIndex]) : null;

        static double? GetDelay(IReadOnlyList<int> p, IReadOnlyList<int> truth, int? proposalIndex, int truthIndex)
            => proposalIndex.HasValue ? p[proposalIndex.Value] - truth[truthIndex] : null;

        return new M7BStreamBoundaryMetrics(
            OnsetHit: matchedProposalIndexByTruth[0].HasValue,
            OffsetHit: truthBoundaries.Count > 1 ? matchedProposalIndexByTruth[1].HasValue : null,
            OnsetLocalizationError: GetError(proposals, truthBoundaries, matchedProposalIndexByTruth[0], 0),
            OffsetLocalizationError: truthBoundaries.Count > 1 ? GetError(proposals, truthBoundaries, matchedProposalIndexByTruth[1], 1) : null,
            FalsePositiveCount: unmatchedProposalIndices.Count,
            OnsetDetectionDelay: GetDelay(proposals, truthBoundaries, matchedProposalIndexByTruth[0], 0),
            OffsetDetectionDelay: truthBoundaries.Count > 1 ? GetDelay(proposals, truthBoundaries, matchedProposalIndexByTruth[1], 1) : null,
            ProposedBoundaries: proposals.ToArray());
    }

    public static IReadOnlyList<int> GetWindowStarts(int streamLength, int windowLength, int stride)
    {
        if (windowLength > streamLength)
        {
            return [0];
        }

        var starts = new List<int>();
        for (var start = 0; start + windowLength <= streamLength; start += stride)
        {
            starts.Add(start);
        }

        if (starts.Count == 0)
        {
            starts.Add(0);
        }

        return starts;
    }

    private static double CalculateRobustThreshold(IReadOnlyList<double> values, double madMultiplier)
    {
        var median = Median(values);
        var absoluteDeviations = values.Select(value => Math.Abs(value - median)).ToArray();
        var mad = Median(absoluteDeviations);
        var threshold = median + (madMultiplier * mad);

        if (mad == 0d)
        {
            var max = values.Max();
            return max <= 0d ? double.PositiveInfinity : median + ((max - median) * 0.5d);
        }

        return threshold;
    }

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0d;
        }

        var ordered = values.OrderBy(value => value).ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2d
            : ordered[middle];
    }
}
