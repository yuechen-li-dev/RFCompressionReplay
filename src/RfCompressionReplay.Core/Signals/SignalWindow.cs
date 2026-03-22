namespace RfCompressionReplay.Core.Signals;

public sealed record SignalWindow(int TrialIndex, int WindowIndex, IReadOnlyList<double> Samples);
