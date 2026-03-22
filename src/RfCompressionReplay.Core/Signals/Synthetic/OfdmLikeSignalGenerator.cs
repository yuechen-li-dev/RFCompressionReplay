using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Randomness;

namespace RfCompressionReplay.Core.Signals.Synthetic;

public sealed class OfdmLikeSignalGenerator
{
    public double[] Generate(int sampleCount, OfdmLikeSignalConfig config, int seed)
    {
        var samples = new double[sampleCount];

        for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            var symbolIndex = sampleIndex / config.SamplesPerSymbol;
            var sampleOffset = sampleIndex % config.SamplesPerSymbol;
            var sampleValue = 0d;

            for (var subcarrierIndex = 0; subcarrierIndex < config.SubcarrierCount; subcarrierIndex++)
            {
                var symbolSeed = SeedMath.Combine(seed, config.SymbolSeed, symbolIndex, subcarrierIndex, config.SubcarrierCount);
                var symbolRandom = new SeededRandom(symbolSeed);
                var symbol = symbolRandom.NextDouble() >= 0.5d ? 1d : -1d;
                var normalizedCarrier = (subcarrierIndex + 1) * config.CarrierSpacing;
                var phase = 2d * Math.PI * normalizedCarrier * (sampleOffset / (double)config.SamplesPerSymbol);
                sampleValue += symbol * Math.Cos(phase);
            }

            var normalized = sampleValue / Math.Sqrt(config.SubcarrierCount);
            samples[sampleIndex] = config.Amplitude * normalized;
        }

        return samples;
    }
}
