using RfCompressionReplay.Core.Evaluation;

namespace RfCompressionReplay.Core.Detectors;

public static class DetectorCatalog
{
    public const string EnergyDetectorName = "ed";
    public const string CovarianceAbsoluteValueDetectorName = "cav";
    public const string LzmsaPaperDetectorName = "lzmsa-paper";
    public const string LzmsaCompressedLengthDetectorName = "lzmsa-compressed-length";
    public const string LzmsaNormalizedCompressedLengthDetectorName = "lzmsa-normalized-compressed-length";
    public const string LzmsaMeanCompressedByteValueDetectorName = "lzmsa-mean-compressed-byte-value";

    public const string EnergyDetectorMode = "average-energy";
    public const string CovarianceAbsoluteValueDetectorMode = "lag-1-absolute-autocovariance";
    public const string LzmsaPaperDetectorMode = "paper-byte-sum";
    public const string LzmsaCompressedLengthDetectorMode = "compressed-byte-count";
    public const string LzmsaNormalizedCompressedLengthDetectorMode = "compressed-byte-count-per-input-byte";
    public const string LzmsaMeanCompressedByteValueDetectorMode = "mean-compressed-byte-value";

    public static IReadOnlyList<string> SupportedDetectorNames { get; } =
    [
        EnergyDetectorName,
        CovarianceAbsoluteValueDetectorName,
        LzmsaPaperDetectorName,
        LzmsaCompressedLengthDetectorName,
        LzmsaNormalizedCompressedLengthDetectorName,
        LzmsaMeanCompressedByteValueDetectorName,
    ];

    public static IReadOnlyDictionary<string, string> SupportedModesByDetector { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EnergyDetectorName] = EnergyDetectorMode,
            [CovarianceAbsoluteValueDetectorName] = CovarianceAbsoluteValueDetectorMode,
            [LzmsaPaperDetectorName] = LzmsaPaperDetectorMode,
            [LzmsaCompressedLengthDetectorName] = LzmsaCompressedLengthDetectorMode,
            [LzmsaNormalizedCompressedLengthDetectorName] = LzmsaNormalizedCompressedLengthDetectorMode,
            [LzmsaMeanCompressedByteValueDetectorName] = LzmsaMeanCompressedByteValueDetectorMode,
        };

    public static IReadOnlyDictionary<string, ScoreOrientation> ScoreOrientationByDetector { get; } =
        new Dictionary<string, ScoreOrientation>(StringComparer.OrdinalIgnoreCase)
        {
            [EnergyDetectorName] = ScoreOrientation.HigherScoreMorePositive,
            [CovarianceAbsoluteValueDetectorName] = ScoreOrientation.HigherScoreMorePositive,
            [LzmsaPaperDetectorName] = ScoreOrientation.HigherScoreMorePositive,
            [LzmsaCompressedLengthDetectorName] = ScoreOrientation.LowerScoreMorePositive,
            [LzmsaNormalizedCompressedLengthDetectorName] = ScoreOrientation.LowerScoreMorePositive,
            [LzmsaMeanCompressedByteValueDetectorName] = ScoreOrientation.HigherScoreMorePositive,
        };

    public static bool IsSupportedDetector(string detectorName)
    {
        return SupportedModesByDetector.ContainsKey(detectorName);
    }

    public static bool IsSupportedMode(string detectorName, string detectorMode)
    {
        return SupportedModesByDetector.TryGetValue(detectorName, out var supportedMode)
            && string.Equals(supportedMode, detectorMode, StringComparison.OrdinalIgnoreCase);
    }

    public static ScoreOrientation GetScoreOrientation(string detectorName)
    {
        if (!ScoreOrientationByDetector.TryGetValue(detectorName, out var orientation))
        {
            throw new InvalidOperationException($"Detector '{detectorName}' does not have a documented score orientation.");
        }

        return orientation;
    }


    public static bool IsPositiveAtThreshold(string detectorName, double score, double threshold)
    {
        return GetScoreOrientation(detectorName) switch
        {
            ScoreOrientation.HigherScoreMorePositive => score >= threshold,
            ScoreOrientation.LowerScoreMorePositive => score <= threshold,
            _ => throw new InvalidOperationException($"Detector '{detectorName}' does not have a supported threshold orientation."),
        };
    }

    public static string SupportedDetectorsDisplay => string.Join(", ", SupportedDetectorNames);

    public static string SupportedModesDisplay(string detectorName)
    {
        return SupportedModesByDetector.TryGetValue(detectorName, out var supportedMode)
            ? supportedMode
            : string.Join(", ", SupportedModesByDetector.Values.Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
