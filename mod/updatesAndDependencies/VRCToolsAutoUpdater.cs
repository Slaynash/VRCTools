using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRCModLoader;

namespace VRCTools
{
    internal static class VRCToolsAutoUpdater
    {
        private static bool updatePopupClose = false;

        public static IEnumerator CheckAndUpdate()
        {
            VRCUiPopupManagerUtils.ShowPopup("VRCTools Updater", "Checking VRCTools version");

            WWW versionWWW = new WWW("https://download2.survival-machines.fr/vrcmodloader/VRCToolsVersion");
            yield return versionWWW;
            while (!versionWWW.isDone)
                yield return null;
            int responseCode = WebRequestsUtils.GetResponseCode(versionWWW);
            VRCModLogger.Log("[VRCModLoaderUpdater] version webpage returned [" + responseCode + "] \"" + versionWWW.text + "\"");
            if (responseCode == 200 && versionWWW.text.Trim() != ModManager.Mods.FirstOrDefault(m => m.Name == "VRCTools").Version)
                yield return ShowVRCToolsUpdatePopup(versionWWW.text.Trim());
            else
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
        }

        internal static IEnumerator ShowVRCToolsUpdatePopup(string version)
        {
            string vrctoolsPath = "";
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                DirectoryInfo baseDir = Directory.GetParent(Values.ModsPath);
                FileInfo oldFile = baseDir.GetFiles().FirstOrDefault(f => f.Name.ToLower().StartsWith("vrctools.") && f.Name.ToLower().EndsWith(".dll"));
                if (oldFile != null)
                    oldFile.Delete();
                vrctoolsPath = Path.Combine(Directory.GetParent(Values.ModsPath).FullName, "VRCTools." + version + ".dll");
            }
            else if (Application.platform == RuntimePlatform.Android)
                vrctoolsPath = "/sdcard/VRCTools/Mods/VRCTools.dll";

            Image downloadProgressFillImage = null;

            VRCUiPopupManagerUtils.ShowPopup("VRCTools Updater", "Updating VRCTools to " + version + "...", "Quit", () => Application.Quit(), (popup) =>
            {
                if (popup.popupProgressFillImage != null)
                {
                    popup.popupProgressFillImage.enabled = true;
                    popup.popupProgressFillImage.fillAmount = 0f;
                    downloadProgressFillImage = popup.popupProgressFillImage;
                }
            });


            WWW vrctoolsDownload = new WWW(string.Format(ModValues.vrctoolsDownloadLink, version));
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
                VRCUiPopupManagerUtils.ShowPopup("VRCTools Updater", "Saving VRCTools");
                VRCModLogger.Log("[AvatarFavUpdater] Saving file");
                File.WriteAllBytes(vrctoolsPath, vrctoolsDownload.bytes);
                updatePopupClose = false;
                VRCUiPopupManagerUtils.ShowPopup("VRCTools Updater", "A VRCTools updated has been downloaded. Please restart your game for it to take effect", "OK", () =>
                {
                    VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                    updatePopupClose = true;
                });
                while (!updatePopupClose)
                    yield return null;
            }
            else
            {
                updatePopupClose = false;
                VRCUiPopupManagerUtils.ShowPopup("VRCTools Updater", "Failed to download the VRCTools update (E" + responseCode + "): " + vrctoolsDownload.text, "OK", () =>
                {
                    VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                    updatePopupClose = true;
                });
                while (!updatePopupClose)
                    yield return null;
            }
        }
    }
}
