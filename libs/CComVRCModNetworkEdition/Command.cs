using System;
using VRCModLoader;

namespace CCom
{
    internal abstract class Command
    {
        private Client client = null;
        private string outId = "";
        public bool Log { get; private set; }

        public abstract void Handle(string parts);

        public void WriteLine(string s)
        {
            try
            {
                if(Log) client.WriteLine(outId + " " + s);
                else client.WriteLineNoLog(outId + " " + s);
            }
            catch (Exception e)
            {
                if (Log)
                {
                    VRCModLogger.LogError(e.ToString());
                    RemoteError(e.Message);
                }
            }
        }

        public void WriteLineSecure(string s)
        {
            try
            {
                if(Log) client.WriteLineSecure(outId + " " + s);
                else client.WriteLineNoLog(outId + " " + s);
            }
            catch (Exception e)
            {
                if (Log)
                {
                    VRCModLogger.LogError(e.ToString());
                    RemoteError(e.Message);
                }
            }
        }

        internal void SetLog(bool log)
        {
            this.Log = log;
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