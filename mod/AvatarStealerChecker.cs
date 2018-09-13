extern alias VRCCoreEditor;

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using VRC;
using VRC.Core;
using VRCModLoader;

namespace VRCTools
{
    internal static class AvatarStealerChecker
    {
        private static MethodInfo vrcAvatarManagerGetter;
        private static MethodInfo vrcAPIAvatarGetter;

        private static bool update = false;
        public static Dictionary<string, bool> checkedAvatars = new Dictionary<string, bool>(); // id, originalIsPrivate


        private static VRCAvatarManager GetAvatarManager(this VRCPlayer vrcPlayer) => vrcAvatarManagerGetter?.Invoke(vrcPlayer, new object[] { }) as VRCAvatarManager;
        private static ApiAvatar GetCurrentAvatar(this VRCAvatarManager vrcAvatarManager) => vrcAPIAvatarGetter?.Invoke(vrcAvatarManager, new object[] { }) as ApiAvatar;


        public static void Setup()
        {
            vrcAvatarManagerGetter = typeof(VRCPlayer).GetMethod("get_AvatarManager", BindingFlags.Public | BindingFlags.Instance);
            vrcAPIAvatarGetter = typeof(VRCAvatarManager).GetMethod("get_CurrentAvatar", BindingFlags.Public | BindingFlags.Instance);
        }

        public static void FixedUpdate()
        {
            update = true;
        }

        public static void LateUpdate()
        {
            if (update)
            {
                update = false;
                foreach (Player player in PlayerManager.GetAllPlayers())
                {
                    if (player == null) continue;
                    ApiAvatar currentAvatar = player.vrcPlayer?.GetAvatarManager()?.GetCurrentAvatar();
                    string id = currentAvatar?.id;
                    if (id != null)
                    {
                        if (checkedAvatars.TryGetValue(id, out bool originalIsPrivate))
                        {
                            if (originalIsPrivate)
                                player.vrcPlayer.SetNamePlateColor(Color.blue);
                        }
                        else
                        {
                            string blueprintId = player.gameObject.GetComponentInChildren<VRCCoreEditor::VRC.Core.PipelineManager>()?.blueprintId ?? "";
                            string authorId = currentAvatar.authorId;
                            if(!blueprintId.Equals("") && !id.Equals(blueprintId) && !checkedAvatars.ContainsKey(id))
                            {
                                checkedAvatars.Add(id, false);
                                ModManager.StartCoroutine(CheckAvatarOriginalReleaseStatus(blueprintId, id, authorId));
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerator CheckAvatarOriginalReleaseStatus(string blueprintId, string id, string authorId)
        {
            VRCModLogger.Log("[AvatarStealerChecker] Checking avatar " + blueprintId);
            using (WWW avtrRequest = new WWW(API.GetApiUrl() + "avatars/" + blueprintId + "?apiKey=" + API.ApiKey))
            {
                yield return avtrRequest;
                int rc = WebRequestsUtils.GetResponseCode(avtrRequest);
                if (rc == 200)
                {
                    try
                    {
                        VRCModLogger.Log("[AvatarStealerChecker] " + avtrRequest.text);
                        SerializableApiAvatar aa = JsonConvert.DeserializeObject<SerializableApiAvatar>(avtrRequest.text);
                        if (!aa.releaseStatus.Equals("public") && !aa.authorId.Equals(authorId))
                        {
                            VRCModLogger.Log("[AvatarStealerChecker] Avatar " + id + " is a private stealed avatar ! (" + blueprintId + ")");
                            checkedAvatars[id] = true;
                        }
                    }
                    catch (Exception e)
                    {
                        VRCModLogger.LogError("[AvatarStealerChecker] " + e.ToString());
                    }
                }
            }
        }

        private class SerializableApiAvatar
        {
            public string id;
            public string authorId;
            public string releaseStatus;
        }
    }
}
