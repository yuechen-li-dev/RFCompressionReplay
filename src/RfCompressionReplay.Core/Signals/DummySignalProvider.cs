using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Randomness;

namespace RfCompressionReplay.Core.Signals;

public sealed class DummySignalProvider : ISignalProvider
{
    public string Name => "dummy-signal";

    public IReadOnlyList<SignalWindow> CreateWindows(int trialIndex, ScenarioConfig scenario, SignalConfig signalConfig, ISeededRandom random)
    {
        var windows = new List<SignalWindow>(scenario.SampleWindowCount);

        for (var windowIndex = 0; windowIndex < scenario.SampleWindowCount; windowIndex++)
        {
            var samples = new double[scenario.SamplesPerWindow];
            for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
            {
                var deterministicBase = signalConfig.BaseLevel + (trialIndex * 0.25d) + (windowIndex * 0.1d) + (sampleIndex * 0.01d);
                var noise = (random.NextDouble() - 0.5d) * signalConfig.NoiseScale;
                samples[sampleIndex] = Math.Round(deterministicBase + noise, 6, MidpointRounding.AwayFromZero);
            }

            windows.Add(new SignalWindow(trialIndex, windowIndex, samples));
        }

        return windows;
    }
}
