using RfCompressionReplay.Core.Compression;
using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Detectors;

public static class DetectorFactory
{
    public static IDetector Create(DetectorConfig config, RepresentationConfig? representation = null)
    {
        if (!DetectorCatalog.IsSupportedDetector(config.Name))
        {
            throw new InvalidOperationException($"Detector '{config.Name}' is not supported. Supported detectors: {DetectorCatalog.SupportedDetectorsDisplay}.");
        }

        if (!DetectorCatalog.IsSupportedMode(config.Name, config.Mode))
        {
            throw new InvalidOperationException($"Detector mode '{config.Mode}' is not supported for detector '{config.Name}'. Supported modes: {DetectorCatalog.SupportedModesDisplay(config.Name)}.");
        }

        var serializer = new LzmsaWindowSerializer(representation);

        return config.Name.ToLowerInvariant() switch
        {
            DetectorCatalog.EnergyDetectorName => new EnergyDetector(),
            DetectorCatalog.CovarianceAbsoluteValueDetectorName => new CovarianceAbsoluteValueDetector(),
            DetectorCatalog.LzmsaPaperDetectorName => new LzmsaPaperDetector(serializer, new BrotliCompressionCodec()),
            DetectorCatalog.LzmsaCompressedLengthDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaCompressedLengthDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.CompressedByteCount),
            DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.InputByteCount == 0 ? 0d : (double)analysis.CompressedByteCount / analysis.InputByteCount),
            DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.MeanCompressedByteValue),
            DetectorCatalog.LzmsaCompressedByteVarianceDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaCompressedByteVarianceDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.CompressedByteVariance),
            DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaCompressedByteBucket0To63ProportionDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.CompressedByteBucket0To63Proportion),
            DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.CompressedByteBucket64To127Proportion),
            DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaCompressedByteBucket128To191ProportionDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.CompressedByteBucket128To191Proportion),
            DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaCompressedByteBucket192To255ProportionDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.CompressedByteBucket192To255Proportion),
            DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaPrefixThirdMeanCompressedByteValueDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.PrefixThirdMeanCompressedByteValue),
            DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName,
                serializer,
                new BrotliCompressionCodec(),
                analysis => analysis.SuffixThirdMeanCompressedByteValue),
            _ => throw new InvalidOperationException($"Detector '{config.Name}' is not supported."),
        };
    }
}
