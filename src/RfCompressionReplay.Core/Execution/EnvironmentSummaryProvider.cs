using System.Runtime.InteropServices;
using RfCompressionReplay.Core.Models;

namespace RfCompressionReplay.Core.Execution;

public sealed class EnvironmentSummaryProvider
{
    public EnvironmentSummary Create()
    {
        return new EnvironmentSummary(
            MachineName: Environment.MachineName,
            UserName: Environment.UserName,
            OperatingSystem: RuntimeInformation.OSDescription,
            FrameworkDescription: RuntimeInformation.FrameworkDescription,
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            CurrentDirectory: Environment.CurrentDirectory);
    }
}
