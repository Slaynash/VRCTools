using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRCModLoader;

namespace VRCTools
{
    public static class Values
    {
        public static string VRCModLoaderAssemblyPath { get { return typeof(ModManager).Assembly.Location; } }
        public static string GamePath { get { return Environment.CurrentDirectory; } }
        public static string VRCToolsDependenciesPath { get { return GamePath + "\\VRCTools\\Dependencies\\"; } }
        public static string ModsPath { get { return GamePath + "\\Mods\\"; } }
    }
}
