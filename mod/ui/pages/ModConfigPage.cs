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

            GameObject screens = GameObject.Find("UserInterface/MenuContent/Screens");
            GameObject avatarscreen = GameObject.Find("UserInterface/MenuContent/Screens/Avatar");
            if (avatarscreen != null)
            {
                GameObject go = new GameObject("ModConfig", typeof(RectTransform), typeof(VRCUiPage));
                go.transform.SetParent(screens.transform, false);
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
                    VRCModLogger.Log("[ModConfigPage] QuickMenu/ShortcutMenu/CloseButton is null");
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
            Transform baseButtonTransform = QuickMenuUtils.GetQuickMenuInstance().transform.Find("ShortcutMenu/CloseButton");
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
                modconf.GetComponent<RectTransform>().anchoredPosition = new Vector2(xoffset, -440);
                modconf.GetComponent<RectTransform>().localRotation = Quaternion.identity;
                modconf.GetComponent<RectTransform>().localScale = Vector3.one;
                modconf.GetComponentInChildren<Text>().fontSize = 30;
            }
            else
            {
                VRCModLogger.Log("[ModConfigPage] QuickMenu/ShortcutMenu/CloseButton is null");
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



        // DEBUG

        private Rect inspectorBox = new Rect(20, 20, 700, 700);
        private Vector2 inspectorScroll;

        void OnGUI()
        {
            //inspectorBox = GUI.Window(1, inspectorBox, OnGUIInspector, "Inspector"); // DEBUG
        }

        private void OnGUIInspector(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 600, 26));
            inspectorScroll = GUILayout.BeginScrollView(inspectorScroll);

            ListAllGO(transform, 0);
            /*
            foreach (GameObject rootGO in GetComponent<RectTransform>())
            {
                //TODO list all hierarchy
                ListAllGO(rootGO.transform, 0);
            }
            */
            GUILayout.EndScrollView();
        }

        private void ListAllGO(Transform gameObject, int depth)
        {
            GUIStyle bs = new GUIStyle(GUI.skin.button);
            bs.margin.left += depth * 10;
            GUILayout.Button(gameObject.name + "[" + gameObject.position.x + "|" + gameObject.position.y + "|" + gameObject.position.z + "]", bs, GUILayout.Width(400));
            foreach (Transform child in gameObject.transform)
            {
                ListAllGO(child, depth + 1);
            }
        }



    }
}
