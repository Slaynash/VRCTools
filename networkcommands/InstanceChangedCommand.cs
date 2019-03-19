using CCom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRCModNetwork.commands
{
    internal class InstanceChangedCommand : Command
    {

        public void Send(string instanceId, string roomSecret)
        {
            WriteLine(instanceId + (!string.IsNullOrEmpty(roomSecret) ? (" " + roomSecret) : ""));
            Destroy();
        }

        public override void Handle(string parts) { }
    }
}
