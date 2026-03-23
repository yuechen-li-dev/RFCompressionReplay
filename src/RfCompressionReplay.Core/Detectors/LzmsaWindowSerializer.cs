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

        foreach (var sample in windows.SelectMany(window => window.Samples))
        {
            var scaledSample = sample * _representation.SampleScale;

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

        return bytes;
    }

    private int GetBytesPerSample()
    {
        return string.Equals(_representation.NumericFormat, RepresentationFormats.Float32LittleEndian, StringComparison.OrdinalIgnoreCase)
            ? sizeof(float)
            : sizeof(double);
    }
}
