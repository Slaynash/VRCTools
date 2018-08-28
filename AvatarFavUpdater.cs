using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VRCModLoader;

namespace VRCTools
{
    internal class AvatarFavUpdater
    {
        private static Image downloadProgressFillImage;

        public static IEnumerator CheckForAvatarFavUpdate()
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
                        int responseCode = WebRequestsUtils.GetResponseCode(hashCheckWWW);
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

        private static IEnumerator DownloadAvatarFav(string avatarfavPath)
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

            int responseCode = WebRequestsUtils.GetResponseCode(vrctoolsDownload);
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
    }
}