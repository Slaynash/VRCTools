using CCom;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRCModLoader;
using VRCTools;

namespace VRCModNetwork.commands
{
    internal class VRCLinkCommand : Command
    {
        private Action onSuccess;
        private Action<string> onError;

        internal void LinkSteam(string uuid, string token, Action onSuccess, Action<string> onError)
        {
            this.onSuccess = onSuccess;
            this.onError = onError;
            WriteLine(CreateLoginJson("steam", token, uuid));
        }

        internal void LinkCrendentials(string uuid, string crendentials, Action onSuccess, Action<string> onError)
        {
            this.onSuccess = onSuccess;
            this.onError = onError;
            WriteLine(CreateLoginJson("credentials", crendentials, uuid));
        }

        public override void Handle(string parts)
        {
            Destroy();
            if (parts.StartsWith("OK"))
            {
                VRCModNetworkManager.SheduleForMainThread(() =>
                {
                    onSuccess?.Invoke();
                });
            }
            else
                onError(parts);
        }

        private static string CreateLoginJson(string type, string data, string uuid)
        {
            VRCAuthData ad = new VRCAuthData();
            ad.type = type;
            ad.data = data;
            ad.uuid = uuid;
            return JsonConvert.SerializeObject(ad);
        }

        public override void RemoteError(string error)
        {
            base.RemoteError(error);
            onError(error);
        }

        private class VRCAuthData
        {
            public string type;
            public string data;
            public string uuid;
        }
    }
}
