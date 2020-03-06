using CCom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRCModLoader;
using VRCTools;

namespace VRCModNetwork.commands
{
    internal class AuthCommand : Command
    {
        private Action onSuccess;
        private Action<string> onError;

        internal void Auth(string username, string password, string uuid, string instanceId, string roomSecret, List<ModDesc> modlist, Action onSuccess, Action<string> onError)
        {
            this.onSuccess = onSuccess;
            this.onError = onError;
            WriteLineSecure(CreateLoginJson(username, password, uuid, instanceId, roomSecret, modlist));
        }

        public override void Handle(string parts)
        {
            Destroy();
            if (parts.StartsWith("OK"))
            {
                VRCModNetworkManager.SheduleForMainThread(() =>
                {
                    int returnValue = int.Parse(parts.Split(new[] { ' ' }, 2)[1]);
                    VRCModNetworkManager.IsAuthenticated = true;
                    
                    VRCModNetworkManager.VRCAuthStatus = returnValue;
                    onSuccess?.Invoke();
                });
            }
            else
                onError(parts);
        }

        private static string CreateLoginJson(string username, string password, string uuid, string instanceId, string roomSecret, List<ModDesc> modlist)
        {
            return "{\"authType\":\"CLIENT\",\"name\":\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(username)) + "\",\"password\":\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(password)) + "\",\"vrcuuid\":\"" + uuid + "\",\"instanceId\":\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(instanceId)) + "\",\"joinSecret\":\"" + roomSecret + "\",\"modlist\":[" + ModDesc.CreateModlistJson(modlist) + "]}";
        }

        public override void RemoteError(string error)
        {
            base.RemoteError(error);
            onError(error);
        }
    }
}
