using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace VRCTools
{
    public static class QuickMenuUtils
    {
        private static QuickMenu quickmenuInstance;

        public static QuickMenu GetQuickMenuInstance()
        {
            MethodInfo quickMenuInstanceGetter = typeof(QuickMenu).GetMethod("get_Instance", BindingFlags.Public | BindingFlags.Static);
            if (quickMenuInstanceGetter == null)
                return null;
            if (quickmenuInstance == null)
                quickmenuInstance = ((QuickMenu)quickMenuInstanceGetter.Invoke(null, new object[] { }));
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
    }
}
