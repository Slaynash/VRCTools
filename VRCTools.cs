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
    [VRCModInfo("VRCTools", "0.6.5", "Slaynash", "https://survival-machines.fr/vrcmod/VRCTools.dll")]
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
            try
            {
                OculusUtils.ApplyPatches();
            }
            catch(Exception e)
            {
                VRCModLogger.Log("[VRCTools] Error while applying Oculus patches: " + e);
            }
            yield return VRCUiManagerUtils.WaitForUiManagerInit();

            VRCModLogger.Log("[VRCTools] Overwriting login button event");
            VRCUiPageAuthentication[] authpages = Resources.FindObjectsOfTypeAll<VRCUiPageAuthentication>();
            VRCUiPageAuthentication loginPage = authpages.First((page) => page.gameObject.name == "LoginUserPass");
            if (loginPage != null)
            {
                Button loginButton = loginPage.transform.Find("ButtonDone (1)")?.GetComponent<Button>();
                if (loginButton != null)
                {
                    ButtonClickedEvent bce = loginButton.onClick;
                    loginButton.onClick = new ButtonClickedEvent();
                    loginButton.onClick.AddListener(() => {
                        VRCModNetworkManager.SetCredentials(GetTextFromUiInputField(loginPage.loginUserName) + ":" + GetTextFromUiInputField(loginPage.loginPassword));
                        bce?.Invoke();
                    });
                }
                else
                    VRCModLogger.Log("[VRCTools] Unable to find login button in login page");
            }
            else
                VRCModLogger.Log("[VRCTools] Unable to find login page");

            yield return DependenciesDownloader.CheckDownloadFiles();
            yield return VRCModLoaderUpdater.CheckVRCModLoaderHash();

            if (ModPrefs.GetBool("vrctools", "enablediscordrichpresence"))
                DiscordManager.Init();

            yield return AvatarFavUpdater.CheckForAvatarFavUpdate();
            
            VRCModNetworkStatus.Setup();
            ModConfigPage.Setup();
            ModdedUsersManager.Init();

            /*
            if (ApiCredentials.Load())
            {
                VRCModLogger.Log("ApiCredentials.GetAuthTokenProviderUserId() => " + ApiCredentials.());
                if (!SecurePlayerPrefs.HasKey("vrcmnw_token_" + ApiCredentials.GetAuthTokenProviderUserId()))
                {
                    ApiCredentials.Clear();
                }
            }
            */
            ApiCredentials.Clear();


            VRCModLogger.Log("[VRCTools] Init done !");

            VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
            
            VRCFlowManagerUtils.EnableVRCFlowManager();

            initialising = false;
            Initialised = true;

            VRCModNetworkManager.ConnectAsync();

        }

        private string GetTextFromUiInputField(UiInputField field)
        {
            FieldInfo textField = typeof(UiInputField).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(f => f.FieldType == typeof(string) && f.Name != "placeholderInputText");
            return textField.GetValue(field) as string;
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
