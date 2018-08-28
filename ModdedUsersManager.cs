using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRCModLoader;
using VRCTools.networking;

namespace VRCTools
{
    public class ModdedUsersManager
    {

        private static Dictionary<string, ModdedUser> moddedUserList = new Dictionary<string, ModdedUser>();
        private static List<string> moddedUserListFound = new List<string>();
        private static PropertyInfo userProperty;
        private static Sprite vrctoolsSprite;
        private static bool roomCleared = true;

        internal static void Init()
        {
            VRCModNetworkManager.SetRPCListener("slaynash.vrctools.moddedplayerlistonjoin", OnModdedplayerlistonjoinReceived);
            VRCModNetworkManager.SetRPCListener("slaynash.vrctools.moddedplayerjoined", OnModdedplayerjoinReceived);
            VRCModNetworkManager.SetRPCListener("slaynash.vrctools.moddedplayerleft", OnModdedplayerleftReceived);

            userProperty = typeof(Player).GetProperties(BindingFlags.Public | BindingFlags.Instance).First((pi) => pi.PropertyType == typeof(APIUser));

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(Convert.FromBase64String(ImageDatas.VRCTOOLS_LOGO));
            vrctoolsSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        
        private static void OnModdedplayerlistonjoinReceived(string sender, string data)
        {
            ModdedUser[] muld = ModdedUser.ParseJson(data);
            lock (moddedUserList)
            {
                foreach (ModdedUser mu in muld)
                {
                    if (!moddedUserList.ContainsKey(mu.id)) moddedUserList.Add(mu.id, mu);
                }
            }
        }

        private static void OnModdedplayerjoinReceived(string sender, string data)
        {
            ModdedUser mu = JsonUtility.FromJson<ModdedUser>(data);
            lock (moddedUserList)
            {
                if (!moddedUserList.ContainsKey(mu.id)) moddedUserList.Add(mu.id, mu);
            }
        }

        private static void OnModdedplayerleftReceived(string sender, string data)
        {
            if (moddedUserList.ContainsKey(data)) moddedUserList.Remove(data);
        }

        internal static void Update()
        {
            lock (moddedUserList)
            {
                List<string> npl = new List<string>();
                if (RoomManager.currentRoom == null)
                {
                    if (!roomCleared)
                    {
                        roomCleared = true;
                        moddedUserList.Clear();
                        VRCModLogger.Log("[ModdedUsersManager] Cleared userlist");
                    }
                }
                else
                {
                    if (roomCleared)
                    {
                        roomCleared = false;
                        VRCModLogger.Log("[ModdedUsersManager] Now in instance");
                    }
                    foreach (Player p in PlayerManager.GetAllPlayers())
                    {
                        APIUser pau = userProperty.GetValue(p, null) as APIUser;
                        string pid = pau?.id;
                        if (pid != null && moddedUserList.ContainsKey(pid))
                        {
                            if (!moddedUserListFound.Contains(pid) && p.vrcPlayer != null)
                            {
                                Transform vrctplayerSprite = UnityUiUtils.DuplicateImage(p.vrcPlayer.friendSprite.transform, new Vector2(140*2, 0));
                                vrctplayerSprite.GetComponent<Image>().sprite = vrctoolsSprite;

                                VRCModLogger.Log("[ModdedUsersManager] Added VRCTools sprite to " + pid + "'s nameplate");
                            }
                            npl.Add(pid);
                        }
                    }
                }
                moddedUserListFound.Clear();
                moddedUserListFound.AddRange(npl);
            }
        }
    }
}
