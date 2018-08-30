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
    internal class DependenciesDownloader
    {
        private static Image downloadProgressFillImage;
        private static string discordrpcdllPath;

        internal static bool CheckDownloadFiles()
        {
            discordrpcdllPath = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) + "\\VRCTools\\Dependencies\\discord-rpc.dll";
            if (File.Exists(discordrpcdllPath)) return true;
            else
            {
                VRCFlowManagerUtils.DisableVRCFlowManager();
                ModManager.StartCoroutine(DownloadDRPCdll());

                return false;
            }
        }

        private static IEnumerator DownloadDRPCdll()
        {
            yield return VRCUiManagerUtils.WaitForUiManagerInit();

            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Downloading dependency:\ndiscord-rpc.dll", "Quit", () => Application.Quit(), (popup) => {
                if (popup.popupProgressFillImage != null)
                {
                    popup.popupProgressFillImage.enabled = true;
                    popup.popupProgressFillImage.fillAmount = 0f;
                    downloadProgressFillImage = popup.popupProgressFillImage;
                }
            });


            WWW vrctoolsDownload = new WWW("https://vrchat.survival-machines.fr/vrcmod/discord-rpc.dll");
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
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Saving dependency");
                VRCModLogger.Log("[AvatarFavUpdater] Saving file");
                VRCModLogger.Log(Path.GetDirectoryName(discordrpcdllPath));
                Directory.CreateDirectory(Path.GetDirectoryName(discordrpcdllPath));
                File.WriteAllBytes(discordrpcdllPath, vrctoolsDownload.bytes);
                VRCModLogger.Log("[AvatarFavUpdater] File saved");
            }
            else
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Unable to download VRCTools dependencies discord-rpc.dll: Server returned code " + responseCode, "Quit", () => Application.Quit());
            }

            VRCModLogger.Log("[AvatarFavUpdater] Initializing Discord RichPresence");
            DiscordManager.Init();

            VRCModLogger.Log("[AvatarFavUpdater] Continuing standard VRCTools start");
            if (VRCModLoaderUpdater.CheckVRCModLoaderHash())
            {
                VRCTools.CheckForPermissions();
            }
        }
    }
}