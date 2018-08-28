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
using VRCTools.networking;

namespace VRCTools
{
    [VRCModInfo("VRCTools", "0.3-180828-0241", "Slaynash", "https://survival-machines.fr/vrcmod/VRCTools.dll")]
    public class VRCTools : VRCMod
    {

        private bool initialised = false;
        private bool initialised2 = false;
        private QuickMenu quickMenuInstance;
        
        private static Text networkstatusText;

        public void OnApplicationStart() {
            String lp = "";
            foreach(var lp2 in Environment.GetCommandLineArgs())
            {
                lp += " " + lp2;
            }
            VRCModLogger.Log("Launch parameters:" + lp);
        }

        void OnLevelWasLoaded(int level)
        {
            VRCModLogger.Log("[VRCTools] OnLevelWasLoaded " + level);
            if (level == 0 && !initialised)
            {
                VRCModLogger.Log("[VRCTools] Initialising VRCTools");
                initialised = true;
                if (VRCModLoaderUpdater.CheckVRCModLoaderHash())
                {
                    CheckForPermissions();
                }
                //ModManager.StartCoroutine(PrintVRCUiManagerHierarchy());
            }

            if (level == 1 && !initialised2)
            {
                initialised2 = true;
                VRCModLogger.Log("[VRCTools] Looking up for QuickMenu instance");
                PropertyInfo[] pil = typeof(QuickMenu).GetProperties(BindingFlags.Public | BindingFlags.Static);
                VRCModLogger.Log("[VRCTools] QuickMenu Public Static Properties: " + pil.Length);
                if (pil.Length > 0)
                {
                    PropertyInfo quickmenu = null;
                    foreach (PropertyInfo pi in pil)
                    {
                        VRCModLogger.Log("[VRCTools] - " + pi.PropertyType + " " + pi.Name);
                        if (pi.PropertyType == typeof(QuickMenu)) quickmenu = pi;
                    }

                    if (quickmenu == null)
                    {
                        VRCModLogger.Log("[VRCTools] Unable to find QuickMenu instance: No public static property returning QuickMenu found");
                    }
                    else
                    {
                        quickMenuInstance = quickmenu.GetValue(null, null) as QuickMenu;
                        if (quickMenuInstance == null)
                        {
                            VRCModLogger.Log("[VRCTools] Unable to get QuickMenu instance: instance is null");
                        }
                        else
                        {
                            //Create VRCModNetwork status
                            Transform baseTextTransform = quickMenuInstance.transform.Find("ShortcutMenu/BuildNumText");
                            if (baseTextTransform != null)
                            {
                                Transform vrcmodNetworkTransform = new GameObject("VRCModNetworkStatusText", typeof(RectTransform), typeof(Text)).transform;
                                vrcmodNetworkTransform.SetParent(baseTextTransform.parent);
                                vrcmodNetworkTransform.SetSiblingIndex(baseTextTransform.GetSiblingIndex() + 1);

                                networkstatusText = vrcmodNetworkTransform.GetComponent<Text>();
                                RectTransform networkstatusRT = vrcmodNetworkTransform.GetComponent<RectTransform>();

                                networkstatusRT.localScale = baseTextTransform.localScale;

                                networkstatusRT.anchorMin = baseTextTransform.GetComponent<RectTransform>().anchorMin;
                                networkstatusRT.anchorMax = baseTextTransform.GetComponent<RectTransform>().anchorMax;
                                networkstatusRT.anchoredPosition = baseTextTransform.GetComponent<RectTransform>().anchoredPosition;
                                networkstatusRT.sizeDelta = new Vector2(2000, baseTextTransform.GetComponent<RectTransform>().sizeDelta.y);
                                networkstatusRT.pivot = baseTextTransform.GetComponent<RectTransform>().pivot;

                                Vector3 newPos = baseTextTransform.localPosition;
                                newPos.x -= baseTextTransform.GetComponent<RectTransform>().sizeDelta.x * 0.5f;
                                newPos.x += 2000 * 0.5f;
                                newPos.y += -85;

                                networkstatusRT.localPosition = newPos;
                                VRCModLogger.Log(baseTextTransform.GetComponent<Text>().text);
                                networkstatusText.text = "VRCModNetworkStatus: <color=orange>Unknown</color>";
                                networkstatusText.color = baseTextTransform.GetComponent<Text>().color;
                                networkstatusText.font = baseTextTransform.GetComponent<Text>().font;
                                networkstatusText.fontSize = baseTextTransform.GetComponent<Text>().fontSize;
                                networkstatusText.fontStyle = baseTextTransform.GetComponent<Text>().fontStyle;

                                UpdateNetworkStatus();
                            }
                            else
                            {
                                VRCModLogger.Log("[VRCTools] QuickMenu/ShortcutMenu/BuildNumText is null");
                            }



                            //Create mods config page
                            GameObject screens = GameObject.Find("UserInterface/MenuContent/Screens");
                            GameObject avatarscreen = GameObject.Find("UserInterface/MenuContent/Screens/Avatar");
                            if (avatarscreen != null)
                            {
                                GameObject go = new GameObject("ModConfig", typeof(RectTransform), typeof(VRCUiPage));
                                go.transform.SetParent(screens.transform);
                                go.GetComponent<RectTransform>().localScale = Vector3.one;
                                go.GetComponent<RectTransform>().localRotation = Quaternion.identity;
                                go.GetComponent<RectTransform>().localPosition = Vector3.zero;
                                go.GetComponent<VRCUiPage>().screenType = avatarscreen.GetComponent<VRCUiPage>().screenType;
                                go.GetComponent<VRCUiPage>().displayName = "Mod Conf";
                                go.GetComponent<VRCUiPage>().AudioShow = avatarscreen.GetComponent<VRCUiPage>().AudioShow;
                                go.GetComponent<VRCUiPage>().AudioLoop = avatarscreen.GetComponent<VRCUiPage>().AudioLoop;
                                go.GetComponent<VRCUiPage>().AudioHide = avatarscreen.GetComponent<VRCUiPage>().AudioHide;

                                Transform scrollContent = UnityUiUtils.CreateScrollView(go.GetComponent<RectTransform>(), 1500, 1000, 0, 1000, false, true);

                                GameObject text = new GameObject("WIP", typeof(RectTransform), typeof(Text));
                                text.transform.SetParent(scrollContent);
                                text.GetComponent<RectTransform>().localScale = Vector3.one;
                                text.GetComponent<RectTransform>().localPosition = Vector3.zero;
                                text.GetComponent<RectTransform>().localRotation = Quaternion.identity;
                                text.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 500);
                                text.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                                text.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                                text.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                                text.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                                text.GetComponent<Text>().font = quickMenuInstance.transform.Find("ShortcutMenu/BuildNumText").GetComponent<Text>().font;
                                text.GetComponent<Text>().fontSize = 70;
                                text.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                                text.GetComponent<Text>().text = "This page will be available soon";
                                text.GetComponent<Text>().color = Color.yellow;

                                //SCREEN CONTENT SIZE: 1500x1000


                                // DEBUG

                                /*
                                GameObject panel = new GameObject("TestPanel", typeof(RectTransform), typeof(Image));
                                panel.transform.SetParent(go.transform);
                                panel.GetComponent<RectTransform>().localScale = Vector3.one;
                                panel.GetComponent<RectTransform>().localPosition = Vector3.zero;
                                panel.GetComponent<RectTransform>().localRotation = Quaternion.identity;
                                panel.GetComponent<RectTransform>().sizeDelta = new Vector2(1300, 1000);
                                */
                                
                                /*
                                Transform tt = go.transform; // screens.transform
                                
                                CreateDebugCube(tt, 1);
                                CreateDebugCube(tt, 20);
                                CreateDebugCube(tt, 100);
                                CreateDebugCube(tt, 200);
                                CreateDebugCube(tt, 400);
                                */
                                //PrintHierarchy(screens.transform, 0); // DEBUG
                            }
                            else
                            {
                                VRCModLogger.Log("[VRCTools] UserInterface/MenuContent/Screens/Avatar is null");
                            }


                            //Create mods config quickmenu button
                            Transform baseButtonTransform = quickMenuInstance.transform.Find("ShortcutMenu/CloseButton");
                            if (baseTextTransform != null)
                            {
                                Transform modconf = UnityUiUtils.DuplicateButton(baseButtonTransform, "Mod\nConfigs", new Vector2(-420, 0));
                                modconf.name = "ModConfigsButton";
                                modconf.GetComponentInChildren<Text>().color = new Color(1, 0.5f, 0.1f);
                                //modconf.GetComponent<Button>().interactable = false;
                                modconf.GetComponent<Button>().onClick.RemoveAllListeners();
                                modconf.GetComponent<Button>().onClick.AddListener(() =>
                                {
                                    VRCUiManagerUtils.GetVRCUiManager().ShowUi(false, true);
                                    ModManager.StartCoroutine(PlaceUiAfterPause());
                                    VRCUiManagerUtils.GetVRCUiManager().ShowScreen("UserInterface/MenuContent/Screens/ModConfig");
                                });
                            }
                            else
                            {
                                VRCModLogger.Log("[VRCTools] QuickMenu/ShortcutMenu/CloseButton is null");
                            }

                            ModdedUsersManager.Init();

                        }
                    }
                }
                else
                {
                    VRCModLogger.Log("[VRCTools] Unable to find QuickMenu instance: No public static property found");
                }
            }
        }

        //Copied from QuickMenu
        private IEnumerator PlaceUiAfterPause()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            VRCUiManagerUtils.GetVRCUiManager().PlaceUi();
            GameObject.Find("UserInterface/MenuContent/Backdrop/Header").gameObject.SetActive(false);
            yield break;
        }

        internal static void UpdateNetworkStatus()
        {
            if(networkstatusText != null)
            {
                if (VRCModNetworkManager.IsAuthenticated)
                    networkstatusText.text = "VRCModNetwork status: <color=lime>Authenticated</color>";
                else if(VRCModNetworkManager.State == ConnectionState.CONNECTED)
                    networkstatusText.text = "VRCModNetwork status: <color=orange>Not Authenticated</color>";
                else if (VRCModNetworkManager.State == ConnectionState.CONNECTING)
                    networkstatusText.text = "VRCModNetwork status: <color=orange>Connecting</color>";
                else
                    networkstatusText.text = "VRCModNetwork status: <color=red>Disconnected</color>";
            }
        }

        public void OnUpdate()
        {
            if (!initialised) return;
            VRCModNetworkManager.Update();
            ModdedUsersManager.Update();
        }



        internal static void CheckForPermissions()
        {
            if (!ModPrefs.HasKey("vrctools", "remoteauthcheck"))
            {
                VRCModLogger.Log("[VRCTools] Key remoteauthcheck not found");
                VRCFlowManagerUtils.DisableVRCFlowManager();
                ModManager.StartCoroutine(ShowAuthAgreePopup());
            }
            else if(ModPrefs.GetBool("vrctools", "remoteauthcheck"))
            {
                VRCModNetworkManager.ConnectAsync();
                VRCModLogger.Log("[VRCTools] Key remoteauthcheck found (true)");
                ModManager.StartCoroutine(AvatarFavUpdater.CheckForAvatarFavUpdate());
            }
            else
            {
                VRCModLogger.Log("[VRCTools] Key remoteauthcheck found (false)");
            }
        }

        private static IEnumerator ShowAuthAgreePopup(IEnumerator onDone = null)
        {
            yield return VRCUiManagerUtils.WaitForUiManagerInit();
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "To use the VRCTools networking features, you will need to send your auth token to the server (Required for the AvatarFav mod)", "Accept", () => {
                ModPrefs.SetBool("vrctools", "remoteauthcheck", true);
                ShowAuthChangePopup();
                VRCModNetworkManager.ConnectAsync();
            }, "Deny", () => {
                ModPrefs.SetBool("vrctools", "remoteauthcheck", false);
                ShowAuthChangePopup();
            });
        }

        private static void ShowAuthChangePopup()
        {
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "You can change this in the setting panel of VRCTools at any time (Upcoming feature)", "OK", () => {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                if(ModPrefs.GetBool("vrctools", "remoteauthcheck"))
                    ModManager.StartCoroutine(AvatarFavUpdater.CheckForAvatarFavUpdate());
                else VRCFlowManagerUtils.EnableVRCFlowManager();
            });
        }




        // DEBUG

        private void PrintHierarchy(Transform transform, int depth)
        {
            String s = "";
            for (int i = 0; i < depth; i++) s += "\t";
            s += transform.name + " [";

            MonoBehaviour[] mbs = transform.GetComponents<MonoBehaviour>();
            for (int i = 0; i < mbs.Length; i++)
            {
                if (mbs[i] == null) continue;
                if (i == 0)
                    s += mbs[i].GetType();
                else
                    s += ", " + mbs[i].GetType();
            }

            s += "]";
            VRCModLogger.Log(s);
            foreach (Transform t in transform)
            {
                if (t != null) PrintHierarchy(t, depth + 1);
            }
        }

        private void CreateDebugCube(Transform parent, int size)
        {

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "DebugCube";
            cube.transform.SetParent(parent);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = new Vector3(size, size, size);
        }
    }
}
