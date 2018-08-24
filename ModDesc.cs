using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VRCModLoader;

namespace VRCTools
{
    internal class ModDesc
    {
        public string name;
        public string version;
        public string author;
        public string downloadLink;
        public string baseClass;

        public ModDesc(string name, string version, string author, string downloadLink, string baseClass)
        {
            this.name = name;
            this.version = version;
            this.author = author;
            this.downloadLink = downloadLink;
            this.baseClass = baseClass;
        }

        public bool Equals(ModDesc modDesc)
        {
            return name.Equals(modDesc.name) && version.Equals(modDesc.version) && baseClass.Equals(modDesc.baseClass);
        }


        public static List<ModDesc> GetAllMods()
        {
            List<ModDesc> list = new List<ModDesc>();
            foreach (VRCMod mod in ModManager.Mods) list.Add(new ModDesc(mod.Name, mod.Version, mod.Author, mod.DownloadLink ?? "", "VRCMod"));
            Type vrmoduleType = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if ((vrmoduleType = a.GetType("VRLoader.Modules.VRModule")) != null)
                {
                    break;
                }
            }
            if (vrmoduleType != null)
            {
                foreach (UnityEngine.Object vrmodule in Resources.FindObjectsOfTypeAll(vrmoduleType))
                {
                    //TODO Get Name, Version from object
                    PropertyInfo nameProperty = vrmodule.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    string name = nameProperty.GetValue(vrmodule, null) as string ?? vrmodule.GetType().Name;
                    PropertyInfo versionProperty = vrmodule.GetType().GetProperty("Version", BindingFlags.Public | BindingFlags.Instance);
                    string version = versionProperty.GetValue(vrmodule, null) as string ?? "?";
                    PropertyInfo authorProperty = vrmodule.GetType().GetProperty("Author", BindingFlags.Public | BindingFlags.Instance);
                    string author = authorProperty.GetValue(vrmodule, null) as string ?? "?";
                    list.Add(new ModDesc(name, version, author, "", "VRModule"));
                }
            }
            return list;
        }

        public static string CreateModlistJson(List<ModDesc> modlist)
        {
            string modListString = "";
            bool firstdone = false;
            foreach (ModDesc mod in modlist)
            {
                if (firstdone) modListString += ",";
                else firstdone = true;

                modListString += "{\"name\":\"" + ParseJsonString(mod.name) + "\",\"version\":\"" + ParseJsonString(mod.version) + "\",\"author\":\"" + ParseJsonString(mod.author) + "\",\"downloadLink\":\"" + ParseJsonString(mod.downloadLink) + "\",\"type\":\"" + ParseJsonString(mod.baseClass) + "\"}";
            }
            return modListString;
        }

        private static string ParseJsonString(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

}