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
    [VRCModInfo("VRCTools", "0.2-180821-0219", "Slaynash", "https://survival-machines.fr/vrcmod/VRCTools.dll")]
    public class VRCTools : VRCMod
    {

        private bool initialised = false;
        private bool initialised2 = false;
        private QuickMenu quickMenuInstance;

        private Image downloadProgressFillImage;
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
                if (CheckVRCModLoaderHash())
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

                                GameObject text = new GameObject("WIP", typeof(RectTransform), typeof(Text));
                                text.transform.SetParent(go.transform);
                                text.GetComponent<RectTransform>().localScale = Vector3.one;
                                text.GetComponent<RectTransform>().localPosition = Vector3.zero;
                                text.GetComponent<RectTransform>().localRotation = Quaternion.identity;
                                text.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 500);
                                text.GetComponent<Text>().font = quickMenuInstance.transform.Find("ShortcutMenu/BuildNumText").GetComponent<Text>().font;
                                text.GetComponent<Text>().fontSize = 70;
                                text.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                                text.GetComponent<Text>().text = "This page will be available soon";
                                text.GetComponent<Text>().color = Color.yellow;

                                //SCREEN SIZE: 1540x1040
                                //OPTIMAL SCREEN CONTENT SIZE: 1300x1000


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
                                //PrintHierarchy(quickMenuInstance.transform, 0); // DEBUG
                            }
                            else
                            {
                                VRCModLogger.Log("[VRCTools] UserInterface/MenuContent/Screens/Avatar is null");
                            }


                            //Create mods config quickmenu button
                            Transform baseButtonTransform = quickMenuInstance.transform.Find("ShortcutMenu/CloseButton");
                            if (baseTextTransform != null)
                            {
                                Transform modconf = DuplicateButton(baseButtonTransform, "Mod\nConfigs", new Vector2(-420, 0));
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

                        }
                    }
                }
                else
                {
                    VRCModLogger.Log("[VRCTools] Unable to find QuickMenu instance: No public static property found");
                }
            }
        }

        private void CreateDebugCube(Transform transform, int s)
        {

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "DebugCube";
            cube.transform.SetParent(transform);
            cube.transform.position = transform.position;
            cube.transform.rotation = transform.rotation;
            cube.transform.localScale = new Vector3(s, s, s);
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

        private bool CheckVRCModLoaderHash()
        {
            string vrcmodloaderPath = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) + "\\VRChat_Data\\Managed\\VRCModLoader.dll";
            if (!File.Exists(vrcmodloaderPath)) return true;
            string fileHash = "";
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(vrcmodloaderPath))
                {
                    var hash = md5.ComputeHash(stream);
                    fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            VRCModLogger.Log("[VRCToolsUpdater] Local VRCModLoader file hash: " + fileHash);

            WWW hashCheckWWW = new WWW("https://download2.survival-machines.fr/vrcmodloader/VRCModLoaderHashCheck.php?localhash=" + fileHash);
            while (!hashCheckWWW.isDone) ;
            int responseCode = getResponseCode(hashCheckWWW);
            VRCModLogger.Log("[VRCToolsUpdater] hash check webpage returned [" + responseCode + "] \"" + hashCheckWWW.text + "\"");
            if (responseCode != 200)
            {
                return true;
            }
            else if (hashCheckWWW.text.Equals("OUTOFDATE"))
            {
                VRCModLogger.Log("[VRCTools] Key remoteauthcheck not found");
                VRCFlowManagerUtils.DisableVRCFlowManager();
                ModManager.StartCoroutine(ShowVRCModLoaderUpdatePopup());
                return false;
            }
            else
            {
                return true;
            }
        }

        private IEnumerator ShowVRCModLoaderUpdatePopup()
        {
            yield return VRCUiManagerUtils.WaitForUiManagerInit();
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "A VRCModLoader update is available. You can install it using the installer (more info on the VRCTools website)", "OK", () =>
            {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                CheckForPermissions();
            });
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

        private void PrintHierarchy(Transform transform, int depth)
        {
            String s = "";
            for (int i = 0; i < depth; i++) s += "\t";
            VRCModLogger.Log(s + transform.name);
            foreach(Transform t in transform)
            {
                if(t != null) PrintHierarchy(t, depth + 1);
            }
        }

        public void OnUpdate()
        {
            if (!initialised) return;
            VRCModNetworkManager.Update();
        }



        private void CheckForPermissions()
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
                ModManager.StartCoroutine(CheckForAvatarFavUpdate());
            }
            else
            {
                VRCModLogger.Log("[VRCTools] Key remoteauthcheck found (false)");
            }
        }

        private IEnumerator ShowAuthAgreePopup(IEnumerator onDone = null)
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

        private void ShowAuthChangePopup()
        {
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "You can change this in the setting panel of VRCTools at any time (Upcoming feature)", "OK", () => {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                if(ModPrefs.GetBool("vrctools", "remoteauthcheck"))
                    ModManager.StartCoroutine(CheckForAvatarFavUpdate());
                else VRCFlowManagerUtils.EnableVRCFlowManager();
            });
        }

        private IEnumerator CheckForAvatarFavUpdate()
        {
            VRCFlowManagerUtils.DisableVRCFlowManager();
            yield return VRCUiManagerUtils.WaitForUiManagerInit();
            string avatarfavPath = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) + "\\Mods\\AvatarFav.dll";
            VRCModLogger.Log("AvatarFav.dll path: " + avatarfavPath);
            string fileHash = "";
            if (ModPrefs.HasKey("vrctools", "avatarfavdownload"))
            {
                VRCModLogger.Log("vrctools.avatarfavdownload: " + ModPrefs.GetBool("vrctools", "avatarfavdownload"));
                if (ModPrefs.GetBool("vrctools", "avatarfavdownload"))
                {
                    if (File.Exists(avatarfavPath))
                    {
                        using (var md5 = MD5.Create())
                        {
                            using (var stream = File.OpenRead(avatarfavPath))
                            {
                                var hash = md5.ComputeHash(stream);
                                fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                            }
                        }
                        VRCModLogger.Log("[VRCToolsUpdater] Local AvatarFav file hash: " + fileHash);

                        WWW hashCheckWWW = new WWW("https://vrchat.survival-machines.fr/vrcmod/AvatarFavHashCheck.php?localhash=" + fileHash);
                        while (!hashCheckWWW.isDone) ;
                        int responseCode = getResponseCode(hashCheckWWW);
                        VRCModLogger.Log("[VRCToolsUpdater] hash check webpage returned [" + responseCode + "] \"" + hashCheckWWW.text + "\"");
                        if (responseCode != 200)
                        {
                            VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Unable to check AvatarFav file hash", "OK", () => VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup());
                        }
                        else if (hashCheckWWW.text.Equals("OUTOFDATE"))
                        {
                            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "An AvatarFav update is available", "Update", () =>
                            {
                                ModManager.StartCoroutine(DownloadAvatarFav(avatarfavPath));
                            }, "Ignore", () =>
                            {
                                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                                VRCFlowManagerUtils.EnableVRCFlowManager();
                            });
                        }
                        else
                        {
                            VRCFlowManagerUtils.EnableVRCFlowManager();
                        }
                    }
                    else
                    {
                        VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Do you want to install the AvatarFav mod ?", "Accept", () => {
                            ModPrefs.SetBool("vrctools", "avatarfavdownload", true);
                            ModManager.StartCoroutine(DownloadAvatarFav(avatarfavPath));
                        }, "Deny", () => {
                            ModPrefs.SetBool("vrctools", "avatarfavdownload", false);
                            VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                            VRCFlowManagerUtils.EnableVRCFlowManager();
                        });
                    }
                }
            }
            else
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Do you want to install the AvatarFav mod ?", "Accept", () => {
                    ModPrefs.SetBool("vrctools", "avatarfavdownload", true);
                    ModManager.StartCoroutine(DownloadAvatarFav(avatarfavPath));
                }, "Deny", () => {
                    ModPrefs.SetBool("vrctools", "avatarfavdownload", false);
                    VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                    VRCFlowManagerUtils.EnableVRCFlowManager();
                });
            }
        }






        private IEnumerator DownloadAvatarFav(string avatarfavPath)
        {
            VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Updating AvatarFav", "Quit", () => Application.Quit(), (popup) => {
                if (popup.popupProgressFillImage != null)
                {
                    popup.popupProgressFillImage.enabled = true;
                    popup.popupProgressFillImage.fillAmount = 0f;
                    downloadProgressFillImage = popup.popupProgressFillImage;
                }
            });


            WWW vrctoolsDownload = new WWW("https://vrchat.survival-machines.fr/vrcmod/AvatarFav.dll");
            yield return vrctoolsDownload;
            while (!vrctoolsDownload.isDone)
            {
                VRCModLogger.Log("[AvatarFavUpdater] Download progress: " + vrctoolsDownload.progress);
                downloadProgressFillImage.fillAmount = vrctoolsDownload.progress;
                yield return null;
            }

            int responseCode = getResponseCode(vrctoolsDownload);
            VRCModLogger.Log("[AvatarFavUpdater] Download done ! response code: " + responseCode);
            VRCModLogger.Log("[AvatarFavUpdater] File size: " + vrctoolsDownload.bytes.Length);

            if (responseCode == 200)
            {
                VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Saving AvatarFav");
                VRCModLogger.Log("[AvatarFavUpdater] Saving file");
                File.WriteAllBytes(avatarfavPath, vrctoolsDownload.bytes);

                VRCModLogger.Log("[AvatarFavUpdater] Showing restart dialog");
                bool choiceDone = false;
                VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Update downloaded", "Restart", () => {
                    choiceDone = true;
                });
                yield return new WaitUntil(() => choiceDone);

                VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Restarting game");
                string args = "";
                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    args = args + arg + " ";
                }
                VRCModLogger.Log("[AvatarFavUpdater] Rebooting game with args " + args);

                Thread t = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    System.Diagnostics.Process.Start(Path.GetDirectoryName(Path.GetDirectoryName(avatarfavPath)) + "\\VRChat.exe", args);
                    Thread.Sleep(100);
                });
                t.Start();

                Application.Quit();
            }
            else
            {
                VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Unable to update VRCTools: Server returned code " + responseCode, "Quit", () => Application.Quit());
            }
        }



        public static int getResponseCode(WWW request)
        {
            int ret = 0;
            if (request.responseHeaders == null)
            {
                Debug.LogError("no response headers.");
            }
            else
            {
                if (!request.responseHeaders.ContainsKey("STATUS"))
                {
                    Debug.LogError("response headers has no STATUS.");
                }
                else
                {
                    ret = parseResponseCode(request.responseHeaders["STATUS"]);
                }
            }

            return ret;
        }

        public static int parseResponseCode(string statusLine)
        {
            int ret = 0;

            string[] components = statusLine.Split(' ');
            if (components.Length < 3)
            {
                Debug.LogError("invalid response status: " + statusLine);
            }
            else
            {
                if (!int.TryParse(components[1], out ret))
                {
                    Debug.LogError("invalid response code: " + components[1]);
                }
            }

            return ret;
        }











        public static Transform DuplicateButton(Transform baseButton, string buttonText, Vector2 posDelta)
        {
            GameObject buttonGO = new GameObject("DuplicatedButton", new Type[] {
                typeof(Button),
                typeof(Image)
            });

            RectTransform rtO = baseButton.GetComponent<RectTransform>();
            RectTransform rtT = buttonGO.GetComponent<RectTransform>();

            buttonGO.transform.SetParent(baseButton.parent);
            buttonGO.GetComponent<Image>().sprite = baseButton.GetComponent<Image>().sprite;
            buttonGO.GetComponent<Image>().type = baseButton.GetComponent<Image>().type;
            buttonGO.GetComponent<Image>().fillCenter = baseButton.GetComponent<Image>().fillCenter;
            buttonGO.GetComponent<Button>().colors = baseButton.GetComponent<Button>().colors;
            buttonGO.GetComponent<Button>().targetGraphic = buttonGO.GetComponent<Image>();

            rtT.localScale = rtO.localScale;

            rtT.anchoredPosition = rtO.anchoredPosition;
            rtT.sizeDelta = rtO.sizeDelta;

            rtT.localPosition = rtO.localPosition + new Vector3(posDelta.x, posDelta.y, 0);
            rtT.localRotation = rtO.localRotation;

            GameObject textGO = new GameObject("Text", typeof(Text));
            textGO.transform.SetParent(buttonGO.transform);

            RectTransform rtO2 = baseButton.Find("Text").GetComponent<RectTransform>();
            RectTransform rtT2 = textGO.GetComponent<RectTransform>();
            rtT2.localScale = rtO2.localScale;

            rtT2.anchorMin = rtO2.anchorMin;
            rtT2.anchorMax = rtO2.anchorMax;
            rtT2.anchoredPosition = rtO2.anchoredPosition;
            rtT2.sizeDelta = rtO2.sizeDelta;

            rtT2.localPosition = rtO2.localPosition;
            rtT2.localRotation = rtO2.localRotation;

            Text tO = baseButton.Find("Text").GetComponent<Text>();
            Text tT = textGO.GetComponent<Text>();
            tT.text = buttonText;
            tT.font = tO.font;
            tT.fontSize = tO.fontSize;
            tT.fontStyle = tO.fontStyle;
            tT.alignment = tO.alignment;
            tT.color = tO.color;

            return buttonGO.transform;
        }
    }
}
