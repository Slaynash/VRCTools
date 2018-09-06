using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRCModLoader;

namespace VRCTools
{
    public static class VRCUiManagerUtils
    {

        private static VRCUiManager uiManagerInstance;

        public static VRCUiManager GetVRCUiManager()
        {
            if (uiManagerInstance == null)
            {
                FieldInfo[] nonpublicStaticPopupFields = typeof(VRCUiManager).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
                if (nonpublicStaticPopupFields.Length == 0)
                {
                    VRCModLogger.Log("[VRCUiManagerUtils] nonpublicStaticPopupFields.Length == 0");
                    return null;
                }
                FieldInfo uiManagerInstanceField = nonpublicStaticPopupFields.First(field => field.FieldType == typeof(VRCUiManager));
                if (uiManagerInstanceField == null)
                {
                    VRCModLogger.Log("[VRCUiManagerUtils] uiManagerInstanceField == null");
                    return null;
                }
                uiManagerInstance = uiManagerInstanceField.GetValue(null) as VRCUiManager;
            }

            return uiManagerInstance;
        }

        public static IEnumerator WaitForUiManagerInit()
        {
            VRCModLogger.Log("WaitForUIManager");
            if (uiManagerInstance == null)
            {
                FieldInfo[] nonpublicStaticFields = typeof(VRCUiManager).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
                if (nonpublicStaticFields.Length == 0)
                {
                    VRCModLogger.Log("[VRCUiManagerUtils] nonpublicStaticFields.Length == 0");
                    yield break;
                }
                FieldInfo uiManagerInstanceField = nonpublicStaticFields.First(field => field.FieldType == typeof(VRCUiManager));
                if (uiManagerInstanceField == null)
                {
                    VRCModLogger.Log("[VRCUiManagerUtils] uiManagerInstanceField == null");
                    yield break;
                }
                uiManagerInstance = uiManagerInstanceField.GetValue(null) as VRCUiManager;
                VRCModLogger.Log("[VRCUiManagerUtils] Waiting for UI Manager...");
                while (uiManagerInstance == null)
                {
                    uiManagerInstance = uiManagerInstanceField.GetValue(null) as VRCUiManager;
                    yield return null;
                }
                VRCModLogger.Log("[VRCUiManagerUtils] UI Manager loaded");
            }
        }
    }
}
