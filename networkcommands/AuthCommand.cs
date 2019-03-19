using CCom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRCTools;

namespace VRCModNetwork.commands
{
    internal class AuthCommand : Command
    {
        private Action onSuccess;
        private Action<string> onError;

        public void Auth(string username, string password, string uuid, string apiType, string instanceId, string roomSecret, List<ModDesc> modlist, Action onSuccess, Action<string> onError)
        {
            this.onSuccess = onSuccess;
            this.onError = onError;
            WriteLine(CreateLoginJson(username, password, uuid, apiType, instanceId, roomSecret, modlist));
        }

        public override void Handle(string parts)
        {
            if (parts.Equals("OK"))
            {
                VRCModNetworkManager.IsAuthenticated = true;
                onSuccess?.Invoke();
            }
            else
            {
                onError(parts);
            }
            Destroy();
        }

        private static string CreateLoginJson(string username, string password, string uuid, string apiType, string instanceId, string roomSecret, List<ModDesc> modlist)
        {
            return "{\"authType\":\"CLIENT\",\"name\":\"" + username.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"password\":\"" + password.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"vrcuuid\":\"" + uuid + "\",\"apiType\":\"" + apiType + "\",\"instanceId\":\"" + instanceId + "\",\"joinSecret\":\"" + roomSecret + "\",\"modlist\":[" + ModDesc.CreateModlistJson(modlist) + "]}";
        }

        public override void RemoteError(string error)
        {
            base.RemoteError(error);
            onError(error);
        }
    }
}
