using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Randomness;

namespace RfCompressionReplay.Core.Signals;

public interface ISignalProvider
{
    string Name { get; }
    IReadOnlyList<SignalWindow> CreateWindows(int trialIndex, ScenarioConfig scenario, SignalConfig signalConfig, ISeededRandom random);
}
