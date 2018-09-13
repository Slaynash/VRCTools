using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VRCModLoader;

namespace VRCTools
{
    internal static class DependenciesDownloader
    {
        private static string discordrpcdllPath;
        private static string vrccedllPath;
        private static string oharmonydllPath;

        private static Image downloadProgressFillImage;

        internal static IEnumerator CheckDownloadFiles()
        {
            discordrpcdllPath = Values.VRCToolsDependenciesPath + "discord-rpc.dll";
            vrccedllPath = Values.VRCToolsDependenciesPath + "VRCCore-Editor.dll";
            oharmonydllPath = Values.VRCToolsDependenciesPath + "0Harmony.dll";

            if (!File.Exists(discordrpcdllPath))
            {
                yield return DownloadDRPCdll();
            }
            if (!File.Exists(vrccedllPath))
            {
                yield return DownloadVRCCEdll();
            }
            if (!File.Exists(oharmonydllPath))
            {
                yield return Download0Harmonydll();
            }

            VRCModLogger.Log("[DependenciesDownloader] Initializing Discord RichPresence");
            DiscordManager.Init();
            Assembly.LoadFile(vrccedllPath);
            Assembly.LoadFile(oharmonydllPath);
        }

        private static IEnumerator DownloadDRPCdll()
        {
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Downloading VRCTools dependency:\ndiscord-rpc.dll", "Quit", () => Application.Quit(), (popup) => {
                if (popup.popupProgressFillImage != null)
                {
                    popup.popupProgressFillImage.enabled = true;
                    popup.popupProgressFillImage.fillAmount = 0f;
                    downloadProgressFillImage = popup.popupProgressFillImage;
                }
            });


            WWW vrctoolsDownload = new WWW(ModValues.discordrpcdependencyDownloadLink);
            yield return vrctoolsDownload;
            while (!vrctoolsDownload.isDone)
            {
                VRCModLogger.Log("[DependenciesDownloader] Download progress: " + vrctoolsDownload.progress);
                downloadProgressFillImage.fillAmount = vrctoolsDownload.progress;
                yield return null;
            }

            int responseCode = WebRequestsUtils.GetResponseCode(vrctoolsDownload);
            VRCModLogger.Log("[DependenciesDownloader] Download done ! response code: " + responseCode);
            VRCModLogger.Log("[DependenciesDownloader] File size: " + vrctoolsDownload.bytes.Length);

            if (responseCode == 200)
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Saving dependency discord-rpc.dll");
                VRCModLogger.Log("[DependenciesDownloader] Saving file discord-rpc.dll");
                VRCModLogger.Log(Path.GetDirectoryName(discordrpcdllPath));
                Directory.CreateDirectory(Path.GetDirectoryName(discordrpcdllPath));
                File.WriteAllBytes(discordrpcdllPath, vrctoolsDownload.bytes);
                VRCModLogger.Log("[DependenciesDownloader] File saved");
            }
            else
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Unable to download VRCTools dependencies discord-rpc.dll: Server returned code " + responseCode, "Quit", () => Application.Quit());
                throw new Exception("Unable to download VRCTools dependencies VRCCore-Editor.dll: Server returned code " + responseCode);
            }
        }

        private static IEnumerator DownloadVRCCEdll()
        {
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Downloading VRCTools dependency:\nVRCCore-Editor.dll", "Quit", () => Application.Quit(), (popup) => {
                if (popup.popupProgressFillImage != null)
                {
                    popup.popupProgressFillImage.enabled = true;
                    popup.popupProgressFillImage.fillAmount = 0f;
                    downloadProgressFillImage = popup.popupProgressFillImage;
                }
            });


            WWW vrctoolsDownload = new WWW(ModValues.vrccoreeditordependencyDownloadLink);
            yield return vrctoolsDownload;
            while (!vrctoolsDownload.isDone)
            {
                VRCModLogger.Log("[AvatarFavUpdater] Download progress: " + vrctoolsDownload.progress);
                downloadProgressFillImage.fillAmount = vrctoolsDownload.progress;
                yield return null;
            }

            int responseCode = WebRequestsUtils.GetResponseCode(vrctoolsDownload);
            VRCModLogger.Log("[DependenciesDownloader] Download done ! response code: " + responseCode);
            VRCModLogger.Log("[DependenciesDownloader] File size: " + vrctoolsDownload.bytes.Length);

            if (responseCode == 200)
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Saving dependency VRCCore-Editor.dll");
                VRCModLogger.Log("[DependenciesDownloader] Saving file VRCCore-Editor.dll");
                VRCModLogger.Log(Path.GetDirectoryName(vrccedllPath));
                Directory.CreateDirectory(Path.GetDirectoryName(vrccedllPath));
                File.WriteAllBytes(vrccedllPath, vrctoolsDownload.bytes);
                VRCModLogger.Log("[DependenciesDownloader] File saved");
            }
            else
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Unable to download VRCTools dependencies VRCCore-Editor.dll: Server returned code " + responseCode, "Quit", () => Application.Quit());
                throw new Exception("Unable to download VRCTools dependencies VRCCore-Editor.dll: Server returned code " + responseCode);
            }
        }

        private static IEnumerator Download0Harmonydll()
        {
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Downloading VRCTools dependency:\n0Harmony.dll", "Quit", () => Application.Quit(), (popup) => {
                if (popup.popupProgressFillImage != null)
                {
                    popup.popupProgressFillImage.enabled = true;
                    popup.popupProgressFillImage.fillAmount = 0f;
                    downloadProgressFillImage = popup.popupProgressFillImage;
                }
            });


            WWW oharmonyDownload = new WWW(ModValues.oharmonydependencyDownloadLink);
            yield return oharmonyDownload;
            while (!oharmonyDownload.isDone)
            {
                VRCModLogger.Log("[AvatarFavUpdater] Download progress: " + oharmonyDownload.progress);
                downloadProgressFillImage.fillAmount = oharmonyDownload.progress;
                yield return null;
            }

            int responseCode = WebRequestsUtils.GetResponseCode(oharmonyDownload);
            VRCModLogger.Log("[DependenciesDownloader] Download done ! response code: " + responseCode);
            VRCModLogger.Log("[DependenciesDownloader] File size: " + oharmonyDownload.bytes.Length);

            if (responseCode == 200)
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Saving dependency 0Harmony.dll");
                VRCModLogger.Log("[DependenciesDownloader] Saving file 0Harmony.dll");
                VRCModLogger.Log(Path.GetDirectoryName(oharmonydllPath));
                Directory.CreateDirectory(Path.GetDirectoryName(oharmonydllPath));
                File.WriteAllBytes(oharmonydllPath, oharmonyDownload.bytes);
                VRCModLogger.Log("[DependenciesDownloader] File saved");
            }
            else
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Unable to download VRCTools dependencies 0Harmony.dll: Server returned code " + responseCode, "Quit", () => Application.Quit());
                throw new Exception("Unable to download VRCTools dependencies 0Harmony.dll: Server returned code " + responseCode);
            }
        }
    }
}