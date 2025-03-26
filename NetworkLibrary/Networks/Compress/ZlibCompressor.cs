using System.IO.Compression;

namespace NetworkLibrary.Networks.Compress
{
    public class ZlibCompressor : ICompressor
    {
        public byte[] Compress(byte[] input)
        {
            using var ms = new MemoryStream();
            using var compressor = new DeflateStream(ms, CompressionMode.Compress);
            compressor.Write(input, 0, input.Length);
            compressor.Dispose();

            return ms.ToArray();
        }

        public byte[] Decompress(byte[] input)
        {
            using var ms = new MemoryStream(input);
            using var output = new MemoryStream();
            using var decompressor = new DeflateStream(ms, CompressionMode.Decompress);
            decompressor.CopyTo(output);
            return output.ToArray();
        }
    }
}
