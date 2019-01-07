using CComVRCModNetworkEdition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRCTools;

namespace VRCModNetwork.commands
{
    internal class AuthCommand : Command
    {
        public void Auth(string authToken, string apiType, string instanceId, string roomSecret, List<ModDesc> modlist)
        {
            WriteLine(CreateLoginJson(authToken, apiType, instanceId, roomSecret, modlist));
        }

        public override void Handle(string parts)
        {
            if (parts.Equals("OK"))
            {
                VRCModNetworkManager.IsAuthenticated = true;
            }
            Destroy();
        }

        private static string CreateLoginJson(string authToken, string apiType, string instanceId, string roomSecret, List<ModDesc> modlist)
        {
            return "{\"authType\":\"CLIENT\",\"authToken\":\"" + authToken + "\",\"apiType\":\"" + apiType + "\",\"instanceId\":\"" + instanceId + "\",\"joinSecret\":\"" + roomSecret + "\",\"modlist\":[" + ModDesc.CreateModlistJson(modlist) + "]}";
        }
    }
}
