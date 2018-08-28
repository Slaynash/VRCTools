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
    internal class VRCModLoaderUpdater
    {
        public static bool CheckVRCModLoaderHash()
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
            int responseCode = WebRequestsUtils.GetResponseCode(hashCheckWWW);
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

        internal static IEnumerator ShowVRCModLoaderUpdatePopup()
        {
            yield return VRCUiManagerUtils.WaitForUiManagerInit();
            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "A VRCModLoader update is available. You can install it using the installer (more info on the VRCTools website)", "OK", () =>
            {
                VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                VRCTools.CheckForPermissions();
            });
        }
    }
}
