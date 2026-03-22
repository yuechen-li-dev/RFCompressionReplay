namespace RfCompressionReplay.Core.Signals.Synthetic;

public sealed class SnrMixer
{
    public double[] MixToTargetSnr(double[] signalSamples, double[] noiseSamples, double targetSnrDb)
    {
        if (signalSamples.Length != noiseSamples.Length)
        {
            throw new InvalidOperationException("Signal and noise streams must have identical lengths before SNR mixing.");
        }

        var signalPower = CalculateAveragePower(signalSamples);
        var noisePower = CalculateAveragePower(noiseSamples);
        if (signalPower <= 0d)
        {
            throw new InvalidOperationException("Signal power must be greater than zero before SNR mixing.");
        }

        if (noisePower <= 0d)
        {
            throw new InvalidOperationException("Noise power must be greater than zero before SNR mixing.");
        }

        var targetSnrLinear = Math.Pow(10d, targetSnrDb / 10d);
        var signalScale = Math.Sqrt(targetSnrLinear * noisePower / signalPower);
        var mixed = new double[signalSamples.Length];
        for (var index = 0; index < mixed.Length; index++)
        {
            mixed[index] = (signalSamples[index] * signalScale) + noiseSamples[index];
        }

        return mixed;
    }

    public static double CalculateAveragePower(IReadOnlyList<double> samples)
    {
        if (samples.Count == 0)
        {
            return 0d;
        }

        var sum = 0d;
        for (var index = 0; index < samples.Count; index++)
        {
            sum += samples[index] * samples[index];
        }

        return sum / samples.Count;
    }

    public static double CalculateSnrDb(IReadOnlyList<double> scaledSignalSamples, IReadOnlyList<double> noiseSamples)
    {
        var signalPower = CalculateAveragePower(scaledSignalSamples);
        var noisePower = CalculateAveragePower(noiseSamples);
        return 10d * Math.Log10(signalPower / noisePower);
    }
}
