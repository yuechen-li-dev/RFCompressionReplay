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

    var config = ExperimentConfigJson.Load(fullConfigPath);
    var application = new ExperimentApplication(
        new SystemRunClock(),
        new RunDirectoryFactory(),
        new ArtifactFileWriter(new CsvArtifactWriter()),
        new EnvironmentSummaryProvider(),
        new GitCommitResolver());

    var run = application.Run(config, fullConfigPath, ResolveRepositoryRoot());
    Console.WriteLine($"Run completed: {run.Manifest.ExperimentId} ({run.Manifest.ScenarioName})");
    Console.WriteLine($"Artifacts: {run.RunDirectory}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
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
