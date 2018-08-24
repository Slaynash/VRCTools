using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRCTools;

namespace VRCTools.networking.commands
{
    internal class AuthCommand : Command
    {
        public void Auth(string authToken, string apiType, string instanceId, List<ModDesc> modlist)
        {
            WriteLine(CreateLoginJson(authToken, apiType, instanceId, modlist));
        }

        public override void Handle(string parts)
        {
            if (parts.Equals("OK"))
            {
                VRCModNetworkManager.IsAuthenticated = true;
            }
            Destroy();
        }

        private static string CreateLoginJson(string authToken, string apiType, string instanceId, List<ModDesc> modlist)
        {
            return "{\"authType\":\"CLIENT\",\"authToken\":\"" + authToken + "\",\"apiType\":\"" + apiType + "\",\"instanceId\":\"" + instanceId + "\",\"modlist\":[" + ModDesc.CreateModlistJson(modlist) + "]}";
        }
    }
}
