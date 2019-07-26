using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VRCModLoader;

namespace VRCTools
{
    internal static class AvatarFavUpdater
    {
        private static Image downloadProgressFillImage;
        private static bool popupClosed = false;

        public static IEnumerator CheckForAvatarFavUpdate()
        {
            string avatarfavPath = ModManager.Mods.FirstOrDefault(m => m.Name == "AvatarFav")?.GetType().Assembly.Location ?? Path.Combine(Values.ModsPath, "AvatarFav.dll");
            VRCModLogger.Log("AvatarFav.dll path: " + avatarfavPath);
            string fileHash = "";
            if (ModPrefs.GetBool("vrctools", "avatarfavdownloadasked"))
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

                        WWW hashCheckWWW = new WWW(ModValues.avatarfavCheckLink + "?localhash=" + fileHash);
                        yield return hashCheckWWW;
                        while (!hashCheckWWW.isDone) yield return null;
                        int responseCode = WebRequestsUtils.GetResponseCode(hashCheckWWW);
                        VRCModLogger.Log("[VRCToolsUpdater] hash check webpage returned [" + responseCode + "] \"" + hashCheckWWW.text + "\"");
                        if (responseCode != 200)
                        {
                            popupClosed = false;
                            VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Unable to check AvatarFav file hash", "OK", () =>
                            {
                                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                                popupClosed = true;
                            });
                            while (!popupClosed) yield return null;
                        }
                        else if (hashCheckWWW.text.Equals("OUTOFDATE"))
                        {
                            popupClosed = false;
                            bool download = false;
                            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "An AvatarFav update is available", "Update", () =>
                            {
                                download = true;
                                popupClosed = true;
                            }, "Ignore", () =>
                            {
                                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                                popupClosed = true;
                            });
                            while (!popupClosed) yield return null;

                            if (download)
                            {
                                yield return DownloadAvatarFav(avatarfavPath);
                            }
                        }
                    }
                    else
                    {
                        yield return DownloadAvatarFav(avatarfavPath);
                    }
                }
            }
            else
            {
                popupClosed = false;
                bool download = false;
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Do you want to install the AvatarFav mod ?", "Accept", () => {
                    ModPrefs.SetBool("vrctools", "avatarfavdownload", true);
                    download = true;
                    popupClosed = true;
                }, "Deny", () => {
                    ModPrefs.SetBool("vrctools", "avatarfavdownload", false);
                    VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                    popupClosed = true;
                });
                while (!popupClosed) yield return null;
                ModPrefs.SetBool("vrctools", "avatarfavdownloadasked", true);

                if (download)
                {
                    yield return DownloadAvatarFav(avatarfavPath);
                }
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


            WWW vrctoolsDownload = new WWW(ModValues.avatarfavDownloadLink);
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
                bool choiceDone = false;
                if (File.Exists(avatarfavPath)){
                    try {
                      File.Delete(avatarfavPath);
                    }
                    catch (String ex){
                        VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Unable to update AvatarFav: " + ex, "Quit", () => Application.Quit());
                        VRCModLogger.Log("[AvatarFavUpdater] Unable to delete old AvatarFav.dll: " + ex);
                    }
                };
                try {
                    File.WriteAllBytes(avatarfavPath, vrctoolsDownload.bytes);
                    VRCModLogger.Log("[AvatarFavUpdater] Showing restart dialog");
                    VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Update downloaded", "Restart", () => {
                    choiceDone = true;
                });
                } catch (String ex){
                    VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Unable to update AvatarFav: " + ex, "Quit", () => Application.Quit());
                    VRCModLogger.Log("[AvatarFavUpdater] Unable to delete old AvatarFav.dll: " + ex);
                }    

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
                    Process.Start(Path.GetDirectoryName(Path.GetDirectoryName(avatarfavPath)) + "\\VRChat.exe", args);
                    Thread.Sleep(100);
                });
                t.Start();

                Process.GetCurrentProcess().Kill();
            }
            else
            {
                VRCUiPopupManagerUtils.ShowPopup("AvatarFav Updater", "Unable to update VRCTools: Server returned code " + responseCode, "Quit", () => Application.Quit());
            }
        }
    }
}
