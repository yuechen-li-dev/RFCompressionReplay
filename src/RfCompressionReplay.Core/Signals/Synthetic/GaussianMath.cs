using RfCompressionReplay.Core.Randomness;

namespace RfCompressionReplay.Core.Signals.Synthetic;

internal static class GaussianMath
{
    public static double NextGaussian(ISeededRandom random, double mean, double standardDeviation)
    {
        if (standardDeviation <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(standardDeviation), "Standard deviation must be greater than zero.");
        }

        var u1 = 1d - random.NextDouble();
        var u2 = 1d - random.NextDouble();
        var standardNormal = Math.Sqrt(-2d * Math.Log(u1)) * Math.Cos(2d * Math.PI * u2);
        return mean + (standardDeviation * standardNormal);
    }
}
