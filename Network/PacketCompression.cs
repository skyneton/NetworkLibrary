namespace NetworkLibrary.Network
{
    public class PacketCompression
    {
        public bool CompressionEnabled = true;
        /// <summary>
        /// Min Compress Packet Length.
        /// </summary>
        public int CompressionThreshold = 50;
        public PacketCompression(bool enabled = true, int threshold = 50)
        {
            CompressionEnabled = enabled;
            CompressionThreshold = threshold;
        }
    }
}
