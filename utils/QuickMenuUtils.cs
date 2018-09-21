using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using VRCModLoader;

namespace VRCTools
{
    public static class QuickMenuUtils
    {
        private static QuickMenu quickmenuInstance;
        //private static QuickMenuContextualDisplay quickmenuContextualDisplay;
        private static Type contextType;
        private static FieldInfo currentPageGetter;
        private static FieldInfo quickmenuContextualDisplayGetter;

        public static QuickMenu GetQuickMenuInstance()
        {
            if (quickmenuInstance == null)
            {
                MethodInfo quickMenuInstanceGetter = typeof(QuickMenu).GetMethod("get_Instance", BindingFlags.Public | BindingFlags.Static);
                if (quickMenuInstanceGetter == null)
                    return null;
                quickmenuInstance = ((QuickMenu)quickMenuInstanceGetter.Invoke(null, new object[] { }));
            }
            return quickmenuInstance;
        }

        //Copied from QuickMenu
        public static IEnumerator PlaceUiAfterPause()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            VRCUiManagerUtils.GetVRCUiManager().PlaceUi();
            GameObject.Find("UserInterface/MenuContent/Backdrop/Header").gameObject.SetActive(false);
            yield break;
        }

        //Partial reproduction of SetMenuIndex from QuickMenu
        internal static void ShowQuickmenuPage(string pagename)
        {
            QuickMenu quickmenu = GetQuickMenuInstance();
            Transform pageTransform = quickmenu?.transform.Find(pagename);
            if(pageTransform == null)
            {
                VRCModLogger.LogError("[QuickMenuUtils] pageTransform is null !");
            }

            if (currentPageGetter == null)
            {
                GameObject shortcutMenu = quickmenu.transform.Find("ShortcutMenu").gameObject;
                FieldInfo[] fis = typeof(QuickMenu).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where((fi) => fi.FieldType == typeof(GameObject)).ToArray();
                VRCModLogger.Log("[QuickMenuUtils] GameObject Fields in QuickMenu:");
                int count = 0;
                foreach (FieldInfo fi in fis)
                {
                    GameObject value = fi.GetValue(quickmenu) as GameObject;
                    //VRCModLogger.Log("[QuickMenuUtils]  - " + fi.Name + " : " + value);
                    if (value == shortcutMenu && ++count == 2)
                    {
                        VRCModLogger.Log("[QuickMenuUtils] currentPage field: " + fi.Name);
                        currentPageGetter = fi;
                        break;
                    }
                }
                if(currentPageGetter == null)
                {
                    VRCModLogger.LogError("[QuickMenuUtils] Unable to find field currentPage in QuickMenu");
                    return;
                }
            }

            ((GameObject)currentPageGetter.GetValue(quickmenu))?.SetActive(false);
            GetQuickMenuInstance().transform.Find("QuickMenu_NewElements/_InfoBar").gameObject.SetActive(false);

            if (quickmenuContextualDisplayGetter != null)
                quickmenuContextualDisplayGetter = typeof(QuickMenu).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault((fi) => fi.FieldType == typeof(QuickMenuContextualDisplay));
            QuickMenuContextualDisplay quickmenuContextualDisplay = quickmenuContextualDisplayGetter?.GetValue(quickmenu) as QuickMenuContextualDisplay;
            if (quickmenuContextualDisplay != null)
                quickmenuContextualDisplay.SetDefaultContext(0);
            
            currentPageGetter.SetValue(quickmenu, pageTransform.gameObject);
            typeof(QuickMenu).GetMethod("SetContext", BindingFlags.Public | BindingFlags.Instance).Invoke(quickmenu, new object[] { 1, null, null }); // This is the only way to pass the unknown enum type value
            pageTransform.gameObject.SetActive(true);
        }
    }
}
