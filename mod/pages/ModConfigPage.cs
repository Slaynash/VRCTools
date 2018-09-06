using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRCModLoader;

namespace VRCTools
{
    internal class ModConfigPage : MonoBehaviour
    {
        private Transform scrollContent;

        void Awake()
        {
            scrollContent = UnityUiUtils.CreateScrollView(GetComponent<RectTransform>(), 1500, 1000, 0, 2000, false, true);
        }

        void OnEnable()
        {
            //clear content
            foreach (Transform child in scrollContent) Destroy(child);

            //foreach elements registered in ModPrefs, add a config element
            float totalHeight = 0;
            Dictionary<string, Dictionary<string, ModPrefs.PrefDesc>> modPrefs = ModPrefs.GetPrefs();
            for (int i = 0; i < modPrefs.Count; i++) {
                KeyValuePair<string, Dictionary<string, ModPrefs.PrefDesc>> modPrefCategory = modPrefs.ElementAt(i);
                //create a new category
                CreateCategoryTitle(modPrefCategory.Key, ref totalHeight);

                //add all prefs under this category
                //TODO
            }
            scrollContent.GetComponent<RectTransform>().sizeDelta = new Vector2(scrollContent.GetComponent<RectTransform>().sizeDelta.x, totalHeight > 1000 ? totalHeight : 1000);
        }

        private void CreateCategoryTitle(string title, ref float totalHeight)
        {
            GameObject text = new GameObject("CategoryTitle ("+title+")", typeof(RectTransform), typeof(Text));
            text.transform.SetParent(scrollContent);
            text.GetComponent<RectTransform>().localScale = Vector3.one;
            text.GetComponent<RectTransform>().localPosition = Vector3.zero;
            text.GetComponent<RectTransform>().localRotation = Quaternion.identity;
            text.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 70);
            text.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1.0f);
            text.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1.0f);
            text.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1.0f);
            text.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -totalHeight);
            totalHeight += 70;
            text.GetComponent<Text>().font = QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/BuildNumText").GetComponent<Text>().font;
            text.GetComponent<Text>().fontSize = 50;
            text.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            text.GetComponent<Text>().text = "[" + title + "]";
            //text.GetComponent<Text>().color = Color.yellow;
        }




        public static void Setup()
        {
            //Create mods config page

            GameObject screens = GameObject.Find("UserInterface/MenuContent/Screens");
            GameObject avatarscreen = GameObject.Find("UserInterface/MenuContent/Screens/Avatar");
            if (avatarscreen != null)
            {
                GameObject go = new GameObject("ModConfig", typeof(RectTransform), typeof(VRCUiPage));
                go.transform.SetParent(screens.transform);
                go.GetComponent<RectTransform>().localScale = Vector3.one;
                go.GetComponent<RectTransform>().localRotation = Quaternion.identity;
                go.GetComponent<RectTransform>().localPosition = Vector3.zero;
                go.GetComponent<VRCUiPage>().screenType = avatarscreen.GetComponent<VRCUiPage>().screenType;
                go.GetComponent<VRCUiPage>().displayName = "Mod Conf";
                go.GetComponent<VRCUiPage>().AudioShow = avatarscreen.GetComponent<VRCUiPage>().AudioShow;
                go.GetComponent<VRCUiPage>().AudioLoop = avatarscreen.GetComponent<VRCUiPage>().AudioLoop;
                go.GetComponent<VRCUiPage>().AudioHide = avatarscreen.GetComponent<VRCUiPage>().AudioHide;

                go.AddComponent<ModConfigPage>();

                //SCREEN CONTENT SIZE: 1500x1000


                //Create mods config quickmenu button
                Transform baseButtonTransform = QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/CloseButton");
                if (baseButtonTransform != null)
                {
                    Transform modconf = UnityUiUtils.DuplicateButton(baseButtonTransform, "Mod\nConfigs", new Vector2(-420, 0));
                    modconf.name = "ModConfigsButton";
                    modconf.GetComponentInChildren<Text>().color = new Color(1, 0.5f, 0.1f);
                    //modconf.GetComponent<Button>().interactable = false;
                    modconf.GetComponent<Button>().onClick.RemoveAllListeners();
                    modconf.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        VRCUiManagerUtils.GetVRCUiManager().ShowUi(false, true);
                        ModManager.StartCoroutine(QuickMenuUtils.PlaceUiAfterPause());
                        VRCUiManagerUtils.GetVRCUiManager().ShowScreen("UserInterface/MenuContent/Screens/ModConfig");
                    });
                }
                else
                {
                    VRCModLogger.Log("[VRCTools] QuickMenu/ShortcutMenu/CloseButton is null");
                }

            }
            else
            {
                VRCModLogger.Log("[VRCTools] UserInterface/MenuContent/Screens/Avatar is null");
            }
        }
    }
}
