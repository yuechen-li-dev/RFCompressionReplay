using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Execution;

public sealed class RunDirectoryFactory
{
    public string Create(string outputRoot, ExperimentConfig config, DateTimeOffset utcTimestamp)
    {
        var safeExperimentId = string.Concat(config.ExperimentId.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-'));
        var folderName = $"{utcTimestamp:yyyyMMddTHHmmssZ}_{safeExperimentId}_seed{config.Seed}";
        return Path.Combine(outputRoot, folderName);
    }
}
