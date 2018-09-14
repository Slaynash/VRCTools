using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRCModLoader;

#pragma warning disable CS0618
namespace VRCTools
{
    public static class ModPrefs
    {
        private static Dictionary<string, Dictionary<string, PrefDesc>> prefs = new Dictionary<string, Dictionary<string, PrefDesc>>();
        private static Dictionary<string, string> categoryDisplayNames = new Dictionary<string, string>();

        public static void RegisterCategory(string name, string displayText)
        {
            categoryDisplayNames[name] = displayText;
        }


        public static void RegisterPrefString(string section, string name, string defaultValue, string displayText = null, bool hideFromList = false)
        {
            RegisterPref(section, name, defaultValue, displayText, PrefType.STRING, hideFromList);
        }

        public static void RegisterPrefBool(string section, string name, bool defaultValue, string displayText = null, bool hideFromList = false)
        {
            RegisterPref(section, name, defaultValue ? "1" : "0", displayText, PrefType.BOOL, hideFromList);
        }

        public static void RegisterPrefInt(string section, string name, int defaultValue, string displayText = null, bool hideFromList = false)
        {
            RegisterPref(section, name, "" + defaultValue, displayText, PrefType.INT, hideFromList);
        }

        public static void RegisterPrefFloat(string section, string name, float defaultValue, string displayText = null, bool hideFromList = false)
        {
            RegisterPref(section, name, "" + defaultValue, displayText, PrefType.FLOAT, hideFromList);
        }

        public static void RegisterPrefColor(string section, string name, Color defaultValue, string displayText = null, bool hideFromList = false)
        {
            RegisterPref(section, name, "#" + ColorUtility.ToHtmlStringRGBA(defaultValue), displayText, PrefType.COLOR, hideFromList);
        }


        private static void RegisterPref(string section, string name, string defaultValue, string displayText, PrefType type, bool hideFromList)
        {
            if (prefs.TryGetValue(section, out Dictionary<string, PrefDesc> prefsInSection))
            {
                if (prefsInSection.TryGetValue(name, out PrefDesc pref))
                {
                    VRCModLogger.LogError("Trying to registered ModPref " + section + ":" + name + " more than one time");
                }
                else
                {
                    string toStoreValue = defaultValue;
                    if (VRCModLoader.ModPrefs.HasKey(section, name))
                        toStoreValue = VRCModLoader.ModPrefs.GetString(section, name, defaultValue);
                    else VRCModLoader.ModPrefs.SetString(section, name, defaultValue);
                    prefsInSection.Add(name, new PrefDesc(toStoreValue, type, hideFromList, (displayText ?? "") == "" ? name : displayText));
                }
            }
            else
            {
                Dictionary<string, PrefDesc> dic = new Dictionary<string, PrefDesc>();
                string toStoreValue = defaultValue;
                if (VRCModLoader.ModPrefs.HasKey(section, name))
                    toStoreValue = VRCModLoader.ModPrefs.GetString(section, name, defaultValue);
                else VRCModLoader.ModPrefs.SetString(section, name, defaultValue);
                dic.Add(name, new PrefDesc(toStoreValue, type, hideFromList, (displayText ?? "") == "" ? name : displayText));
                prefs.Add(section, dic);
            }
        }

        public static bool HasKey(string section, string name)
        {
            return prefs.TryGetValue(section, out Dictionary<string, PrefDesc> prefsInSection) && prefsInSection.ContainsKey(name);
        }


        internal static Dictionary<string, Dictionary<string, PrefDesc>> GetPrefs()
        {
            return prefs;
        }

        internal static string GetCategoryDisplayName(string key)
        {
            if (categoryDisplayNames.TryGetValue(key, out string name)) return name;
            return key;
        }

        internal static void SaveConfigs()
        {
            foreach (KeyValuePair<string, Dictionary<string, PrefDesc>> prefsInSection in prefs)
            {
                foreach (KeyValuePair<string, PrefDesc> pref in prefsInSection.Value)
                {
                    pref.Value.Value = pref.Value.ValueEdited;
                    VRCModLoader.ModPrefs.SetString(prefsInSection.Key, pref.Key, pref.Value.Value);
                }
            }
            VRCModLogger.Log("[ModPrefs] Configs saved !");
        }


        // GETTERS

        public static string GetString(string section, string name)
        {
            if (prefs.TryGetValue(section, out Dictionary<string, PrefDesc> prefsInSection) && prefsInSection.TryGetValue(name, out PrefDesc pref))
                return pref.Value;
            VRCModLogger.LogError("Trying to get unregistered ModPref " + section + ":" + name);
            return "";
        }

        public static bool GetBool(string section, string name)
        {
            if (prefs.TryGetValue(section, out Dictionary<string, PrefDesc> prefsInSection) && prefsInSection.TryGetValue(name, out PrefDesc pref))
                return pref.Value.Equals("1");
            VRCModLogger.LogError("Trying to get unregistered ModPref " + section + ":" + name);
            return false;
        }

        public static int GetInt(string section, string name)
        {
            if (prefs.TryGetValue(section, out Dictionary<string, PrefDesc> prefsInSection) && prefsInSection.TryGetValue(name, out PrefDesc pref))
                if (int.TryParse(pref.Value, out int valueI))
                    return valueI;
            VRCModLogger.LogError("Trying to get unregistered ModPref " + section + ":" + name);
            return 0;
        }

        public static float GetFloat(string section, string name)
        {
            if (prefs.TryGetValue(section, out Dictionary<string, PrefDesc> prefsInSection) && prefsInSection.TryGetValue(name, out PrefDesc pref))
                if (float.TryParse(pref.Value, out float valueF))
                    return valueF;
            VRCModLogger.LogError("Trying to get unregistered ModPref " + section + ":" + name);
            return 0.0f;
        }

        public static Color GetColor(string section, string name)
        {
            if (prefs.TryGetValue(section, out Dictionary<string, PrefDesc> prefsInSection) && prefsInSection.TryGetValue(name, out PrefDesc pref))
                if (ColorUtility.TryParseHtmlString(pref.Value, out Color valueC))
                    return valueC;
            VRCModLogger.LogError("Trying to get unregistered ModPref " + section + ":" + name);
            return Color.white;
        }


        // SETTERS

        public static void SetString(string section, string name, string value)
        {
            if (prefs.TryGetValue(section, out Dictionary<string, PrefDesc> prefsInSection) && prefsInSection.TryGetValue(name, out PrefDesc pref))
            {
                pref.Value = value;
                VRCModLoader.ModPrefs.SetString(section, name, value);
            }
            else
            {
                VRCModLogger.LogError("Trying to save unknown pref " + section + ":" + name);
            }
        }

        public static void SetBool(string section, string name, bool value)
        {
            SetString(section, name, value ? "1" : "0");
        }

        public static void SetInt(string section, string name, int value)
        {
            SetString(section, name, value.ToString());
        }

        public static void SetFloat(string section, string name, float value)
        {
            SetString(section, name, value.ToString());
        }

        public static void SetColor(string section, string name, Color value)
        {
            SetString(section, name, "#" + ColorUtility.ToHtmlStringRGBA(value));
        }

        public enum PrefType
        {
            STRING,
            BOOL,
            INT,
            FLOAT,
            COLOR
        }

        public class PrefDesc
        {
            public string Value { get; internal set; }
            public string ValueEdited { get; internal set; }
            public PrefType Type { get; private set; }
            public bool Hidden { get; private set; }
            public String DisplayText { get; private set; }

            public PrefDesc(string value, PrefType type, bool hidden, string displayText)
            {
                Value = value;
                ValueEdited = value;
                Type = type;
                Hidden = hidden;
                DisplayText = displayText;
            }
        }
    }
}
#pragma warning restore CS0618
