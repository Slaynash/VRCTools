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
        private List<IConfigElement> configElements = new List<IConfigElement>();

        public static void Setup()
        {
            //Create mods config page
            VRCModLogger.Log("[ModConfigPage] Setup");
            
            GameObject avatarscreen = GameObject.Find("UserInterface/MenuContent/Screens/Avatar");
            GameObject cameramenu = GameObject.Find("UserInterface/MenuContent/Screens/CameraMenu");
            VRCModLogger.Log("[ModConfigPage] avatarscreen: " + avatarscreen);
            if (avatarscreen != null)
            {
                VRCModLogger.Log("[ModConfigPage] Setting up ModConfigPage");
                GameObject go = new GameObject("ModConfig", typeof(RectTransform), typeof(VRCUiPage));
                go.transform.SetParent(avatarscreen.transform.parent, false);
                go.GetComponent<VRCUiPage>().screenType = avatarscreen.GetComponent<VRCUiPage>().screenType;
                go.GetComponent<VRCUiPage>().displayName = "Mod Conf";
                go.GetComponent<VRCUiPage>().AudioShow = avatarscreen.GetComponent<VRCUiPage>().AudioShow;
                go.GetComponent<VRCUiPage>().AudioHide = avatarscreen.GetComponent<VRCUiPage>().AudioHide;

                VRCModLogger.Log("[ModConfigPage] Adding ModConfigPage component");
                go.AddComponent<ModConfigPage>();

                //SCREEN CONTENT SIZE: 1500x1000

                int buildNumber = -1;
                VRCModLogger.Log("[ModConfigPage] Getting game version");
                PropertyInfo vrcApplicationSetupInstanceProperty = typeof(VRCApplicationSetup).GetProperties(BindingFlags.Public | BindingFlags.Static).First((pi) => pi.PropertyType == typeof(VRCApplicationSetup));
                if (vrcApplicationSetupInstanceProperty != null) buildNumber = ((VRCApplicationSetup)vrcApplicationSetupInstanceProperty.GetValue(null, null)).buildNumber;
                VRCModLogger.Log("[ModConfigPage] Game build " + buildNumber);
                
                VRCModLogger.Log("[ModConfigPage] Editing QuickMenu's Settings button");
                
                Transform settingsButtonTransform = QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton");
                settingsButtonTransform.GetComponentInChildren<Text>().text = "Mod/Game\nSettings";
                settingsButtonTransform.GetComponent<UiTooltip>().text = "Tune Control, Audio, Video and Mod Settings. Log Out or Quit.";

                VRCModLogger.Log("[ModConfigPage] Editing QuickMenu's InfoBar");

                Transform infobarpanelTransform = QuickMenuUtils.GetQuickMenuInstance().transform.Find("QuickMenu_NewElements/_InfoBar/Panel");
                RectTransform infobarpanelRectTransform = infobarpanelTransform.GetComponent<RectTransform>();
                infobarpanelRectTransform.sizeDelta = new Vector2(infobarpanelRectTransform.sizeDelta.x, infobarpanelRectTransform.sizeDelta.y + 80);
                infobarpanelRectTransform.anchoredPosition = new Vector2(infobarpanelRectTransform.anchoredPosition.x, infobarpanelRectTransform.anchoredPosition.y - 40);


                VRCModLogger.Log("[ModConfigPage] Setting up SettingsMenu");

                Transform cameraMenuTransform = QuickMenuUtils.GetQuickMenuInstance().transform.Find("CameraMenu");
                Transform settingsMenuTransform = Instantiate(cameraMenuTransform, QuickMenuUtils.GetQuickMenuInstance().transform);
                settingsMenuTransform.name = "SettingsMenu";

                Button.ButtonClickedEvent showGameConfigMenu = settingsButtonTransform.GetComponent<Button>().onClick;
                settingsButtonTransform.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                settingsButtonTransform.GetComponent<Button>().onClick.AddListener(() =>
                {
                    QuickMenuUtils.ShowQuickmenuPage("SettingsMenu");
                });

                VRCModLogger.Log("[ModConfigPage] Editing QuickMenu's SettingsMenu buttons");

                int i = 0;
                foreach (Transform child in settingsMenuTransform)
                {
                    if (child != null)
                    {
                        if (i == 0)
                        {
                            child.name = "Game Settings";
                            child.GetComponentInChildren<Text>().text = "Game\nSettings";
                            child.GetComponent<UiTooltip>().text = "Tune Control, Audio and Video Settings. Log Out or Quit.";
                            child.GetComponent<Button>().onClick = showGameConfigMenu;
                        }
                        else if (i == 1)
                        {
                            child.name = "Mod Settings";
                            child.GetComponentInChildren<Text>().text = "Mod\nSettings";
                            child.GetComponent<UiTooltip>().text = "Enable Features or Configure Installed Mods";
                            child.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                            child.GetComponent<Button>().onClick.AddListener(() =>
                            {
                                VRCUiManagerUtils.GetVRCUiManager().ShowUi(false, true);
                                ModManager.StartCoroutine(QuickMenuUtils.PlaceUiAfterPause());
                                VRCUiManagerUtils.GetVRCUiManager().ShowScreen("UserInterface/MenuContent/Screens/ModConfig");
                            });

                        }
                        else if (child.name != "BackButton")
                            Destroy(child.gameObject);
                    }
                    i++;
                }

            }
            else
            {
                VRCModLogger.Log("[ModConfigPage] UserInterface/MenuContent/Screens/Avatar is null");
            }
        }



        void Awake()
        {
            scrollContent = UnityUiUtils.CreateScrollView(GetComponent<RectTransform>(), 1500, 850, 0, 875, false, true); // 1000 -> 800
            scrollContent.parent.parent.localPosition = new Vector2(0, 62);

            CreateButton("Apply", -300, () =>
            {
                ModPrefs.SaveConfigs();
                VRCUiManagerUtils.GetVRCUiManager().CloseUi(true);
                VRCUiCursorManager.SetUiActive(false);
                ModComponent.OnModSettingsApplied();
            });
            CreateButton("Close",  300, () =>
            {
                ResetConfigs();
                VRCUiManagerUtils.GetVRCUiManager().CloseUi(true);
                VRCUiCursorManager.SetUiActive(false);
            });

            SetupConfigs();
        }

        private void ResetConfigs()
        {
            foreach(IConfigElement element in configElements)
            {
                element.ResetValue();
            }
        }

        private void CreateButton(string text, int xoffset, Action onClick)
        {
            Transform baseButtonTransform = QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/CloseButton") ?? QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/SettingsButton");
            if (baseButtonTransform != null)
            {
                Transform modconf = UnityUiUtils.DuplicateButton(baseButtonTransform, text, new Vector2(0, 0));
                modconf.name = "ModConfigsButton (" + text + ")";
                modconf.GetComponentInChildren<RectTransform>().sizeDelta = new Vector2(300, 100);
                modconf.GetComponentInChildren<Text>().color = Color.white;
                //modconf.GetComponent<Button>().interactable = false;
                modconf.GetComponent<Button>().onClick.RemoveAllListeners();
                modconf.GetComponent<Button>().onClick.AddListener(() => onClick());
                modconf.GetComponent<RectTransform>().SetParent(transform, true);
                modconf.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
                modconf.GetComponent<RectTransform>().anchoredPosition = new Vector2(xoffset, -440);
                modconf.GetComponent<RectTransform>().localRotation = Quaternion.identity;
                modconf.GetComponent<RectTransform>().localScale = Vector3.one;
                modconf.GetComponentInChildren<Text>().fontSize = 30;
            }
            else
            {
                VRCModLogger.Log("[ModConfigPage] QuickMenu/ShortcutMenu/SettingsButton and QuickMenu/ShortcutMenu/SettingsButton are null");
            }
        }

        internal void SetupConfigs()
        {
            VRCModLogger.Log("[ModConfigPage] SetupConfigs");
            VRCModLogger.Log("[ModConfigPage] Layer: " + scrollContent.gameObject.layer);
            //clear content
            //foreach (Transform child in scrollContent) Destroy(child);

            //foreach elements registered in ModPrefs, add a config element
            float totalHeight = 0;
            
            Dictionary<string, Dictionary<string, ModPrefs.PrefDesc>> modPrefs = ModPrefs.GetPrefs();
            for (int i = 0; i < modPrefs.Count; i++) {
                bool categoryCreated = false;
                KeyValuePair<string, Dictionary<string, ModPrefs.PrefDesc>> modPrefCategory = modPrefs.ElementAt(i);

                foreach (KeyValuePair<string, ModPrefs.PrefDesc> pref in modPrefCategory.Value)
                {
                    if (!pref.Value.Hidden)
                    {
                        //create a new category
                        if (!categoryCreated)
                        {
                            categoryCreated = !categoryCreated;
                            CreateCategoryTitle(ModPrefs.GetCategoryDisplayName(modPrefCategory.Key), ref totalHeight);
                        }

                        //add all prefs under this category
                        CreatePref(pref.Value, ref totalHeight);
                    }
                }
            }
            
            scrollContent.GetComponent<RectTransform>().sizeDelta = new Vector2(scrollContent.GetComponent<RectTransform>().sizeDelta.x, totalHeight > 800 ? totalHeight : 800); // 1000 -> 800
        }

        private void CreateCategoryTitle(string title, ref float totalHeight)
        {
            GameObject text = new GameObject("CategoryTitle ("+title+")", typeof(RectTransform), typeof(Text));
            text.transform.SetParent(scrollContent, false);
            text.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 90);
            text.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1.0f);
            text.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1.0f);
            text.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1.0f);
            text.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -totalHeight);

            text.GetComponent<Text>().font = QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/BuildNumText").GetComponent<Text>().font;
            text.GetComponent<Text>().fontSize = 70;
            text.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            text.GetComponent<Text>().alignByGeometry = true;
            text.GetComponent<Text>().text = title;
            text.GetComponent<Text>().color = Color.yellow;

            totalHeight += 90;
        }

        private void CreatePref(ModPrefs.PrefDesc pref, ref float totalHeight)
        {
            GameObject textName = new GameObject("PrefName (" + pref.DisplayText + ")", typeof(RectTransform), typeof(Text));
            textName.transform.SetParent(scrollContent, false);
            textName.GetComponent<RectTransform>().sizeDelta = new Vector2(675f, 70f);
            textName.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1.0f);
            textName.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1.0f);
            textName.GetComponent<RectTransform>().pivot = new Vector2(1f, 1.0f);
            textName.GetComponent<RectTransform>().anchoredPosition = new Vector2(-75, -totalHeight);

            textName.GetComponent<Text>().font = QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/BuildNumText").GetComponent<Text>().font;
            textName.GetComponent<Text>().fontSize = 50;
            textName.GetComponent<Text>().alignment = TextAnchor.MiddleRight;
            textName.GetComponent<Text>().alignByGeometry = true;
            textName.GetComponent<Text>().text = pref.DisplayText;

            if (pref.Type == ModPrefs.PrefType.BOOL)
            {
                UIToggleSwitch toggle = UnityUiUtils.CreateUIToggleSwitch(scrollContent.GetComponent<RectTransform>());
                toggle.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1.0f);
                toggle.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1.0f);
                toggle.GetComponent<RectTransform>().pivot = new Vector2(0.0f, 1.0f);
                toggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(75f, -totalHeight);
                toggle.GetComponent<Toggle>().isOn = pref.Value == "1";
                toggle.OnChange = (isOn) => pref.ValueEdited = isOn ? "1" : "0";
            }
            else
            {
                GameObject textValue = new GameObject("PrefValue (" + pref.DisplayText + ")", typeof(RectTransform), typeof(Text));
                textValue.transform.SetParent(scrollContent, false);
                textValue.GetComponent<RectTransform>().sizeDelta = new Vector2(675f, 70f);
                textValue.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 1.0f);
                textValue.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1.0f);
                textValue.GetComponent<RectTransform>().pivot = new Vector2(0f, 1.0f);
                textValue.GetComponent<RectTransform>().anchoredPosition = new Vector2(75f, -totalHeight);

                textValue.GetComponent<Text>().font = QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/BuildNumText").GetComponent<Text>().font;
                textValue.GetComponent<Text>().fontSize = 50;
                textValue.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
                textValue.GetComponent<Text>().alignByGeometry = true;
                textValue.GetComponent<Text>().text = pref.Value;
            }

            totalHeight += 70;

        }

    }
}
