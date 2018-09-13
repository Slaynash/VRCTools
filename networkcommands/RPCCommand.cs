using CComVRCModNetworkEdition;
using System;
using VRCModLoader;

namespace VRCModNetwork.commands
{
    internal class RPCCommand : Command
    {
        private string rpcId;
        private Action onSuccess;
        private Action<string> onError;

        public override void Handle(string parts)
        {
            Destroy();
            if (!parts.Equals("OK"))
            {
                string[] rpcparts = parts.Split(new char[] { ' ' }, 3);
                VRCModNetworkManager.HandleRpc(rpcparts[0], rpcparts[1], rpcparts[2]);
                WriteLine("OK");
            }
            else
            {
                onSuccess?.Invoke();
            }
        }

        internal void SendCommand(string rpcId, string rpcData, Action onSuccess, Action<string> onError)
        {
            this.rpcId = rpcId;
            this.onSuccess = onSuccess;
            this.onError = onError;
            WriteLine(rpcId + " 0 " + rpcData);
        }

        internal void SendCommand(string rpcId, string targetId, string rpcData, Action onSuccess, Action<string> onError)
        {
            this.rpcId = rpcId;
            this.onSuccess = onSuccess;
            this.onError = onError;
            WriteLine(rpcId + " 1 " + targetId + " " + rpcData);
        }

        public override void RemoteError(string error)
        {
            base.RemoteError(error);
            VRCModLogger.LogError("[RPCCommand] Server returned error for RPC " + rpcId + " : " + error);
            onError?.Invoke(error);
        }
    }
}