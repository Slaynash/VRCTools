using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRCModLoader;
using VRCModNetwork;

namespace VRCTools
{
    [VRCModInfo("VRCTools", "0.4-180913-0103", "Slaynash", "https://survival-machines.fr/vrcmod/VRCTools.dll")]
    public class VRCTools : VRCMod
    {

        private bool initialising = false;
        public static bool Initialised { get; private set; }
        private static bool popupClosed = false;


        private void OnApplicationStart() {
            String lp = "";
            bool first = true;
            foreach (var lp2 in Environment.GetCommandLineArgs())
            {
                if (first) first = false;
                else lp += " " + lp2;
            }
            VRCModLogger.Log("Launch parameters:" + lp);

            ModPrefs.RegisterCategory("vrctools", "VRCTools");
            ModPrefs.RegisterPrefBool("vrctools", "remoteauthcheckasked", false, null, true);
            ModPrefs.RegisterPrefBool("vrctools", "remoteauthcheck", false, "Allow VRCModNetwork Auth");
            ModPrefs.RegisterPrefBool("vrctools", "avatarfavdownloadasked", false, null, true);
            ModPrefs.RegisterPrefBool("vrctools", "avatarfavdownload", false, "Enable AvatarFav Updater");
        }

        private void OnApplicationQuit()
        {
            DiscordManager.OnApplicationQuit();
        }

        private void OnLevelWasLoaded(int level)
        {
            VRCModLogger.Log("[VRCTools] OnLevelWasLoaded " + level);
            if (level == 0 && !initialising && !Initialised)
            {
                VRCModLogger.Log("[VRCTools] Initialising VRCTools");
                VRCFlowManagerUtils.DisableVRCFlowManager();
                ModManager.StartCoroutine(VRCToolsSetup());
                initialising = true;
                
                //ModManager.StartCoroutine(PrintVRCUiManagerHierarchy());
            }
        }

        private IEnumerator VRCToolsSetup()
        {
            yield return VRCUiManagerUtils.WaitForUiManagerInit();

            yield return DependenciesDownloader.CheckDownloadFiles();
            yield return VRCModLoaderUpdater.CheckVRCModLoaderHash();
            DiscordManager.Init();
            yield return CheckForPermissions();

            RamExploitPatcher.Patch();
            VRCModNetworkStatus.Setup();
            ModConfigPage.Setup();
            ModdedUsersManager.Init();
            AvatarStealerChecker.Setup();


            VRCFlowManagerUtils.EnableVRCFlowManager();

            initialising = false;
            Initialised = true;
        }

        private void OnUpdate()
        {
            if (!Initialised) return;
            VRCModNetworkManager.Update();
            ModdedUsersManager.Update();
            DiscordManager.Update();
        }

        private void OnFixedUpdate()
        {
            if (!Initialised) return;
            AvatarStealerChecker.FixedUpdate();
        }

        private void OnLateUpdate()
        {
            if (!Initialised) return;
            AvatarStealerChecker.LateUpdate();
        }

        private void OnGUI()
        {
            if (!Initialised) return;
        }



        private static IEnumerator CheckForPermissions()
        {
            if (!ModPrefs.GetBool("vrctools", "remoteauthcheckasked"))
            {
                VRCModLogger.Log("[VRCTools] Asking for auth");
                yield return ShowAuthAgreePopup();
                ModPrefs.SetBool("vrctools", "remoteauthcheckasked", true);
            }
            if(ModPrefs.GetBool("vrctools", "remoteauthcheck"))
            {
                VRCModNetworkManager.ConnectAsync();
                VRCModLogger.Log("[VRCTools] Key remoteauthcheck found (true)");
                yield return AvatarFavUpdater.CheckForAvatarFavUpdate();
            }
            else
            {
                VRCModLogger.Log("[VRCTools] Key remoteauthcheck found (false)");
            }
        }

        private static IEnumerator ShowAuthAgreePopup(IEnumerator onDone = null)
        {
            popupClosed = false;
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "To use the VRCTools networking features, you will need to send your auth token to the server (Required for the AvatarFav mod)", "Accept", () => {
                ModPrefs.SetBool("vrctools", "remoteauthcheck", true);
                ShowAuthChangePopup();
            }, "Deny", () => {
                ModPrefs.SetBool("vrctools", "remoteauthcheck", false);
                ShowAuthChangePopup();
            });
            while (!popupClosed) yield return false;
        }

        private static void ShowAuthChangePopup()
        {
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "You can change this in the setting panel of VRCTools at any time (Upcoming feature)", "OK", () => {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                popupClosed = true;
            });
        }
    }
}
