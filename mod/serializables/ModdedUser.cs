using Newtonsoft.Json;

namespace VRCTools
{
    internal class ModdedUser
    {
        public string vrctid;
        public string vrcid;
        internal ModDesc[] mods;

        internal static ModdedUser[] ParseJson(string json)
        {
            return JsonConvert.DeserializeObject<ModdedUserList>(json).list;
        }
    }

    internal class ModdedUserList
    {
        public ModdedUser[] list;
    }
}