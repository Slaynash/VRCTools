using CCom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRCModNetwork.commands
{
    internal class InstanceChangedCommand : Command
    {

        public void Send(string instanceId)
        {
            WriteLine(instanceId);
            Destroy();
        }

        public override void Handle(string parts) { }
    }
}
