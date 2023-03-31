namespace NetworkLibrary.Networks
{
    public class PacketCompression
    {
        public bool CompressionEnabled;
        /// <summary>
        /// Min Compress Packet Length.
        /// </summary>
        public int CompressionThreshold = 50;
        public PacketCompression(bool enabled = false, int threshold = 50)
        {
            CompressionEnabled = enabled;
            CompressionThreshold = threshold;
        }
    }
}
