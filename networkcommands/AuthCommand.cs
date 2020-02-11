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
        private string id = null;

        public void Auth(string id, string authToken, string apiType, string instanceId, string roomSecret, List<ModDesc> modlist)
        {
            VRCModLogger.Log("Authdata:" + authToken);
            WriteLine(CreateLoginJson(authToken, apiType, instanceId, roomSecret, modlist));
            this.id = id;
        }

        public override void Handle(string parts)
        {
            if (parts.StartsWith("OK"))
            {
                VRCModNetworkManager.SheduleForMainThread(() =>
                {
                    VRCModNetworkManager.IsAuthenticated = true;
                    if (!parts.Equals("OK"))
                    {
                        string token = parts.Substring(3);
                        SecurePlayerPrefs.SetString("vrcmnw_token_" + id, token, "vl9u1grTnvXA");
                    }
                });
            }
            Destroy();
        }

        private static string CreateLoginJson(string authToken, string apiType, string instanceId, string roomSecret, List<ModDesc> modlist)
        {
            return "{\"authType\":\"CLIENT\",\"authToken\":\"" + authToken + "\",\"apiType\":\"" + apiType + "\",\"instanceId\":\"" + instanceId + "\",\"joinSecret\":\"" + roomSecret + "\",\"modlist\":[" + ModDesc.CreateModlistJson(modlist) + "]}";
        }

        public override void RemoteError(string error)
        {
            base.RemoteError(error);

            if(id != null)
            {
                VRCModNetworkManager.SheduleForMainThread(() =>
                {
                    if (SecurePlayerPrefs.HasKey("vrcmnw_token_" + id))
                    {
                        SecurePlayerPrefs.DeleteKey("vrcmnw_token_" + id);
                        VRCModNetworkManager.userUuid = "";
                    }
                });
            }
            VRCModNetworkManager.authError = error;
        }
    }
}
