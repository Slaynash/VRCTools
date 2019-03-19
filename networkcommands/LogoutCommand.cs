using CCom;
using System;

namespace VRCModNetwork.commands
{
    internal class LogoutCommand : Command
    {
        public void LogOut()
        {
            WriteLine("");
            Destroy();
        }

        public override void Handle(string parts) {
            VRCModNetworkManager.IsAuthenticated = false;
        }
    }
}