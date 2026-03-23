namespace RfCompressionReplay.Core.Evaluation;

public static class BenchmarkTaskCatalog
{
    public const string OfdmSignalPresentVsNoiseOnly = "ofdm-signal-present-vs-noise-only";
    public const string GaussianEmitterVsNoiseOnly = "gaussian-emitter-vs-noise-only";
    public const string StructuredBurstVsNoiseOnly = "structured-burst-vs-noise-only";
    public const string ColoredNuisanceVsWhiteNoise = "colored-nuisance-vs-white-noise";
    public const string EqualEnergyStructuredVsUnstructured = "equal-energy-structured-vs-unstructured";
    public const string EngineeredStructureVsNaturalCorrelation = "engineered-structure-vs-natural-correlation";
    public const string EqualEnergyEngineeredStructureVsNaturalCorrelation = "equal-energy-engineered-structure-vs-natural-correlation";

    public static IReadOnlyList<string> SupportedTasks { get; } =
    [
        OfdmSignalPresentVsNoiseOnly,
        GaussianEmitterVsNoiseOnly,
        StructuredBurstVsNoiseOnly,
        ColoredNuisanceVsWhiteNoise,
        EqualEnergyStructuredVsUnstructured,
        EngineeredStructureVsNaturalCorrelation,
        EqualEnergyEngineeredStructureVsNaturalCorrelation,
    ];

    public static string SupportedTasksDisplay => string.Join(", ", SupportedTasks);

    public static bool IsSupportedTask(string taskName)
    {
        return SupportedTasks.Contains(taskName, StringComparer.OrdinalIgnoreCase);
    }
}
