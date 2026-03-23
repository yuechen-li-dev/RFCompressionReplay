using RfCompressionReplay.Core.Detectors;

namespace RfCompressionReplay.Core.Config;

public static class M5B3ExplorationConfigValidator
{
    private const string RawFamilyId = "raw-scaled";
    private const string NormalizedFamilyId = "normalized-rms";

    private static readonly string[] RequiredDetectorNames =
    [
        DetectorCatalog.LzmsaPaperDetectorName,
        DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
        DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName,
        DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName,
    ];

    public static IReadOnlyList<string> Validate(M5B3ExplorationConfig config)
    {
        var errors = new List<string>();

        if (config.SeedPanel is null || config.SeedPanel.Count == 0)
        {
            errors.Add("SeedPanel must contain at least one seed.");
            return errors;
        }

        if (config.SeedPanel.Count < 2)
        {
            errors.Add("SeedPanel must contain at least two explicit seeds for M5b3 exploration.");
        }

        if (config.SeedPanel.Any(seed => seed < 0))
        {
            errors.Add("SeedPanel seeds must be zero or greater.");
        }

        if (config.SeedPanel.Distinct().Count() != config.SeedPanel.Count)
        {
            errors.Add("SeedPanel seeds must be distinct.");
        }

        if (config.ScaleValues is null || config.ScaleValues.Count < 3 || config.ScaleValues.Count > 4)
        {
            errors.Add("ScaleValues must contain a compact explicit scale panel of 3 or 4 values for M5b3.");
        }
        else
        {
            if (config.ScaleValues.Any(scale => double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0d))
            {
                errors.Add("ScaleValues must be finite numbers greater than zero.");
            }

            if (!ContainsScale(config.ScaleValues, 0.5d) || !ContainsScale(config.ScaleValues, 1d) || !ContainsScale(config.ScaleValues, 2d))
            {
                errors.Add("ScaleValues must include 0.5, 1.0, and 2.0 at minimum.");
            }

            if (config.ScaleValues.Distinct().Count() != config.ScaleValues.Count)
            {
                errors.Add("ScaleValues must be distinct.");
            }
        }

        if (config.RepresentationFamilies is null || config.RepresentationFamilies.Count != 2)
        {
            errors.Add("RepresentationFamilies must contain exactly two entries for M5b3: raw-scaled and one normalization variant.");
            return errors;
        }

        if (config.RepresentationFamilies.Any(family => string.IsNullOrWhiteSpace(family.Id)))
        {
            errors.Add("RepresentationFamilies[].Id is required.");
        }
        else if (config.RepresentationFamilies.Select(family => family.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != config.RepresentationFamilies.Count)
        {
            errors.Add("Representation family ids must be distinct.");
        }

        for (var index = 0; index < config.RepresentationFamilies.Count; index++)
        {
            var family = config.RepresentationFamilies[index];
            if (string.IsNullOrWhiteSpace(family.Description))
            {
                errors.Add($"RepresentationFamilies[{index}].Description is required.");
            }

            var representationErrors = ExperimentConfigValidator.Validate(new ExperimentConfig(
                config.ExperimentId,
                config.ExperimentName,
                config.SeedPanel[0],
                config.OutputDirectory,
                config.Scenario,
                config.TrialCount,
                config.Detector,
                config.Signal,
                config.Benchmark,
                config.Evaluation,
                config.ManifestMetadata,
                config.ArtifactRetentionMode,
                family.Representation));

            errors.AddRange(representationErrors.Where(error => error.StartsWith("Representation.", StringComparison.Ordinal)));
        }

        ValidateFamilies(config, errors);

        if (config.ScaleValues is { Count: > 0 } && config.RepresentationFamilies is { Count: > 0 })
        {
            var baseValidationErrors = ExperimentConfigValidator.Validate(config.ToSeededExperimentConfig(config.SeedPanel[0], config.RepresentationFamilies[0], config.ScaleValues[0]));
            errors.AddRange(baseValidationErrors.Where(error => !error.StartsWith("Representation.", StringComparison.Ordinal)));
        }

        var detectorNames = config.Evaluation.Detectors.Select(detector => detector.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!detectorNames.SetEquals(RequiredDetectorNames))
        {
            errors.Add($"M5b3 exploration requires the focused detector panel exactly: {string.Join(", ", RequiredDetectorNames)}.");
        }

        return errors;
    }

    private static void ValidateFamilies(M5B3ExplorationConfig config, List<string> errors)
    {
        var raw = config.RepresentationFamilies.SingleOrDefault(family => string.Equals(family.Id, RawFamilyId, StringComparison.OrdinalIgnoreCase));
        var normalized = config.RepresentationFamilies.SingleOrDefault(family => string.Equals(family.Id, NormalizedFamilyId, StringComparison.OrdinalIgnoreCase));

        if (raw is null)
        {
            errors.Add("RepresentationFamilies must include a raw-scaled family.");
        }
        else
        {
            if (!string.Equals(raw.Representation.NumericFormat, RepresentationFormats.Float64LittleEndian, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("The raw-scaled family must use numericFormat float64-le.");
            }

            if (!string.Equals(raw.Representation.NormalizationMode, RepresentationNormalizations.None, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("The raw-scaled family must use normalizationMode none.");
            }
        }

        if (normalized is null)
        {
            errors.Add("RepresentationFamilies must include a normalized-rms family.");
        }
        else
        {
            if (!string.Equals(normalized.Representation.NumericFormat, RepresentationFormats.Float64LittleEndian, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("The normalized-rms family must use numericFormat float64-le.");
            }

            if (!string.Equals(normalized.Representation.NormalizationMode, RepresentationNormalizations.Rms, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("The normalized-rms family must use normalizationMode rms.");
            }

            if (normalized.Representation.NormalizationTarget <= 0d)
            {
                errors.Add("The normalized-rms family must use a positive normalizationTarget.");
            }
        }
    }

    private static bool ContainsScale(IReadOnlyList<double> scaleValues, double expected)
    {
        return scaleValues.Any(scale => Math.Abs(scale - expected) <= 1e-9);
    }
}
