namespace RfCompressionReplay.Core.Config;

public static class ExperimentConfigValidator
{
    public static IReadOnlyList<string> Validate(ExperimentConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.ExperimentId))
        {
            errors.Add("ExperimentId is required.");
        }

        if (string.IsNullOrWhiteSpace(config.ExperimentName))
        {
            errors.Add("ExperimentName is required.");
        }

        if (config.Seed < 0)
        {
            errors.Add("Seed must be zero or greater.");
        }

        if (string.IsNullOrWhiteSpace(config.OutputDirectory))
        {
            errors.Add("OutputDirectory is required.");
        }

        if (config.TrialCount <= 0)
        {
            errors.Add("TrialCount must be greater than zero.");
        }

        if (config.Scenario is null)
        {
            errors.Add("Scenario is required.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(config.Scenario.Name))
        {
            errors.Add("Scenario.Name is required.");
        }

        if (config.Scenario.SampleWindowCount <= 0)
        {
            errors.Add("Scenario.SampleWindowCount must be greater than zero.");
        }

        if (config.Scenario.SamplesPerWindow <= 0)
        {
            errors.Add("Scenario.SamplesPerWindow must be greater than zero.");
        }

        if (config.Detector is null)
        {
            errors.Add("Detector is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(config.Detector.Name))
            {
                errors.Add("Detector.Name is required.");
            }
            else if (!string.Equals(config.Detector.Name, "placeholder-detector", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Detector.Name '{config.Detector.Name}' is not supported in M0. Supported detectors: placeholder-detector.");
            }

            if (string.IsNullOrWhiteSpace(config.Detector.Mode))
            {
                errors.Add("Detector.Mode is required.");
            }
            else if (!string.Equals(config.Detector.Mode, "placeholder", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Detector.Mode '{config.Detector.Mode}' is not supported in M0. Supported modes: placeholder.");
            }
        }

        if (config.Signal is null)
        {
            errors.Add("Signal is required.");
        }
        else if (string.IsNullOrWhiteSpace(config.Signal.Name))
        {
            errors.Add("Signal.Name is required.");
        }
        else if (!string.Equals(config.Signal.Name, "dummy-signal", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Signal.Name '{config.Signal.Name}' is not supported in M0. Supported signals: dummy-signal.");
        }

        if (config.ManifestMetadata is null)
        {
            errors.Add("ManifestMetadata is required.");
        }

        return errors;
    }
}
