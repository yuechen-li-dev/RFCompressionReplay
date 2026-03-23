namespace RfCompressionReplay.Core.Config;

public static class ArtifactRetentionModes
{
    public const string Full = "full";
    public const string Milestone = "milestone";
    public const string Smoke = "smoke";

    public static bool IsSupported(string? mode)
    {
        return string.Equals(mode, Full, StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, Milestone, StringComparison.OrdinalIgnoreCase)
            || string.Equals(mode, Smoke, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return Full;
        }

        if (string.Equals(mode, Full, StringComparison.OrdinalIgnoreCase))
        {
            return Full;
        }

        if (string.Equals(mode, Milestone, StringComparison.OrdinalIgnoreCase))
        {
            return Milestone;
        }

        if (string.Equals(mode, Smoke, StringComparison.OrdinalIgnoreCase))
        {
            return Smoke;
        }

        throw new InvalidOperationException($"Unsupported artifact retention mode '{mode}'.");
    }

    public static string SupportedModesDisplay => $"{Full}, {Milestone}, {Smoke}";
}

public sealed record ArtifactRetentionPlan(
    string Mode,
    bool WriteTrialsCsv,
    bool WriteRawRocPointsCsv,
    bool WriteCompactRocPointsCsv,
    IReadOnlyList<string> OmittedArtifactKinds,
    string RegenerationNote)
{
    public static ArtifactRetentionPlan Create(string mode)
    {
        var normalized = ArtifactRetentionModes.Normalize(mode);
        return normalized switch
        {
            ArtifactRetentionModes.Full => new ArtifactRetentionPlan(
                Mode: normalized,
                WriteTrialsCsv: true,
                WriteRawRocPointsCsv: true,
                WriteCompactRocPointsCsv: false,
                OmittedArtifactKinds: Array.Empty<string>(),
                RegenerationNote: "Full retention emits the raw local artifact set; milestone/smoke omissions can be reproduced by rerunning with the same config, code revision, seed, and manifest metadata."),
            ArtifactRetentionModes.Milestone => new ArtifactRetentionPlan(
                Mode: normalized,
                WriteTrialsCsv: false,
                WriteRawRocPointsCsv: false,
                WriteCompactRocPointsCsv: true,
                OmittedArtifactKinds: ["trials.csv", "roc_points.csv"],
                RegenerationNote: "Milestone retention keeps compact, conclusion-critical artifacts only. Omitted raw trial rows and full ROC thresholds are intentionally regenerable by rerunning the same config in full retention mode with the recorded code revision, seed, and manifest metadata."),
            ArtifactRetentionModes.Smoke => new ArtifactRetentionPlan(
                Mode: normalized,
                WriteTrialsCsv: false,
                WriteRawRocPointsCsv: false,
                WriteCompactRocPointsCsv: false,
                OmittedArtifactKinds: ["trials.csv", "roc_points.csv", "roc_points_compact.csv"],
                RegenerationNote: "Smoke retention is for minimal regression/CI evidence only. Fuller raw and compact evaluation artifacts are regenerable by rerunning the same config in milestone or full retention mode with the recorded code revision, seed, and manifest metadata."),
            _ => throw new InvalidOperationException($"Unsupported artifact retention mode '{mode}'.")
        };
    }
}
