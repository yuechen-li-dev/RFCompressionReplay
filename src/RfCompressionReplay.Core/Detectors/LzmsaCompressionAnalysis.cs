namespace RfCompressionReplay.Core.Detectors;

public sealed record LzmsaCompressionAnalysis(
    int SerializedByteCount,
    int InputByteCount,
    int CompressedByteCount,
    double CompressedByteSum,
    IReadOnlyList<byte> CompressedBytes)
{
    public double MeanCompressedByteValue => CompressedByteCount == 0 ? 0d : CompressedByteSum / CompressedByteCount;

    public double CompressedByteVariance
    {
        get
        {
            if (CompressedByteCount <= 1)
            {
                return 0d;
            }

            var mean = MeanCompressedByteValue;
            return CompressedBytes
                .Select(value => Math.Pow(value - mean, 2d))
                .Average();
        }
    }

    public double CompressedByteBucket0To63Proportion => GetBucketProportion(0, 63);

    public double CompressedByteBucket64To127Proportion => GetBucketProportion(64, 127);

    public double CompressedByteBucket128To191Proportion => GetBucketProportion(128, 191);

    public double CompressedByteBucket192To255Proportion => GetBucketProportion(192, 255);

    public double PrefixThirdMeanCompressedByteValue => GetSegmentMean(0, GetThirdLength());

    public double SuffixThirdMeanCompressedByteValue => GetSegmentMean(Math.Max(0, CompressedByteCount - GetThirdLength()), GetThirdLength());

    private double GetBucketProportion(byte minimumInclusive, byte maximumInclusive)
    {
        if (CompressedByteCount == 0)
        {
            return 0d;
        }

        var count = CompressedBytes.Count(value => value >= minimumInclusive && value <= maximumInclusive);
        return (double)count / CompressedByteCount;
    }

    private int GetThirdLength()
    {
        return CompressedByteCount == 0
            ? 0
            : Math.Max(1, CompressedByteCount / 3);
    }

    private double GetSegmentMean(int startIndex, int length)
    {
        if (CompressedByteCount == 0 || length <= 0)
        {
            return 0d;
        }

        return CompressedBytes
            .Skip(startIndex)
            .Take(length)
            .Average(static value => (double)value);
    }
}
