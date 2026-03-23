using System.Text.Json;
using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Evaluation;
using RfCompressionReplay.Core.Execution;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: RfCompressionReplay.Cli <config-path>");
    return 1;
}

var configPath = args[0];

try
{
    var fullConfigPath = Path.GetFullPath(configPath);
    if (!File.Exists(fullConfigPath))
    {
        Console.Error.WriteLine($"Config file not found: {fullConfigPath}");
        return 1;
    }

    var artifactWriter = new ArtifactFileWriter(new CsvArtifactWriter());
    var environmentSummaryProvider = new EnvironmentSummaryProvider();
    var gitCommitResolver = new GitCommitResolver();
    var runClock = new SystemRunClock();
    var application = new ExperimentApplication(
        runClock,
        new RunDirectoryFactory(),
        artifactWriter,
        environmentSummaryProvider,
        gitCommitResolver);
    var bundleEvaluator = new TinyLogisticRegressionBundleEvaluator(new RocAucCalculator());

    if (IsM7BChangePointConfig(fullConfigPath))
    {
        var config = M7BChangePointConfigJson.Load(fullConfigPath);
        var changePointApplication = new M7BChangePointExperimentApplication(
            runClock,
            environmentSummaryProvider,
            gitCommitResolver);
        var runDirectory = changePointApplication.Run(config, fullConfigPath, ResolveRepositoryRoot());
        Console.WriteLine($"Run completed: {config.ExperimentId} ({config.Scenario.Name})");
        Console.WriteLine($"Artifacts: {runDirectory}");
        return 0;
    }

    if (IsM6A2ComplementaryValueConfig(fullConfigPath))
    {
        var config = M6A2ComplementaryValueConfigJson.Load(fullConfigPath);
        var complementaryValueApplication = new M6A2ComplementaryValueExperimentApplication(
            runClock,
            application,
            environmentSummaryProvider,
            gitCommitResolver,
            bundleEvaluator);
        var runDirectory = complementaryValueApplication.Run(config, fullConfigPath, ResolveRepositoryRoot());
        Console.WriteLine($"Run completed: {config.ExperimentId} ({config.Scenario.Name})");
        Console.WriteLine($"Artifacts: {runDirectory}");
        return 0;
    }

    if (IsM6A1UsefulnessConfig(fullConfigPath))
    {
        var config = M6A1UsefulnessConfigJson.Load(fullConfigPath);
        var usefulnessApplication = new M6A1UsefulnessExperimentApplication(
            runClock,
            application,
            environmentSummaryProvider,
            gitCommitResolver);
        var runDirectory = usefulnessApplication.Run(config, fullConfigPath, ResolveRepositoryRoot());
        Console.WriteLine($"Run completed: {config.ExperimentId} ({config.Scenario.Name})");
        Console.WriteLine($"Artifacts: {runDirectory}");
        return 0;
    }

    if (IsM5B3ExplorationConfig(fullConfigPath))
    {
        var config = M5B3ExplorationConfigJson.Load(fullConfigPath);
        var explorationApplication = new M5B3ExplorationExperimentApplication(
            runClock,
            application,
            environmentSummaryProvider,
            gitCommitResolver);
        var runDirectory = explorationApplication.Run(config, fullConfigPath, ResolveRepositoryRoot());
        Console.WriteLine($"Run completed: {config.ExperimentId} ({config.Scenario.Name})");
        Console.WriteLine($"Artifacts: {runDirectory}");
        return 0;
    }

    if (IsM5B2ExplorationConfig(fullConfigPath))
    {
        var config = M5B2ExplorationConfigJson.Load(fullConfigPath);
        var explorationApplication = new M5B2ExplorationExperimentApplication(
            runClock,
            application,
            environmentSummaryProvider,
            gitCommitResolver);
        var runDirectory = explorationApplication.Run(config, fullConfigPath, ResolveRepositoryRoot());
        Console.WriteLine($"Run completed: {config.ExperimentId} ({config.Scenario.Name})");
        Console.WriteLine($"Artifacts: {runDirectory}");
        return 0;
    }

    if (IsM5B1ExplorationConfig(fullConfigPath))
    {
        var config = M5B1ExplorationConfigJson.Load(fullConfigPath);
        var explorationApplication = new M5B1ExplorationExperimentApplication(
            runClock,
            application,
            environmentSummaryProvider,
            gitCommitResolver);
        var runDirectory = explorationApplication.Run(config, fullConfigPath, ResolveRepositoryRoot());
        Console.WriteLine($"Run completed: {config.ExperimentId} ({config.Scenario.Name})");
        Console.WriteLine($"Artifacts: {runDirectory}");
        return 0;
    }

    if (IsM5A3StabilityConfig(fullConfigPath))
    {
        var config = M5A3StabilityConfigJson.Load(fullConfigPath);
        var stabilityApplication = new M5A3StabilityExperimentApplication(
            runClock,
            application,
            environmentSummaryProvider,
            gitCommitResolver);
        var runDirectory = stabilityApplication.Run(config, fullConfigPath, ResolveRepositoryRoot());
        Console.WriteLine($"Run completed: {config.ExperimentId} ({config.Scenario.Name})");
        Console.WriteLine($"Artifacts: {runDirectory}");
        return 0;
    }

    var standardConfig = ExperimentConfigJson.Load(fullConfigPath);
    var run = application.Run(standardConfig, fullConfigPath, ResolveRepositoryRoot());
    Console.WriteLine($"Run completed: {run.Manifest.ExperimentId} ({run.Manifest.ScenarioName})");
    Console.WriteLine($"Artifacts: {run.RunDirectory}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static bool IsM5A3StabilityConfig(string configPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    return document.RootElement.TryGetProperty("seedPanel", out _)
        && !IsM7BChangePointDocument(document.RootElement)
        && !IsM6A2ComplementaryValueDocument(document.RootElement)
        && !IsM6A1UsefulnessDocument(document.RootElement)
        && !document.RootElement.TryGetProperty("perturbations", out _);
}

static bool IsM7BChangePointConfig(string configPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    return IsM7BChangePointDocument(document.RootElement);
}

static bool IsM6A2ComplementaryValueConfig(string configPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    return IsM6A2ComplementaryValueDocument(document.RootElement);
}

static bool IsM6A1UsefulnessConfig(string configPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    return IsM6A1UsefulnessDocument(document.RootElement);
}

static bool IsM5B1ExplorationConfig(string configPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    return document.RootElement.TryGetProperty("seedPanel", out _)
        && document.RootElement.TryGetProperty("perturbations", out _)
        && !HasM5B2AxisTags(document.RootElement);
}

static bool IsM5B3ExplorationConfig(string configPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    return document.RootElement.TryGetProperty("seedPanel", out _)
        && document.RootElement.TryGetProperty("scaleValues", out _)
        && document.RootElement.TryGetProperty("representationFamilies", out _);
}

static bool IsM5B2ExplorationConfig(string configPath)
{
    using var document = JsonDocument.Parse(File.ReadAllText(configPath));
    return document.RootElement.TryGetProperty("seedPanel", out _)
        && document.RootElement.TryGetProperty("perturbations", out _)
        && HasM5B2AxisTags(document.RootElement);
}

static bool HasM5B2AxisTags(JsonElement rootElement)
{
    if (!rootElement.TryGetProperty("perturbations", out var perturbations)
        || perturbations.ValueKind != JsonValueKind.Array)
    {
        return false;
    }

    foreach (var perturbation in perturbations.EnumerateArray())
    {
        if (!perturbation.TryGetProperty("axisTag", out _))
        {
            return false;
        }
    }

    return true;
}

static bool IsM6A1UsefulnessDocument(JsonElement rootElement)
{
    if (!rootElement.TryGetProperty("seedPanel", out _)
        || !rootElement.TryGetProperty("evaluation", out var evaluation)
        || !evaluation.TryGetProperty("tasks", out var tasks)
        || tasks.ValueKind != JsonValueKind.Array)
    {
        return false;
    }

    var taskNames = tasks.EnumerateArray()
        .Where(task => task.TryGetProperty("name", out _))
        .Select(task => task.GetProperty("name").GetString())
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .ToArray();

    return taskNames.Length == 3
        && taskNames.Contains("structured-burst-vs-noise-only", StringComparer.Ordinal)
        && taskNames.Contains("colored-nuisance-vs-white-noise", StringComparer.Ordinal)
        && taskNames.Contains("equal-energy-structured-vs-unstructured", StringComparer.Ordinal);
}

static bool IsM7BChangePointDocument(JsonElement rootElement)
{
    if (!rootElement.TryGetProperty("seedPanel", out _)
        || !rootElement.TryGetProperty("benchmark", out var benchmark)
        || !benchmark.TryGetProperty("tasks", out var tasks)
        || tasks.ValueKind != JsonValueKind.Array)
    {
        return false;
    }

    var taskNames = tasks.EnumerateArray()
        .Where(task => task.TryGetProperty("name", out _))
        .Select(task => task.GetProperty("name").GetString())
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .ToArray();

    return taskNames.Length == 3
        && taskNames.Contains("quiet-to-structured-regime", StringComparer.Ordinal)
        && taskNames.Contains("correlated-nuisance-to-engineered-structure", StringComparer.Ordinal)
        && taskNames.Contains("structure-to-structure-regime-shift", StringComparer.Ordinal);
}

static bool IsM6A2ComplementaryValueDocument(JsonElement rootElement)
{
    if (!rootElement.TryGetProperty("seedPanel", out _)
        || !rootElement.TryGetProperty("bundles", out _)
        || !rootElement.TryGetProperty("evaluation", out var evaluation)
        || !evaluation.TryGetProperty("tasks", out var tasks)
        || tasks.ValueKind != JsonValueKind.Array)
    {
        return false;
    }

    var taskNames = tasks.EnumerateArray()
        .Where(task => task.TryGetProperty("name", out _))
        .Select(task => task.GetProperty("name").GetString())
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .ToArray();

    return taskNames.Length == 2
        && taskNames.Contains("engineered-structure-vs-natural-correlation", StringComparer.Ordinal)
        && taskNames.Contains("equal-energy-engineered-structure-vs-natural-correlation", StringComparer.Ordinal);
}

static string ResolveRepositoryRoot()
{
    var current = Directory.GetCurrentDirectory();
    while (!string.IsNullOrWhiteSpace(current))
    {
        if (Directory.Exists(Path.Combine(current, ".git")))
        {
            return current;
        }

        current = Directory.GetParent(current)?.FullName ?? string.Empty;
    }

    return Directory.GetCurrentDirectory();
}
