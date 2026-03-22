using System.Diagnostics;

namespace RfCompressionReplay.Tests;

public sealed class CliSmokeTests
{
    [Fact]
    public void CliShowsUsageWhenConfigArgumentMissing()
    {
        var cliDllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/RfCompressionReplay.Cli/bin/Debug/net8.0/RfCompressionReplay.Cli.dll"));
        var dotnetHost = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        var psi = new ProcessStartInfo(dotnetHost, $"\"{cliDllPath}\"")
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
