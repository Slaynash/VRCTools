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
    [VRCModInfo("VRCTools", "0.6.2a", "Slaynash", "https://survival-machines.fr/vrcmod/VRCTools.dll")]
    public class VRCTools : VRCMod
    {

        private bool initialising = false;
        public static bool Initialised { get; private set; }


        private void OnApplicationStart() {

            if (!ApiCredentials.Load())
            {
                VRCModLogger.Log("No credential founds");
            }
            else
            {
                VRCModLogger.Log("Credentials:\n - Token: " + ApiCredentials.GetAuthToken() + "\n - Provider: " + ApiCredentials.GetAuthTokenProvider() + "\n - UserId: " + ApiCredentials.GetAuthTokenProviderUserId());
            }


            string lp = "";
            bool first = true;
            foreach (var lp2 in Environment.GetCommandLineArgs())
            {
                if (first) first = false;
                else lp += " " + lp2;
            }
            VRCModLogger.Log("Launch parameters:" + lp);

            ModPrefs.RegisterCategory("vrctools", "VRCTools");
            ModPrefs.RegisterPrefBool("vrctools", "avatarfavdownloadasked", false, null, true);
            ModPrefs.RegisterPrefBool("vrctools", "avatarfavdownload", false, "Enable AvatarFav Updater");

            ModPrefs.RegisterPrefBool("vrctools", "enablediscordrichpresence", true, "Enable Discord RichPresence");
            ModPrefs.RegisterPrefBool("vrctools", "enabledebugconsole", false, "Enable Debug Console");

            ModPrefs.RegisterPrefBool("vrctools", "allowdiscordjoinrequests", true, "Allow Discord join requests");
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
                VRCModLogger.Log("[VRCTools] Disabling VRCFlowManager");
                VRCFlowManagerUtils.DisableVRCFlowManager();
                VRCModLogger.Log("[VRCTools] Initialising VRCTools");
                ModManager.StartCoroutine(VRCToolsSetup());
                VRCModLogger.Log("[VRCTools] VRCToolsSetup Coroutine started");
                initialising = true;
            }
        }

        private IEnumerator VRCToolsSetup()
        {
            VRCModLogger.Log("[VRCTools] Waiting for UI Manager...");
            yield return VRCUiManagerUtils.WaitForUiManagerInit();
            VRCModLogger.Log("[VRCTools] UIManager initialised ! Resuming setup");

            VRCModLogger.Log("[VRCTools] CheckDownloadFiles");
            yield return DependenciesDownloader.CheckDownloadFiles();
            VRCModLogger.Log("[VRCTools] CheckVRCModLoaderHash");
            yield return VRCModLoaderUpdater.CheckVRCModLoaderHash();
            if (ModPrefs.GetBool("vrctools", "enablediscordrichpresence"))
            {
                VRCModLogger.Log("[VRCTools] DiscordManager Init");
                DiscordManager.Init();
            }
            VRCModLogger.Log("[VRCTools] Checking AvatarFav update");
            yield return AvatarFavUpdater.CheckForAvatarFavUpdate();

            VRCModLogger.Log("[VRCTools] VRCModNetworkStatus Setup");
            VRCModNetworkStatus.Setup();
            VRCModLogger.Log("[VRCTools] VRCModNetworkLoginPage Setup");
            try
            {
                VRCModNetworkLogin.SetupVRCModNetworkLoginPage();
            }
            catch(Exception e)
            {
                VRCModLogger.Log("Unable to setup VRCModNetworkLoginPage: " + e);
            }
            if (VRCModNetworkLogin.VrcmnwDoLogin)
            {
                VRCModLogger.Log("[VRCTools] Injecting VRCModNetwork login page");
                VRCModNetworkLogin.InjectVRCModNetworkLoginPage();
            }
            VRCModLogger.Log("[VRCTools] ModConfigPage Setup");
            ModConfigPage.Setup();
            VRCModLogger.Log("[VRCTools] ModdedUsersManager Init");
            ModdedUsersManager.Init();
            /*
            VRCUiManagerUtils.OnPageShown += (page) => {
                VRCModLogger.Log("[VRCTools] OnPageShown: " + page.screenType + " " + (string.IsNullOrEmpty(page.displayName) ? "" : page.displayName + " ") + "(" + page.GetType() + ")");
            };
            */
            VRCModLogger.Log("[VRCTools] Init done !");

            VRCModLogger.Log("[VRCTools] Connecting to VRCModNetwork");
            if (VRCModNetworkLogin.VrcmnwDoLogin)
                VRCModNetworkLogin.TryConnectToVRCModNetwork();
            while (VRCModNetworkLogin.VrcmnwDoLogin && !VRCModNetworkLogin.VrcmnwConnected)
                yield return null;
            VRCModLogger.Log("[VRCTools] Connection loop finished");
            VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();


            VRCFlowManagerUtils.EnableVRCFlowManager();

            initialising = false;
            Initialised = true;

        }





        /*
        private string GetTextFromUiInputField(UiInputField field)
        {
            foreach(FieldInfo fi in typeof(UiInputField).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (fi.FieldType == typeof(string) && fi.Name != "placeholderInputText")
                    return fi.GetValue(field) as string;
            }
            return null;
        }
        */

        private void OnUpdate()
        {
            if (!Initialised) return;
            VRCModNetworkManager.Update();
            ModdedUsersManager.Update();
            DiscordManager.Update();
        }
    }
}
