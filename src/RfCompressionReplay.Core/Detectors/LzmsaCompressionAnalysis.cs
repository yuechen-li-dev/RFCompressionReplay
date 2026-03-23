namespace RfCompressionReplay.Core.Detectors;

public sealed record LzmsaCompressionAnalysis(
    int SerializedByteCount,
    int InputByteCount,
    int CompressedByteCount,
    double CompressedByteSum)
{
    public double MeanCompressedByteValue => CompressedByteCount == 0 ? 0d : CompressedByteSum / CompressedByteCount;
}
