namespace VRCTools
{
    internal interface IConfigElement
    {
        void SetConfigPref(ModPrefs.PrefDesc pref);
        void ResetValue();
    }
}