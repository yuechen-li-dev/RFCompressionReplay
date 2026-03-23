using RfCompressionReplay.Core.Evaluation;

namespace RfCompressionReplay.Core.Config;

public static class M5A3StabilityConfigValidator
{
    public static IReadOnlyList<string> Validate(M5A3StabilityConfig config)
    {
        var errors = new List<string>();

        if (config.SeedPanel is null || config.SeedPanel.Count == 0)
        {
            errors.Add("SeedPanel must contain at least one seed.");
            return errors;
        }

        if (config.SeedPanel.Count < 3)
        {
            errors.Add("SeedPanel must contain at least three explicit seeds for M5a3 stability confirmation.");
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

        if (!M5A2ScoreDecompositionReportBuilder.IsEnabled(config.ToSeededExperimentConfig(config.SeedPanel[0])))
        {
            errors.Add("M5a3 stability confirmation requires the current M5a2 detector set exactly as implemented on current main.");
        }

        return errors;
    }
}
