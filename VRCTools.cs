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
using static UnityEngine.UI.Button;

namespace VRCTools
{
    [VRCModInfo("VRCTools", "0.5.2", "Slaynash", "https://survival-machines.fr/vrcmod/VRCTools.dll")]
    public class VRCTools : VRCMod
    {

        private bool initialising = false;
        public static bool Initialised { get; private set; }
        private static bool popupClosed = false;


        private void OnApplicationStart() {

            if (!ApiCredentials.Load())
            {
                VRCModLogger.Log("No credential founds");
            }
            else
            {
                VRCModLogger.Log("Credentials:\n - Token: " + ApiCredentials.GetAuthToken() + "\n - Provider: " + ApiCredentials.GetAuthTokenProvider() + "\n - UserId: " + ApiCredentials.GetAuthTokenProviderUserId());
            }


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

            ModPrefs.RegisterPrefBool("vrctools", "enablediscordrichpresence", true, "Enable Discord RichPresence");
            ModPrefs.RegisterPrefBool("vrctools", "enabledebugconsole", false, "Enable Debug Console");

            ModPrefs.RegisterPrefBool("vrctools", "hasvrcmnwtoken", false, null, true);

            ModPrefs.RegisterPrefBool("vrctools", "allowdiscordjoinrequests", true, "Allow Discord join requests");

            //Reset the credentials to ask login again if this is the first time the user login to the VRCMNW
            if (!ModPrefs.GetBool("vrctools", "hasvrcmnwtoken"))
                ApiCredentials.Clear();
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

            VRCModLogger.Log("[VRCTools] CheckDownloadFiles");
            yield return DependenciesDownloader.CheckDownloadFiles();
            VRCModLogger.Log("[VRCTools] CheckVRCModLoaderHash");
            yield return VRCModLoaderUpdater.CheckVRCModLoaderHash();
            if (ModPrefs.GetBool("vrctools", "enablediscordrichpresence"))
            {
                VRCModLogger.Log("[VRCTools] DiscordManager Init");
                DiscordManager.Init();
            }
            VRCModLogger.Log("[VRCTools] CheckForPermissions");
            yield return CheckForPermissions();

            VRCModLogger.Log("[VRCTools] VRCModNetworkStatus Setup");
            VRCModNetworkStatus.Setup();
            VRCModLogger.Log("[VRCTools] ModConfigPage Setup");
            ModConfigPage.Setup();
            VRCModLogger.Log("[VRCTools] ModdedUsersManager Init");
            ModdedUsersManager.Init();

            VRCUiManagerUtils.OnPageShown += (page) => {
                VRCModLogger.Log("[VRCTools] OnPageShown: " + page.screenType + " " + (string.IsNullOrEmpty(page.displayName) ? "" : page.displayName + " ") + "(" + page.GetType() + ")");
            };

            VRCModLogger.Log("[VRCTools] Init done !");

            VRCFlowManagerUtils.EnableVRCFlowManager();

            initialising = false;
            Initialised = true;

            //DebugUtils.PrintHierarchy(VRCUiManagerUtils.GetVRCUiManager().transform.root, 0);

        }

        private string GetTextFromUiInputField(UiInputField field)
        {
            foreach(FieldInfo fi in typeof(UiInputField).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (fi.FieldType == typeof(string) && fi.Name != "placeholderInputText")
                    return fi.GetValue(field) as string;
            }
            return null;
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
        }

        private void OnLateUpdate()
        {
            if (!Initialised) return;
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
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "To use the VRCModNetwork, you need to accept sending your VRChat credentials to the server (Required for the AvatarFav mod)", "Accept", () => {
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
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "You can change this in the Mods Config page at any time", "OK", () => {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                popupClosed = true;
            });
        }
    }
}
