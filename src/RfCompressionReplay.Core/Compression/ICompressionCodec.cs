namespace RfCompressionReplay.Core.Compression;

public interface ICompressionCodec
{
    string Name { get; }

    byte[] Compress(byte[] input);
}
