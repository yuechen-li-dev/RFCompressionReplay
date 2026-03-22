namespace RfCompressionReplay.Core.Execution;

public interface IRunClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemRunClock : IRunClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
