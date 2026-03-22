using System.Diagnostics;

namespace RfCompressionReplay.Tests;

public sealed class CliSmokeTests
{
    [Fact]
    public void CliShowsUsageWhenConfigArgumentMissing()
    {
        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/RfCompressionReplay.Cli/RfCompressionReplay.Cli.csproj"));
        var psi = new ProcessStartInfo("/tmp/dotnet/dotnet", $"run --project \"{projectPath}\" --no-build")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)!;
        process.WaitForExit();
        var stderr = process.StandardError.ReadToEnd();

        Assert.NotEqual(0, process.ExitCode);
        Assert.Contains("Usage: RfCompressionReplay.Cli <config-path>", stderr);
    }
}
