using System.Buffers.Binary;
using RfCompressionReplay.Core.Signals;

namespace RfCompressionReplay.Core.Detectors;

public sealed class LzmsaWindowSerializer
{
    public byte[] Serialize(IReadOnlyList<SignalWindow> windows)
    {
        var sampleCount = windows.Sum(window => window.Samples.Count);
        var bytes = new byte[sampleCount * sizeof(double)];
        var offset = 0;

        foreach (var sample in windows.SelectMany(window => window.Samples))
        {
            BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(offset, sizeof(double)), BitConverter.DoubleToInt64Bits(sample));
            offset += sizeof(double);
        }

        return bytes;
    }
}
