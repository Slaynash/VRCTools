using Harmony;
using Harmony.ILCopying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using VRC;
using VRCModLoader;
using VRCSDK2;

namespace VRCTools
{
    class BasicLogoutLogAndPatch
    {

        internal static readonly BindingFlags FlagMinusOne = (BindingFlags)(-1);

        private static HarmonyMethod GetPatch(string name) => new HarmonyMethod(typeof(Patches).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));

        public static void ApplyPatches()
        {
            /*
            try
            {
                VRCModLogger.Log("[BasicLogoutLogAndPatch] Applying basic logout patch and logging (Made by Magic3000)");

                MethodInfo methodInfo = typeof(VRC_EventDispatcherRFC).GetMethods(FlagMinusOne).FirstOrDefault((m) =>
                {
                    ParameterInfo[] parameters = m.GetParameters();
                    if (parameters.Length == 6 && parameters[0].ParameterType == typeof(VRC_EventHandler.VrcBroadcastType) && parameters[1].ParameterType == typeof(int) && parameters[2].ParameterType == typeof(VRC_EventHandler.VrcTargetType) && parameters[3].ParameterType == typeof(GameObject) && parameters[4].ParameterType == typeof(string) && parameters[5].ParameterType == typeof(byte[]))
                        return m.Parse().FirstOrDefault((x) => x.OpCode == OpCodes.Ldstr).GetArgument<string>().StartsWith("SendRPC");
                    return false;
                });
                HarmonyInstance harmonyInstance = HarmonyInstance.Create("vrctools.voidpatch");
                harmonyInstance.Patch(methodInfo, GetPatch("ModerationManagerPrefix"), null, null);
            }
            catch (Exception e)
            {
                VRCModLogger.LogError("[BasicLogoutLogAndPatch] Error while patching client " + e);
            }
            */
        }

        private static bool ModerationManagerPrefix(ref int __1, string __4, byte[] __5)
        {
            bool flag = __5.Length > 5000;
            if (flag)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                VRCModLogger.Log("[BasicLogoutLogAndPatch] Logout data detected by " + PlayerManager.GetPlayer(__1) + " " + __1);
                Console.ForegroundColor = ConsoleColor.White;
            }
            return true;
        }



    }
}
