using RfCompressionReplay.Core.Execution;

namespace RfCompressionReplay.Tests;

internal sealed class FixedRunClock : IRunClock
{
    public FixedRunClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}
