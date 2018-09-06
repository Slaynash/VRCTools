using System;
using System.Linq;
using System.Reflection;
using VRCModLoader;

namespace VRCTools
{
    public static class VRCUiPopupManagerUtils
    {
        private static VRCUiPopupManager uiPopupManagerInstance;



        public static VRCUiPopupManager GetVRCUiPopupManager()
        {
            if (uiPopupManagerInstance == null)
            {
                FieldInfo[] nonpublicStaticPopupFields = typeof(VRCUiPopupManager).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
                if (nonpublicStaticPopupFields.Length == 0)
                {
                    VRCModLogger.Log("[VRCUiPopupManagerUtils] nonpublicStaticPopupFields.Length == 0");
                    return null;
                }
                FieldInfo uiPopupManagerInstanceField = nonpublicStaticPopupFields.First(field => field.FieldType == typeof(VRCUiPopupManager));
                if (uiPopupManagerInstanceField == null)
                {
                    VRCModLogger.Log("[VRCUiPopupManagerUtils] uiPopupManagerInstanceField == null");
                    return null;
                }
                uiPopupManagerInstance = uiPopupManagerInstanceField.GetValue(null) as VRCUiPopupManager;
            }

            return uiPopupManagerInstance;
        }
        
        public static void ShowPopup(string title, string body, string leftButton, Action leftButtonAction, string rightButton, Action rightButtonAction, Action<VRCUiPopup> additionalSetup = null)
        {
            if (GetVRCUiPopupManager() == null)
            {
                VRCModLogger.Log("[VRCUiPopupManagerUtils] uiPopupManagerInstance == null");
                return;
            }

            uiPopupManagerInstance.ShowStandardPopup(title, body, leftButton, leftButtonAction, rightButton, rightButtonAction, additionalSetup);
        }

        public static void ShowPopup(string title, string body, Action<VRCUiPopup> additionalSetup = null)
        {
            if (GetVRCUiPopupManager() == null)
            {
                VRCModLogger.Log("[VRCUiPopupManagerUtils] uiPopupManagerInstance == null");
                return;
            }

            uiPopupManagerInstance.ShowStandardPopup(title, body, additionalSetup);
        }

        public static void ShowPopup(string title, string body, string middleButton, Action middleButtonAction, Action<VRCUiPopup> additionalSetup = null)
        {
            if (GetVRCUiPopupManager() == null)
            {
                VRCModLogger.Log("[VRCUiPopupManagerUtils] uiPopupManagerInstance == null");
                return;
            }

            uiPopupManagerInstance.ShowStandardPopup(title, body, middleButton, middleButtonAction, additionalSetup);
        }
    }
}