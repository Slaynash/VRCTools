using System;
using VRCModLoader;

namespace VRCTools.networking
{
    internal abstract class Command
    {
        private Client client = null;
        private string outId = "";

        public abstract void Handle(string parts);

        public void WriteLine(string s)
        {
            try
            {
                client.WriteLine(outId + " " + s);
            }
            catch (Exception e)
            {
                VRCModLogger.LogError(e.ToString());
                RemoteError(e.Message);
            }
        }

        public void WriteLineSecure(string s)
        {
            try
            {
                client.WriteLineSecure(outId + " " + s);
            }
            catch (Exception e)
            {
                VRCModLogger.LogError(e.ToString());
                RemoteError(e.Message);
            }
        }

        public void SetClient(Client client)
        {
            this.client = client;
        }

        protected Client GetClient()
        {
            return client;
        }

        protected void Destroy()
        {
            CommandManager.Remove(this);
        }

        public void SetOutId(String outId)
        {
            this.outId = outId;
        }

        public String GetOutId()
        {
            return outId;
        }

        public virtual void RemoteError(String error)
        {
            Destroy();
        }
    }
}