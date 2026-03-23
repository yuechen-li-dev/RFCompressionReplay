using RfCompressionReplay.Core.Detectors;

namespace RfCompressionReplay.Core.Config;

public static class M5B1ExplorationConfigValidator
{
    private static readonly string[] RequiredDetectorNames =
    [
        DetectorCatalog.LzmsaPaperDetectorName,
        DetectorCatalog.LzmsaMeanCompressedByteValueDetectorName,
        DetectorCatalog.LzmsaCompressedByteBucket64To127ProportionDetectorName,
        DetectorCatalog.LzmsaSuffixThirdMeanCompressedByteValueDetectorName,
    ];

    public static IReadOnlyList<string> Validate(M5B1ExplorationConfig config)
    {
        var errors = new List<string>();

        if (config.SeedPanel is null || config.SeedPanel.Count == 0)
        {
            errors.Add("SeedPanel must contain at least one seed.");
            return errors;
        }

        if (config.SeedPanel.Count < 2)
        {
            errors.Add("SeedPanel must contain at least two explicit seeds for M5b1 exploration.");
        }

        if (config.SeedPanel.Any(seed => seed < 0))
        {
            errors.Add("SeedPanel seeds must be zero or greater.");
        }

        if (config.SeedPanel.Distinct().Count() != config.SeedPanel.Count)
        {
            errors.Add("SeedPanel seeds must be distinct.");
        }

        if (config.Perturbations is null || config.Perturbations.Count != 3)
        {
            errors.Add("Perturbations must contain exactly three entries for M5b1: baseline, one numeric scaling perturbation, and one serialization perturbation.");
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

        var baselineCount = config.Perturbations.Count(perturbation =>
            NearlyEqual(perturbation.Representation.SampleScale, 1d)
            && string.Equals(perturbation.Representation.NumericFormat, RepresentationFormats.Float64LittleEndian, StringComparison.OrdinalIgnoreCase));
        if (baselineCount != 1)
        {
            errors.Add("Perturbations must include exactly one baseline representation using sampleScale 1.0 and numericFormat float64-le.");
        }

        var scalingPerturbationCount = config.Perturbations.Count(perturbation =>
            !NearlyEqual(perturbation.Representation.SampleScale, 1d)
            && string.Equals(perturbation.Representation.NumericFormat, RepresentationFormats.Float64LittleEndian, StringComparison.OrdinalIgnoreCase));
        if (scalingPerturbationCount != 1)
        {
            errors.Add("Perturbations must include exactly one numeric scaling perturbation using float64-le serialization.");
        }

        var serializationPerturbationCount = config.Perturbations.Count(perturbation =>
            NearlyEqual(perturbation.Representation.SampleScale, 1d)
            && !string.Equals(perturbation.Representation.NumericFormat, RepresentationFormats.Float64LittleEndian, StringComparison.OrdinalIgnoreCase));
        if (serializationPerturbationCount != 1)
        {
            errors.Add("Perturbations must include exactly one serialization perturbation at baseline scale.");
        }

        var baseValidationErrors = ExperimentConfigValidator.Validate(config.ToSeededExperimentConfig(config.SeedPanel[0], config.Perturbations[0]));
        errors.AddRange(baseValidationErrors.Where(error => !error.StartsWith("Representation.", StringComparison.Ordinal)));

        var detectorNames = config.Evaluation.Detectors.Select(detector => detector.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!detectorNames.SetEquals(RequiredDetectorNames))
        {
            errors.Add($"M5b1 exploration requires the focused detector panel exactly: {string.Join(", ", RequiredDetectorNames)}.");
        }

        return errors;
    }

    private static bool NearlyEqual(double x, double y) => Math.Abs(x - y) <= 1e-9;
}
