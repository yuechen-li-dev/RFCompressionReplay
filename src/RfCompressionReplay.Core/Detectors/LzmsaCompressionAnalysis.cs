namespace RfCompressionReplay.Core.Detectors;

public sealed record LzmsaCompressionAnalysis(
    int SerializedByteCount,
    int InputByteCount,
    int CompressedByteCount,
    double CompressedByteSum);
