using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Randomness;

namespace RfCompressionReplay.Core.Signals.Synthetic;

public sealed class CorrelatedGaussianProcessGenerator
{
    public double[] Generate(int sampleCount, CorrelatedGaussianProcessConfig config, ISeededRandom random)
    {
        var samples = new double[sampleCount];
        if (sampleCount <= 0)
        {
            return samples;
        }

        var innovationScale = config.InnovationStandardDeviation;
        var coefficient = config.ArCoefficient;
        var state = 0d;

        for (var index = 0; index < sampleCount; index++)
        {
            var innovation = GaussianMath.NextGaussian(random, 0d, innovationScale);
            state = (coefficient * state) + innovation;
            samples[index] = state;
        }

        return samples;
    }
}
