using RfCompressionReplay.Core.Config;

namespace RfCompressionReplay.Core.Signals.Synthetic;

public sealed class BurstOfdmLikeGenerator
{
    private readonly OfdmLikeSignalGenerator _carrierGenerator;

    public BurstOfdmLikeGenerator(OfdmLikeSignalGenerator carrierGenerator)
    {
        _carrierGenerator = carrierGenerator;
    }

    public double[] Generate(int sampleCount, BurstOfdmLikeConfig config, int seed)
    {
        var samples = new double[sampleCount];
        if (sampleCount <= 0)
        {
            return samples;
        }

        var burstStart = Math.Clamp((int)Math.Round(config.StartFraction * sampleCount), 0, Math.Max(0, sampleCount - 1));
        var burstLength = Math.Max(1, (int)Math.Round(config.LengthFraction * sampleCount));
        var burstEnd = Math.Min(sampleCount, burstStart + burstLength);
        var activeSampleCount = Math.Max(1, burstEnd - burstStart);
        var burstCarrier = _carrierGenerator.Generate(activeSampleCount, config.Carrier, seed);

        Array.Copy(burstCarrier, 0, samples, burstStart, activeSampleCount);
        return samples;
    }
}
