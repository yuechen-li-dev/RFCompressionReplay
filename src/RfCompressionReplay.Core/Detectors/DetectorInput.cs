using RfCompressionReplay.Core.Signals;

namespace RfCompressionReplay.Core.Detectors;

public sealed record DetectorInput(int TrialIndex, IReadOnlyList<SignalWindow> Windows);
