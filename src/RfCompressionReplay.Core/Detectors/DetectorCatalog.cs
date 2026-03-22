namespace RfCompressionReplay.Core.Detectors;

public static class DetectorCatalog
{
    public const string EnergyDetectorName = "ed";
    public const string CovarianceAbsoluteValueDetectorName = "cav";
    public const string LzmsaPaperDetectorName = "lzmsa-paper";

    public const string EnergyDetectorMode = "average-energy";
    public const string CovarianceAbsoluteValueDetectorMode = "lag-1-absolute-autocovariance";
    public const string LzmsaPaperDetectorMode = "paper-byte-sum";

    public static IReadOnlyList<string> SupportedDetectorNames { get; } =
    [
        EnergyDetectorName,
        CovarianceAbsoluteValueDetectorName,
        LzmsaPaperDetectorName,
    ];

    public static IReadOnlyDictionary<string, string> SupportedModesByDetector { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EnergyDetectorName] = EnergyDetectorMode,
            [CovarianceAbsoluteValueDetectorName] = CovarianceAbsoluteValueDetectorMode,
            [LzmsaPaperDetectorName] = LzmsaPaperDetectorMode,
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

    public static string SupportedDetectorsDisplay => string.Join(", ", SupportedDetectorNames);

    public static string SupportedModesDisplay(string detectorName)
    {
        return SupportedModesByDetector.TryGetValue(detectorName, out var supportedMode)
            ? supportedMode
            : string.Join(", ", SupportedModesByDetector.Values.Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
