using RfCompressionReplay.Core.Compression;

namespace RfCompressionReplay.Core.Detectors;

public sealed class LzmsaPaperDetector : LzmsaCompressionDetector
{
    public LzmsaPaperDetector(LzmsaWindowSerializer serializer, ICompressionCodec compressionCodec)
        : base(
            DetectorCatalog.LzmsaPaperDetectorName,
            serializer,
            compressionCodec,
            analysis => analysis.CompressedByteSum)
    {
    }
}
