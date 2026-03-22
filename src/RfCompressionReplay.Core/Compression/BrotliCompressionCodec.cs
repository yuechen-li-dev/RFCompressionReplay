using System.IO.Compression;

namespace RfCompressionReplay.Core.Compression;

public sealed class BrotliCompressionCodec : ICompressionCodec
{
    public string Name => "brotli";

    public byte[] Compress(byte[] input)
    {
        using var output = new MemoryStream();
        using (var compressionStream = new BrotliStream(output, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            compressionStream.Write(input, 0, input.Length);
        }

        return output.ToArray();
    }
}
