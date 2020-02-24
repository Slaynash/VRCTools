using CCom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRCModNetwork.commands
{
    internal class ModListChangedCommand : Command
    {

        public void Send(string modListJson)
        {
            WriteLine(modListJson);
            Destroy();
        }

        public override void Handle(string parts) { }
    }
}
