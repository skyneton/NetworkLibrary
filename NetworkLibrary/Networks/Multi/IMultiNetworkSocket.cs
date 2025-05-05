namespace NetworkLibrary.Networks.Multi
{
    internal interface IMultiNetworkSocket
    {
        void BindConnecting(TemporaryMultiNetwork network);
        void BindConnecting(TemporaryMultiNetwork network, Guid id);
        void BindConnecting(TemporaryMultiNetwork network, int size, Guid id);
    }
}
