using System.Text.Json;
using RfCompressionReplay.Core.Artifacts;
using RfCompressionReplay.Core.Config;
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
    return document.RootElement.TryGetProperty("seedPanel", out _);
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
