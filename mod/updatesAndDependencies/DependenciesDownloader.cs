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
        private static Image downloadProgressFillImage;

        internal static IEnumerator CheckDownloadFiles()
        {
            string vrccedllPath = Values.VRCToolsDependenciesPath + "VRCCore-Editor.dll";
            string oharmonydllPath = Values.VRCToolsDependenciesPath + "0Harmony.dll";
            
            yield return DownloadDependency(ModValues.discordrpcdependencyDownloadLink, "discord-rpc.dll");
            yield return DownloadDependency(ModValues.vrccoreeditordependencyDownloadLink, "VRCCore-Editor.dll");
            yield return DownloadDependency(ModValues.oharmonydependencyDownloadLink, "0Harmony.dll");
            //yield return DownloadDependency(ModValues.vrcmnwclientdependencyDownloadLink, "VRCModNetworkClient.dll");

            VRCModLogger.Log("[DependenciesDownloader] Initializing Discord RichPresence");
            DiscordManager.Init();
            Assembly.LoadFile(vrccedllPath);
            Assembly.LoadFile(oharmonydllPath);
        }

        private static IEnumerator DownloadDependency(string downloadUrl, string dllName)
        {
            String dependenciesDownloadFile = Values.VRCToolsDependenciesPath + dllName;

            if (!File.Exists(dependenciesDownloadFile))
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Downloading VRCTools dependency:\n" + dllName, "Quit", () => Application.Quit(), (popup) =>
                {
                    if (popup.popupProgressFillImage != null)
                    {
                        popup.popupProgressFillImage.enabled = true;
                        popup.popupProgressFillImage.fillAmount = 0f;
                        downloadProgressFillImage = popup.popupProgressFillImage;
                    }
                });


                WWW dependencyDownload = new WWW(downloadUrl);
                yield return dependencyDownload;
                while (!dependencyDownload.isDone)
                {
                    VRCModLogger.Log("[DependenciesDownloader] Download progress: " + dependencyDownload.progress);
                    downloadProgressFillImage.fillAmount = dependencyDownload.progress;
                    yield return null;
                }

                int responseCode = WebRequestsUtils.GetResponseCode(dependencyDownload);
                VRCModLogger.Log("[DependenciesDownloader] Download done ! response code: " + responseCode);
                VRCModLogger.Log("[DependenciesDownloader] File size: " + dependencyDownload.bytes.Length);

                if (responseCode == 200)
                {
                    VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Saving dependency " + dllName);
                    VRCModLogger.Log("[DependenciesDownloader] Saving file " + dllName);
                    VRCModLogger.Log(Path.GetDirectoryName(dependenciesDownloadFile));
                    Directory.CreateDirectory(Path.GetDirectoryName(dependenciesDownloadFile));
                    File.WriteAllBytes(dependenciesDownloadFile, dependencyDownload.bytes);
                    VRCModLogger.Log("[DependenciesDownloader] File saved");
                }
                else
                {
                    VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Unable to download VRCTools dependencies " + dllName + ": Server returned code " + responseCode, "Quit", () => Application.Quit());
                    throw new Exception("Unable to download VRCTools dependencies 0Harmony.dll: Server returned code " + responseCode);
                }
            }
        }
    }
}