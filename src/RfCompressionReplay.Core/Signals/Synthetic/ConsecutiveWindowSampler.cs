using RfCompressionReplay.Core.Randomness;

namespace RfCompressionReplay.Core.Signals.Synthetic;

public sealed class ConsecutiveWindowSampler
{
    public IReadOnlyList<int> CreateStartIndices(int baseStreamLength, int trialCount, int sampleWindowCount, int samplesPerWindow, ISeededRandom random)
    {
        var trialSampleCount = checked(sampleWindowCount * samplesPerWindow);
        if (baseStreamLength < trialSampleCount)
        {
            throw new InvalidOperationException("Base stream length must be at least as large as one trial's consecutive sample span.");
        }

        var maxStartExclusive = baseStreamLength - trialSampleCount + 1;
        var startIndices = new int[trialCount];
        for (var trialIndex = 0; trialIndex < trialCount; trialIndex++)
        {
            startIndices[trialIndex] = random.Next(0, maxStartExclusive);
        }

        return startIndices;
    }

    public IReadOnlyList<SignalWindow> ExtractWindows(double[] baseStream, int trialIndex, int startIndex, int sampleWindowCount, int samplesPerWindow)
    {
        var windows = new List<SignalWindow>(sampleWindowCount);

        for (var windowIndex = 0; windowIndex < sampleWindowCount; windowIndex++)
        {
            var offset = startIndex + (windowIndex * samplesPerWindow);
            var samples = new double[samplesPerWindow];
            Array.Copy(baseStream, offset, samples, 0, samplesPerWindow);
            windows.Add(new SignalWindow(trialIndex, windowIndex, samples));
        }

        return windows;
    }
}
