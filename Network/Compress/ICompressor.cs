namespace NetworkLibrary.Network.Compress
{
    public interface ICompressor
    {
        byte[] Compress(byte[] input);
        byte[] Decompress(byte[] input);
    }
}
