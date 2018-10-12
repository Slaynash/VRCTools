using System;
using System.Collections;
using System.IO;
using System.Linq;
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

            int buildNumber = -1;
            VRCModLogger.Log("[ModConfigPage] Getting game version");
            PropertyInfo vrcApplicationSetupInstanceProperty = typeof(VRCApplicationSetup).GetProperties(BindingFlags.Public | BindingFlags.Static).First((pi) => pi.PropertyType == typeof(VRCApplicationSetup));
            if (vrcApplicationSetupInstanceProperty != null) buildNumber = ((VRCApplicationSetup)vrcApplicationSetupInstanceProperty.GetValue(null, null)).buildNumber;
            VRCModLogger.Log("[ModConfigPage] Game build " + buildNumber);


            yield return DownloadDependency(ModValues.discordrpcdependencyDownloadLink, "discord-rpc.dll");

            if (buildNumber < 630){
                yield return DownloadDependency(ModValues.vrccoreeditordependencyDownloadLink, "VRCCore-Editor.dll");

                VRCModLogger.Log("[DependenciesDownloader] Loading VRCCore-Editor.dll");
                Assembly.LoadFile(vrccedllPath);
            }
        }

        private static IEnumerator DownloadDependency(string downloadUrl, string dllName)
        {
            VRCModLogger.Log("[DependenciesDownloader] Checking dependency " + dllName);
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