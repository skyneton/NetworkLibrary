namespace NetworkLibrary.Networks
{
    public interface IRawHandler
    {
        /// <summary>
        /// When packet received first work.
        /// </summary>
        /// <param name="input">Packet Buffer.</param>
        /// <param name="size">Received count.</param>
        /// <returns>Array size must more or same size.</returns>
        byte[] Read(byte[] input, int size);
        byte[] Write(byte[] output);
    }
}
