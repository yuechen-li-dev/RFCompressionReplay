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
    public const string LzmsaCompressedByteVarianceDetectorName = "lzmsa-compressed-byte-variance";
    public const string LzmsaCompressedByteBucket0To63ProportionDetectorName = "lzmsa-compressed-byte-bucket-0-63-proportion";
    public const string LzmsaCompressedByteBucket64To127ProportionDetectorName = "lzmsa-compressed-byte-bucket-64-127-proportion";
    public const string LzmsaCompressedByteBucket128To191ProportionDetectorName = "lzmsa-compressed-byte-bucket-128-191-proportion";
    public const string LzmsaCompressedByteBucket192To255ProportionDetectorName = "lzmsa-compressed-byte-bucket-192-255-proportion";
    public const string LzmsaPrefixThirdMeanCompressedByteValueDetectorName = "lzmsa-prefix-third-mean-compressed-byte-value";
    public const string LzmsaSuffixThirdMeanCompressedByteValueDetectorName = "lzmsa-suffix-third-mean-compressed-byte-value";

    public const string EnergyDetectorMode = "average-energy";
    public const string CovarianceAbsoluteValueDetectorMode = "lag-1-absolute-autocovariance";
    public const string LzmsaPaperDetectorMode = "paper-byte-sum";
    public const string LzmsaCompressedLengthDetectorMode = "compressed-byte-count";
    public const string LzmsaNormalizedCompressedLengthDetectorMode = "compressed-byte-count-per-input-byte";
    public const string LzmsaMeanCompressedByteValueDetectorMode = "mean-compressed-byte-value";
    public const string LzmsaCompressedByteVarianceDetectorMode = "compressed-byte-variance";
    public const string LzmsaCompressedByteBucket0To63ProportionDetectorMode = "compressed-byte-bucket-0-63-proportion";
    public const string LzmsaCompressedByteBucket64To127ProportionDetectorMode = "compressed-byte-bucket-64-127-proportion";
    public const string LzmsaCompressedByteBucket128To191ProportionDetectorMode = "compressed-byte-bucket-128-191-proportion";
    public const string LzmsaCompressedByteBucket192To255ProportionDetectorMode = "compressed-byte-bucket-192-255-proportion";
    public const string LzmsaPrefixThirdMeanCompressedByteValueDetectorMode = "prefix-third-mean-compressed-byte-value";
    public const string LzmsaSuffixThirdMeanCompressedByteValueDetectorMode = "suffix-third-mean-compressed-byte-value";

    public static IReadOnlyList<string> SupportedDetectorNames { get; } =
    [
        EnergyDetectorName,
        CovarianceAbsoluteValueDetectorName,
        LzmsaPaperDetectorName,
        LzmsaCompressedLengthDetectorName,
        LzmsaNormalizedCompressedLengthDetectorName,
        LzmsaMeanCompressedByteValueDetectorName,
        LzmsaCompressedByteVarianceDetectorName,
        LzmsaCompressedByteBucket0To63ProportionDetectorName,
        LzmsaCompressedByteBucket64To127ProportionDetectorName,
        LzmsaCompressedByteBucket128To191ProportionDetectorName,
        LzmsaCompressedByteBucket192To255ProportionDetectorName,
        LzmsaPrefixThirdMeanCompressedByteValueDetectorName,
        LzmsaSuffixThirdMeanCompressedByteValueDetectorName,
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
            [LzmsaCompressedByteVarianceDetectorName] = LzmsaCompressedByteVarianceDetectorMode,
            [LzmsaCompressedByteBucket0To63ProportionDetectorName] = LzmsaCompressedByteBucket0To63ProportionDetectorMode,
            [LzmsaCompressedByteBucket64To127ProportionDetectorName] = LzmsaCompressedByteBucket64To127ProportionDetectorMode,
            [LzmsaCompressedByteBucket128To191ProportionDetectorName] = LzmsaCompressedByteBucket128To191ProportionDetectorMode,
            [LzmsaCompressedByteBucket192To255ProportionDetectorName] = LzmsaCompressedByteBucket192To255ProportionDetectorMode,
            [LzmsaPrefixThirdMeanCompressedByteValueDetectorName] = LzmsaPrefixThirdMeanCompressedByteValueDetectorMode,
            [LzmsaSuffixThirdMeanCompressedByteValueDetectorName] = LzmsaSuffixThirdMeanCompressedByteValueDetectorMode,
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
            [LzmsaCompressedByteVarianceDetectorName] = ScoreOrientation.HigherScoreMorePositive,
            [LzmsaCompressedByteBucket0To63ProportionDetectorName] = ScoreOrientation.HigherScoreMorePositive,
            [LzmsaCompressedByteBucket64To127ProportionDetectorName] = ScoreOrientation.HigherScoreMorePositive,
            [LzmsaCompressedByteBucket128To191ProportionDetectorName] = ScoreOrientation.HigherScoreMorePositive,
            [LzmsaCompressedByteBucket192To255ProportionDetectorName] = ScoreOrientation.HigherScoreMorePositive,
            [LzmsaPrefixThirdMeanCompressedByteValueDetectorName] = ScoreOrientation.HigherScoreMorePositive,
            [LzmsaSuffixThirdMeanCompressedByteValueDetectorName] = ScoreOrientation.HigherScoreMorePositive,
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
