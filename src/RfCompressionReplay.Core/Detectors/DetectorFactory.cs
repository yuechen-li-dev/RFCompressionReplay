using RfCompressionReplay.Core.Compression;
using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Detectors;

public static class DetectorFactory
{
    public static IDetector Create(DetectorConfig config)
    {
        if (!DetectorCatalog.IsSupportedDetector(config.Name))
        {
            throw new InvalidOperationException($"Detector '{config.Name}' is not supported. Supported detectors: {DetectorCatalog.SupportedDetectorsDisplay}.");
        }

        if (!DetectorCatalog.IsSupportedMode(config.Name, config.Mode))
        {
            throw new InvalidOperationException($"Detector mode '{config.Mode}' is not supported for detector '{config.Name}'. Supported modes: {DetectorCatalog.SupportedModesDisplay(config.Name)}.");
        }

        return config.Name.ToLowerInvariant() switch
        {
            DetectorCatalog.EnergyDetectorName => new EnergyDetector(),
            DetectorCatalog.CovarianceAbsoluteValueDetectorName => new CovarianceAbsoluteValueDetector(),
            DetectorCatalog.LzmsaPaperDetectorName => new LzmsaPaperDetector(new LzmsaWindowSerializer(), new BrotliCompressionCodec()),
            DetectorCatalog.LzmsaCompressedLengthDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaCompressedLengthDetectorName,
                new LzmsaWindowSerializer(),
                new BrotliCompressionCodec(),
                analysis => analysis.CompressedByteCount),
            DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaNormalizedCompressedLengthDetectorName,
                new LzmsaWindowSerializer(),
                new BrotliCompressionCodec(),
                analysis => analysis.InputByteCount == 0 ? 0d : (double)analysis.CompressedByteCount / analysis.InputByteCount),
            DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName => new LzmsaCompressionDetector(
                DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
                new LzmsaWindowSerializer(),
                new BrotliCompressionCodec(),
                analysis => analysis.MeanCompressedByteValue),
            _ => throw new InvalidOperationException($"Detector '{config.Name}' is not supported."),
        };
    }
}
