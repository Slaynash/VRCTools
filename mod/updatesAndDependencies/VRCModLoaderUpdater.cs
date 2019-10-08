using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using VRCModLoader;

namespace VRCTools
{
    internal static class VRCModLoaderUpdater
    {
        private static bool updatePopupClose = false;

        public static IEnumerator CheckVRCModLoaderHash()
        {
            string vrcmodloaderPath = "";
            if (Application.platform == RuntimePlatform.WindowsPlayer)
                vrcmodloaderPath = Values.VRCModLoaderAssemblyPath;
            else if (Application.platform == RuntimePlatform.Android)
                vrcmodloaderPath = "/sdcard/VRCTools/VRCModLoader.dll";
            if (File.Exists(vrcmodloaderPath))
            {
                string fileHash = "";
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(vrcmodloaderPath))
                    {
                        var hash = md5.ComputeHash(stream);
                        fileHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
                VRCModLogger.Log("[VRCModLoaderUpdater] Local VRCModLoader file hash: " + fileHash);

                WWW hashCheckWWW = new WWW("https://download2.survival-machines.fr/vrcmodloader/VRCModLoaderHashCheck.php?localhash=" + fileHash);
                yield return hashCheckWWW;
                while (!hashCheckWWW.isDone)
                    yield return null;
                int responseCode = WebRequestsUtils.GetResponseCode(hashCheckWWW);
                VRCModLogger.Log("[VRCModLoaderUpdater] hash check webpage returned [" + responseCode + "] \"" + hashCheckWWW.text + "\"");
                if (responseCode == 200 && hashCheckWWW.text.Equals("OUTOFDATE"))
                    yield return ShowVRCModLoaderUpdatePopup();
            }
        }

        internal static IEnumerator ShowVRCModLoaderUpdatePopup()
        {
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "A VRCModLoader update is available. You can install it using the installer (more info on the VRCTools website)", "OK", () =>
            {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                updatePopupClose = true;
            });

            while (!updatePopupClose) yield return null;
        }
    }
}
