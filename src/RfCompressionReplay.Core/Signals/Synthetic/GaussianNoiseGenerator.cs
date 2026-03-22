using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Randomness;

namespace RfCompressionReplay.Core.Signals.Synthetic;

public sealed class GaussianNoiseGenerator
{
    public double[] Generate(int sampleCount, GaussianNoiseConfig config, ISeededRandom random)
    {
        var samples = new double[sampleCount];
        for (var index = 0; index < sampleCount; index++)
        {
            samples[index] = GaussianMath.NextGaussian(random, config.Mean, config.StandardDeviation);
        }

        return samples;
    }
}
