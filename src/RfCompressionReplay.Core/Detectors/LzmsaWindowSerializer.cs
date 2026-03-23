using System.Buffers.Binary;
using RfCompressionReplay.Core.Config;
using RfCompressionReplay.Core.Signals;

namespace RfCompressionReplay.Core.Detectors;

public sealed class LzmsaWindowSerializer
{
    private readonly RepresentationConfig _representation;

    public LzmsaWindowSerializer(RepresentationConfig? representation = null)
    {
        _representation = representation ?? new RepresentationConfig();
    }

    public byte[] Serialize(IReadOnlyList<SignalWindow> windows)
    {
        var sampleCount = windows.Sum(window => window.Samples.Count);
        var bytesPerSample = GetBytesPerSample();
        var bytes = new byte[sampleCount * bytesPerSample];
        var offset = 0;

        foreach (var window in windows)
        {
            var normalizedSamples = TransformWindow(window.Samples);

            foreach (var scaledSample in normalizedSamples)
            {
                if (string.Equals(_representation.NumericFormat, RepresentationFormats.Float32LittleEndian, StringComparison.OrdinalIgnoreCase))
                {
                    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset, sizeof(float)), BitConverter.SingleToInt32Bits((float)scaledSample));
                    offset += sizeof(float);
                }
                else
                {
                    BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(offset, sizeof(double)), BitConverter.DoubleToInt64Bits(scaledSample));
                    offset += sizeof(double);
                }
            }
        }

        return bytes;
    }

    private IReadOnlyList<double> TransformWindow(IReadOnlyList<double> samples)
    {
        var scaled = samples.Select(sample => sample * _representation.SampleScale).ToArray();
        if (!string.Equals(_representation.NormalizationMode, RepresentationNormalizations.Rms, StringComparison.OrdinalIgnoreCase))
        {
            return scaled;
        }

        if (scaled.Length == 0)
        {
            return scaled;
        }

        var sumSquares = 0d;
        foreach (var sample in scaled)
        {
            sumSquares += sample * sample;
        }

        var rms = Math.Sqrt(sumSquares / scaled.Length);
        if (rms <= 0d)
        {
            return scaled;
        }

        var normalizationScale = _representation.NormalizationTarget / rms;
        for (var index = 0; index < scaled.Length; index++)
        {
            scaled[index] *= normalizationScale;
        }

        return scaled;
    }

    private int GetBytesPerSample()
    {
        return string.Equals(_representation.NumericFormat, RepresentationFormats.Float32LittleEndian, StringComparison.OrdinalIgnoreCase)
            ? sizeof(float)
            : sizeof(double);
    }
}
