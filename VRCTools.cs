using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using VRCModLoader;
using VRCModNetwork;
using static UnityEngine.UI.Button;

using UnityEngine.SceneManagement;

namespace VRCTools
{
    [VRCModInfo("VRCTools", "0.8.3", "Slaynash")]
    public class VRCTools : VRCMod
    {
        private bool usingVRCMenuUtils = false;

        public static bool Initialised { get; private set; }


        private void OnApplicationStart() {
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                string lp = "";
                bool first = true;
                foreach (var lp2 in Environment.GetCommandLineArgs())
                {
                    if (first) first = false;
                    else lp += " " + lp2;
                }
                VRCModLogger.Log("[VRCTools] Launch parameters:" + lp);
            }

            ModPrefs.RegisterCategory("vrctools", "VRCTools");
            ModPrefs.RegisterPrefBool("vrctools", "enabledebugconsole", false, "Enable Debug Console");

            Type vrcMenuUtilsAPI = null;
            usingVRCMenuUtils = AppDomain.CurrentDomain.GetAssemblies().Any(a =>
            {
                vrcMenuUtilsAPI = a.GetType("VRCMenuUtils.VRCMenuUtilsAPI");
                return vrcMenuUtilsAPI != null;
            });

            VRCModLogger.Log("[VRCTools] Using VRCMenuUtils: " + usingVRCMenuUtils);

            /*if (!usingVRCMenuUtils)
            {
                SceneManager.sceneLoaded += (scene, mode) =>
                {
                    if(scene.buildIndex == 0 && !Initialised)
                        ModManager.StartCoroutine(VRCToolsSetup());
                };
            }*/
            if (usingVRCMenuUtils)
            {
                vrcMenuUtilsAPI.GetMethod("RunBeforeFlowManager").Invoke(null, new object[] { VRCToolsSetup() });
            }
        }

        private void OnLevelWasLoaded(int level)
        {
            if (!usingVRCMenuUtils && level == (Application.platform == RuntimePlatform.WindowsPlayer ? 0 : 2) && !Initialised)
            {
                VRCFlowManagerUtils.DisableVRCFlowManager();
                ModManager.StartCoroutine(VRCToolsSetup());
            }
        }

        private IEnumerator VRCToolsSetup()
        {
            VRCModLogger.Log("[VRCTools] Initialising VRCTools");

            yield return VRCUiManagerUtils.WaitForUiManagerInit();

            VRCModLogger.Log("[VRCTools] Overwriting login button event");
            VRCUiPageAuthentication loginPage = Resources.FindObjectsOfTypeAll<VRCUiPageAuthentication>().FirstOrDefault((page) => page.gameObject.name == "LoginUserPass");
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

            yield return VRCModLoaderUpdater.CheckVRCModLoaderHash();

            try
            {
                VRCModNetworkStatus.Setup();
                ModConfigPage.Setup();
                ModdedUsersManager.Init();
            } catch (Exception ex)
            {
                VRCModLogger.Log("[VRCTools]" + ex.ToString());
            }
            VRCModLogger.Log("[VRCTools] Init done !");

            VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();

            Initialised = true;

            if (!usingVRCMenuUtils)
                VRCFlowManagerUtils.EnableVRCFlowManager();

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
        }
    }
}
