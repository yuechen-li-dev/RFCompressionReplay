using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Execution;

public sealed class RunDirectoryFactory
{
    public string Create(string outputRoot, ExperimentConfig config, DateTimeOffset utcTimestamp)
    {
        var safeExperimentId = string.Concat(config.ExperimentId.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-'));
        var baseFolderName = $"{utcTimestamp:yyyyMMddTHHmmssZ}_{safeExperimentId}_seed{config.Seed}";
        var candidate = Path.Combine(outputRoot, baseFolderName);

        if (!Directory.Exists(candidate))
        {
            return candidate;
        }

        for (var collisionIndex = 2; collisionIndex < int.MaxValue; collisionIndex++)
        {
            candidate = Path.Combine(outputRoot, $"{baseFolderName}_{collisionIndex}");
            if (!Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Could not create a unique run directory beneath '{outputRoot}'.");
    }
}
