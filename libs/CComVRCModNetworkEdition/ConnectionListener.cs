namespace CCom
{
    internal interface IConnectionListener
    {
        void ConnectionStarted();
        void WaitingForConnection();
        void Connecting();
        void Connected();
        void ConnectionFailed(string error);
        void Disconnected(string error);
    }
}