using RfCompressionReplay.Core.Detectors;
using RfCompressionReplay.Core.Evaluation;

namespace RfCompressionReplay.Core.Config;

public static class M6A2ComplementaryValueConfigValidator
{
    public const string BundleAId = "bundle-a-ed-cav";
    public const string BundleBId = "bundle-b-ed-cav-rms-normalized-mean";

    private static readonly string[] RequiredTaskPanel =
    [
        BenchmarkTaskCatalog.EngineeredStructureVsNaturalCorrelation,
        BenchmarkTaskCatalog.EqualEnergyEngineeredStructureVsNaturalCorrelation,
    ];

    private static readonly string[] RequiredDetectorPanel =
    [
        DetectorCatalog.EnergyDetectorName,
        DetectorCatalog.CovarianceAbsoluteValueDetectorName,
        DetectorCatalog.LzmsaPaperDetectorName,
        DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName,
    ];

    public static IReadOnlyList<string> Validate(M6A2ComplementaryValueConfig config)
    {
        var errors = new List<string>();

        if (config.SeedPanel is null || config.SeedPanel.Count == 0)
        {
            errors.Add("SeedPanel must contain at least one seed.");
            return errors;
        }

        if (config.SeedPanel.Count < 3)
        {
            errors.Add("SeedPanel must contain at least three explicit seeds for M6a2 complementary-value usefulness mapping.");
        }

        if (config.SeedPanel.Any(seed => seed < 0))
        {
            errors.Add("SeedPanel seeds must be zero or greater.");
        }

        if (config.SeedPanel.Distinct().Count() != config.SeedPanel.Count)
        {
            errors.Add("SeedPanel seeds must be distinct.");
        }

        var baseValidationErrors = ExperimentConfigValidator.Validate(config.ToSeededExperimentConfig(config.SeedPanel[0]));
        errors.AddRange(baseValidationErrors);

        var taskNames = config.Evaluation.Tasks.Select(task => task.Name).ToArray();
        if (!taskNames.SequenceEqual(RequiredTaskPanel, StringComparer.Ordinal))
        {
            errors.Add($"M6a2 complementary-value usefulness mapping requires the focused two-task suite in order: {string.Join(", ", RequiredTaskPanel)}.");
        }

        var detectorNames = config.Evaluation.Detectors.Select(detector => detector.Name).ToArray();
        if (!detectorNames.SequenceEqual(RequiredDetectorPanel, StringComparer.Ordinal))
        {
            errors.Add($"M6a2 complementary-value usefulness mapping requires the focused detector panel exactly: {string.Join(", ", RequiredDetectorPanel)}.");
        }

        if (!config.Evaluation.SnrDbValues.SequenceEqual([-9d, -3d, 0d]))
        {
            errors.Add("M6a2 complementary-value usefulness mapping expects the readable default SNR panel exactly: -9, -3, 0 dB.");
        }

        if (!config.Evaluation.WindowLengths.SequenceEqual([64, 128]))
        {
            errors.Add("M6a2 complementary-value usefulness mapping expects the readable default window-length panel exactly: 64, 128.");
        }

        if (config.Bundles is null || config.Bundles.Count == 0)
        {
            errors.Add("Bundles must contain at least the required M6a2 bundle panel.");
            return errors;
        }

        var bundleIds = config.Bundles.Select(bundle => bundle.Id).ToArray();
        if (!bundleIds.SequenceEqual([BundleAId, BundleBId], StringComparer.Ordinal))
        {
            errors.Add($"M6a2 complementary-value usefulness mapping requires exactly two bundles in order: {BundleAId}, {BundleBId}.");
        }

        ValidateBundle(config.Bundles, BundleAId, [DetectorCatalog.EnergyDetectorName, DetectorCatalog.CovarianceAbsoluteValueDetectorName], errors);
        ValidateBundle(config.Bundles, BundleBId, [DetectorCatalog.EnergyDetectorName, DetectorCatalog.CovarianceAbsoluteValueDetectorName, DetectorCatalog.LzmsaRmsNormalizedMeanCompressedByteValueDetectorName], errors);

        return errors;
    }

    private static void ValidateBundle(
        IReadOnlyList<FeatureBundleConfig> bundles,
        string requiredId,
        IReadOnlyList<string> requiredDetectors,
        List<string> errors)
    {
        var bundle = bundles.SingleOrDefault(candidate => string.Equals(candidate.Id, requiredId, StringComparison.Ordinal));
        if (bundle is null)
        {
            errors.Add($"Bundle '{requiredId}' is required.");
            return;
        }

        if (!bundle.FeatureDetectors.SequenceEqual(requiredDetectors, StringComparer.Ordinal))
        {
            errors.Add($"Bundle '{requiredId}' must use detector features exactly: {string.Join(", ", requiredDetectors)}.");
        }
    }
}
