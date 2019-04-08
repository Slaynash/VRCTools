using Harmony;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.Core.BestHTTP;
using VRCModLoader;
using VRCModNetwork;
using static UnityEngine.UI.Button;

namespace VRCTools
{
    [VRCModInfo("VRCTools", "0.6.3", "Slaynash", "https://survival-machines.fr/vrcmod/VRCTools.dll")]
    public class VRCTools : VRCMod
    {

        private bool initialising = false;
        public static bool Initialised { get; private set; }


        private void OnApplicationStart() {
            
            string lp = "";
            bool first = true;
            foreach (var lp2 in Environment.GetCommandLineArgs())
            {
                if (first) first = false;
                else lp += " " + lp2;
            }
            VRCModLogger.Log("[VRCTools] Launch parameters:" + lp);

            ModPrefs.RegisterCategory("vrctools", "VRCTools");
            ModPrefs.RegisterPrefBool("vrctools", "avatarfavdownloadasked", false, null, true);
            ModPrefs.RegisterPrefBool("vrctools", "avatarfavdownload", false, "Enable AvatarFav Updater");

            ModPrefs.RegisterPrefBool("vrctools", "enablediscordrichpresence", true, "Enable Discord RichPresence");
            ModPrefs.RegisterPrefBool("vrctools", "enabledebugconsole", false, "Enable Debug Console");

            ModPrefs.RegisterPrefBool("vrctools", "allowdiscordjoinrequests", true, "Allow Discord join requests");
        }

        private void OnLevelWasLoaded(int level)
        {
            if (level == 0 && !initialising && !Initialised)
            {
                VRCFlowManagerUtils.DisableVRCFlowManager();
                ModManager.StartCoroutine(VRCToolsSetup());
            }
        }

        private IEnumerator VRCToolsSetup()
        {
            initialising = true;
            VRCModLogger.Log("[VRCTools] Initialising VRCTools");
            yield return VRCUiManagerUtils.WaitForUiManagerInit();
            
            yield return DependenciesDownloader.CheckDownloadFiles();
            yield return VRCModLoaderUpdater.CheckVRCModLoaderHash();

            if (ModPrefs.GetBool("vrctools", "enablediscordrichpresence"))
                DiscordManager.Init();

            yield return AvatarFavUpdater.CheckForAvatarFavUpdate();
            
            VRCModNetworkStatus.Setup();
            try {
                VRCModNetworkLogin.SetupVRCModNetworkLoginPage();
            } catch(Exception e) {
                VRCModLogger.Log("Unable to setup VRCModNetworkLoginPage: " + e);
                yield break;
            }

            if (VRCModNetworkLogin.VrcmnwDoLogin)
                VRCModNetworkLogin.InjectVRCModNetworkLoginPage();
            ModConfigPage.Setup();
            ModdedUsersManager.Init();

            VRCModLogger.Log("[VRCTools] Init done !");

            while (VRCModNetworkLogin.VrcmnwDoLogin && !VRCModNetworkLogin.VrcmnwConnected)
            {
                VRCModLogger.Log("[VRCTools] Trying to connect to the VRCModNetwork");
                yield return VRCModNetworkLogin.TryConnectToVRCModNetwork();
            }

            VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
            
            VRCFlowManagerUtils.EnableVRCFlowManager();

            initialising = false;
            Initialised = true;

        }

        

        private void OnUpdate()
        {
            if (!Initialised) return;
            VRCModNetworkManager.Update();
            VRCModNetworkStatus.Update();
            ModdedUsersManager.Update();
            DiscordManager.Update();
        }

        private void OnApplicationQuit()
        {
            DiscordManager.OnApplicationQuit();
        }
    }
}
