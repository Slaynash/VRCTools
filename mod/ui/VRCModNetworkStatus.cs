using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRCModLoader;
using VRCModNetwork;

namespace VRCTools
{
    internal static class VRCModNetworkStatus
    {
        private static Text networkstatusText;

        public static void Setup()
        {
            Transform baseTextTransform = QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/BuildNumText");
            if (baseTextTransform != null)
            {
                Transform vrcmodNetworkTransform = new GameObject("VRCModNetworkStatusText", typeof(RectTransform), typeof(Text)).transform;
                vrcmodNetworkTransform.SetParent(baseTextTransform.parent);
                vrcmodNetworkTransform.SetSiblingIndex(baseTextTransform.GetSiblingIndex() + 1);

                networkstatusText = vrcmodNetworkTransform.GetComponent<Text>();
                RectTransform networkstatusRT = vrcmodNetworkTransform.GetComponent<RectTransform>();

                networkstatusRT.localScale = baseTextTransform.localScale;

                networkstatusRT.anchorMin = baseTextTransform.GetComponent<RectTransform>().anchorMin;
                networkstatusRT.anchorMax = baseTextTransform.GetComponent<RectTransform>().anchorMax;
                networkstatusRT.anchoredPosition = baseTextTransform.GetComponent<RectTransform>().anchoredPosition;
                networkstatusRT.sizeDelta = new Vector2(2000, baseTextTransform.GetComponent<RectTransform>().sizeDelta.y);
                networkstatusRT.pivot = baseTextTransform.GetComponent<RectTransform>().pivot;

                Vector3 newPos = baseTextTransform.localPosition;
                newPos.x -= baseTextTransform.GetComponent<RectTransform>().sizeDelta.x * 0.5f;
                newPos.x += 2000 * 0.5f;
                newPos.y += -85;

                networkstatusRT.localPosition = newPos;
                networkstatusText.text = "VRCModNetworkStatus: <color=orange>Unknown</color>";
                networkstatusText.color = baseTextTransform.GetComponent<Text>().color;
                networkstatusText.font = baseTextTransform.GetComponent<Text>().font;
                networkstatusText.fontSize = baseTextTransform.GetComponent<Text>().fontSize;
                networkstatusText.fontStyle = baseTextTransform.GetComponent<Text>().fontStyle;

                UpdateNetworkStatus();
            }
            else
            {
                VRCModLogger.Log("[VRCMNWStatus] QuickMenu/ShortcutMenu/BuildNumText is null");
            }
        }


        internal static void UpdateNetworkStatus()
        {
            if (networkstatusText != null)
            {
                if (VRCModNetworkManager.IsAuthenticated)
                    networkstatusText.text = "VRCModNetwork status: <color=lime>Authenticated</color>";
                else if (VRCModNetworkManager.State == VRCModNetworkManager.ConnectionState.CONNECTED)
                    networkstatusText.text = "VRCModNetwork status: <color=orange>Not Authenticated</color>";
                else if (VRCModNetworkManager.State == VRCModNetworkManager.ConnectionState.CONNECTING)
                    networkstatusText.text = "VRCModNetwork status: <color=orange>Connecting</color>";
                else
                    networkstatusText.text = "VRCModNetwork status: <color=red>Disconnected</color>";
            }
        }
    }
}
