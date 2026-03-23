using RfCompressionReplay.Core.Detectors;

namespace RfCompressionReplay.Core.Config;

public static class M5B2ExplorationConfigValidator
{
    private static readonly string[] RequiredDetectorNames =
    [
        DetectorCatalog.LzmsaPaperDetectorName,
        DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
        DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName,
        DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName,
    ];

    public static IReadOnlyList<string> Validate(M5B2ExplorationConfig config)
    {
        var errors = new List<string>();

        if (config.SeedPanel is null || config.SeedPanel.Count == 0)
        {
            errors.Add("SeedPanel must contain at least one seed.");
            return errors;
        }

        if (config.SeedPanel.Count < 2)
        {
            errors.Add("SeedPanel must contain at least two explicit seeds for M5b2 exploration.");
        }

        if (config.SeedPanel.Any(seed => seed < 0))
        {
            errors.Add("SeedPanel seeds must be zero or greater.");
        }

        if (config.SeedPanel.Distinct().Count() != config.SeedPanel.Count)
        {
            errors.Add("SeedPanel seeds must be distinct.");
        }

        if (config.Perturbations is null || config.Perturbations.Count < 3 || config.Perturbations.Count > 4)
        {
            errors.Add("Perturbations must contain baseline, one scale-only perturbation, one packing-only perturbation, and optionally one combined perturbation for M5b2.");
            return errors;
        }

        if (config.Perturbations.Any(perturbation => string.IsNullOrWhiteSpace(perturbation.Id)))
        {
            errors.Add("Perturbations[].Id is required.");
        }
        else if (config.Perturbations.Select(perturbation => perturbation.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != config.Perturbations.Count)
        {
            errors.Add("Perturbation ids must be distinct.");
        }

        for (var index = 0; index < config.Perturbations.Count; index++)
        {
            var perturbation = config.Perturbations[index];
            if (string.IsNullOrWhiteSpace(perturbation.AxisTag))
            {
                errors.Add($"Perturbations[{index}].AxisTag is required.");
            }
            else if (!M5B2PerturbationAxes.IsSupported(perturbation.AxisTag))
            {
                errors.Add($"Perturbations[{index}].AxisTag '{perturbation.AxisTag}' is not supported. Supported axes: {M5B2PerturbationAxes.SupportedAxesDisplay}.");
            }

            if (string.IsNullOrWhiteSpace(perturbation.Description))
            {
                errors.Add($"Perturbations[{index}].Description is required.");
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
                perturbation.Representation));

            errors.AddRange(representationErrors.Where(error => error.StartsWith("Representation.", StringComparison.Ordinal)));
        }

        ValidateAxisShape(config, errors);

        var baseValidationErrors = ExperimentConfigValidator.Validate(config.ToSeededExperimentConfig(config.SeedPanel[0], config.Perturbations[0]));
        errors.AddRange(baseValidationErrors.Where(error => !error.StartsWith("Representation.", StringComparison.Ordinal)));

        var detectorNames = config.Evaluation.Detectors.Select(detector => detector.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!detectorNames.SetEquals(RequiredDetectorNames))
        {
            errors.Add($"M5b2 exploration requires the focused detector panel exactly: {string.Join(", ", RequiredDetectorNames)}.");
        }

        return errors;
    }

    private static void ValidateAxisShape(M5B2ExplorationConfig config, List<string> errors)
    {
        var baseline = config.Perturbations.Where(perturbation => string.Equals(perturbation.AxisTag, M5B2PerturbationAxes.Baseline, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (baseline.Length != 1)
        {
            errors.Add("Perturbations must include exactly one baseline axis entry.");
        }
        else if (!Matches(baseline[0], 1d, RepresentationFormats.Float64LittleEndian))
        {
            errors.Add("Baseline perturbation must use sampleScale 1.0 and numericFormat float64-le.");
        }

        var scale = config.Perturbations.Where(perturbation => string.Equals(perturbation.AxisTag, M5B2PerturbationAxes.Scale, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (scale.Length != 1)
        {
            errors.Add("Perturbations must include exactly one scale-only axis entry.");
        }
        else if (NearlyEqual(scale[0].Representation.SampleScale, 1d)
            || !string.Equals(scale[0].Representation.NumericFormat, RepresentationFormats.Float64LittleEndian, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Scale-only perturbation must change sampleScale away from 1.0 while keeping numericFormat float64-le.");
        }

        var packing = config.Perturbations.Where(perturbation => string.Equals(perturbation.AxisTag, M5B2PerturbationAxes.Packing, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (packing.Length != 1)
        {
            errors.Add("Perturbations must include exactly one packing-only axis entry.");
        }
        else if (!NearlyEqual(packing[0].Representation.SampleScale, 1d)
            || string.Equals(packing[0].Representation.NumericFormat, RepresentationFormats.Float64LittleEndian, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Packing-only perturbation must keep sampleScale 1.0 while changing numericFormat away from float64-le.");
        }

        var combined = config.Perturbations.Where(perturbation => string.Equals(perturbation.AxisTag, M5B2PerturbationAxes.Combined, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (combined.Length > 1)
        {
            errors.Add("Perturbations may include at most one combined axis entry.");
        }
        else if (combined.Length == 1 && (NearlyEqual(combined[0].Representation.SampleScale, 1d)
            || string.Equals(combined[0].Representation.NumericFormat, RepresentationFormats.Float64LittleEndian, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Combined perturbation must change both sampleScale and numericFormat away from the baseline representation.");
        }
    }

    private static bool Matches(M5B2PerturbationConfig perturbation, double sampleScale, string numericFormat)
    {
        return NearlyEqual(perturbation.Representation.SampleScale, sampleScale)
            && string.Equals(perturbation.Representation.NumericFormat, numericFormat, StringComparison.OrdinalIgnoreCase);
    }

    private static bool NearlyEqual(double x, double y) => Math.Abs(x - y) <= 1e-9;
}
