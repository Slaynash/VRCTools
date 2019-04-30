using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using VRCModLoader;
using VRCTools.IL;

namespace VRCTools
{
    internal static class OculusUtils
    {
        private static Type t_Oculus_Platform_Callback = null;
        private static MethodInfo m_HandleMessage = null;
        private static Dictionary<ulong, Action<bool, string>> callbackActions = new Dictionary<ulong, Action<bool, string>>();

        public static string OculusName { get; private set; }
        public static ulong OculusId { get; private set; }

        public static void GetAccessToken(Action<string> onSuccess, Action<string> onError)
        {
            VRCModLogger.LogError("[VRCTools] [OculusUtils] Adding GetAccessToken callback to list");
            lock (callbackActions)
            {
                callbackActions.Add(ovr_User_GetAccessToken(), new Action<bool, string>((success, message) =>
                {
                    if (success)
                        onSuccess?.Invoke(message);
                    else
                        onError?.Invoke(message);
                }));
            }
        }


        internal static void ApplyPatches()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("vrctools.oculuspatchs");

            // Patch oculus lib callbacks
            VRCModLogger.Log("[VRCTools] [OculusUtils] Applying Oculus lib patches");
            MethodInfo callbackHandleMessageMethod = GetCallbackHandleMessageMethod();
            if (callbackHandleMessageMethod == null)
            {
                VRCModLogger.LogError("[VRCTools] [OculusUtils] Patching of Oculus.Platform.Callback.HandleMessage(Message msg) failed: Method Not found");
                return;
            }

            try
            {
                harmonyInstance.Patch(callbackHandleMessageMethod, new HarmonyMethod(typeof(OculusUtils).GetMethod("HandleMessagePrefix", BindingFlags.NonPublic | BindingFlags.Static)));
            }
            catch(Exception e)
            {
                VRCModLogger.LogError("[VRCTools] [OculusUtils] An error has occured while patching Oculus.Platform.Callback.HandleMessage: " + e);
                return;
            }

            // Patch oculus user id / name fetch
            MethodInfo oculusInfoSetMethod = typeof(VRCFlowManagerVRC).GetFirstMethodContainingString(" oculus id: ");
            try
            {
                harmonyInstance.Patch(oculusInfoSetMethod, null, null, new HarmonyMethod(typeof(OculusUtils).GetMethod("OculusInfoSetTranspiler", BindingFlags.NonPublic | BindingFlags.Static)));
            }
            catch (Exception e)
            {
                VRCModLogger.LogError("[VRCTools] [OculusUtils] An error has occured while patching Oculus.Platform.Callback.HandleMessage: " + e);
                return;
            }
        }

        private static bool HandleMessagePrefix(object __1)
        {
            VRCModLogger.LogError("[VRCTools] [OculusUtils] Handling oculus message");
            try
            {
                ulong requestId = (ulong)__1.GetType().BaseType.BaseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).First(f => f.FieldType == typeof(ulong)).GetValue(__1);
                VRCModLogger.LogError("[VRCTools] [OculusUtils] requestId: " + requestId);
                lock (callbackActions)
                {
                    if (callbackActions.TryGetValue(requestId, out Action<bool, string> action))
                    {
                        VRCModLogger.LogError("[VRCTools] [OculusUtils] Found request with id " + requestId);
                        try
                        {
                            string data = (string)__1.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).First().Invoke(__1, new object[] { });
                            VRCModLogger.LogError("[VRCTools] [OculusUtils] data: " + data ?? "(null)");
                            action?.Invoke(data == null, data);
                        }
                        catch (Exception e)
                        {
                            VRCModLogger.LogError("[VRCTools] [OculusUtils] An error has occured while handling a callback of Oculus.Platform.Callback.HandleMessage: " + e);
                            return true;
                        }
                        callbackActions.Remove(requestId);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                VRCModLogger.LogError("[VRCTools] [OculusUtils] An error has occured while handling Oculus.Platform.Callback.HandleMessage call: " + e);
            }
            return true;
        }

        private static IEnumerable<CodeInstruction> OculusInfoSetTranspiler(ILGenerator ilg, IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            int ldfldEncountered = 0;
            for (int i = 0; i < instructionList.Count; i++)
            {
                if(instructionList[i].opcode == OpCodes.Ldfld)
                {
                    instructionList.InsertRange(i+1, new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OculusUtils), ldfldEncountered == 0 ? "SetOculusUserId" : "SetOculusUserName")),
                        instructionList[i-2],
                        instructionList[i-1],
                        instructionList[i],
                    });
                    i += 4;
                    ldfldEncountered++;
                    if (ldfldEncountered == 2)
                        break;
                }
            }

            return instructionList;
        }

        private static void SetOculusUserId(ulong value)
        {
            VRCModLogger.LogError("[VRCTools] [OculusUtils] OculusId: " + value);
            OculusId = value;
        }
        private static void SetOculusUserName(string value)
        {
            VRCModLogger.LogError("[VRCTools] [OculusUtils] OculusName: " + value);
            OculusName = value;
        }

        private static MethodInfo GetCallbackHandleMessageMethod()
        {
            VRCModLogger.Log("[VRCTools] [OculusUtils] Fetching Callback.HandleMessage Method");
            if (t_Oculus_Platform_Callback == null)
            {

                Type[] staticTypes = typeof(QuickMenu).Assembly.GetTypes();
                foreach (Type t in staticTypes)
                {
                    if (t.HasMethodContainingString("Cannot provide a null notification callback."))
                    {
                        t_Oculus_Platform_Callback = t;
                        break;
                    }
                }
            }
            if (t_Oculus_Platform_Callback == null) return null; // Don't have oculus classes or not found

            if (m_HandleMessage == null)
            {
                foreach (MethodInfo m in t_Oculus_Platform_Callback.GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (m.GetParameters().Length == 1)
                    {
                        m_HandleMessage = m;
                        break;
                    }
                }
            }
            return m_HandleMessage;
        }

        private static bool HasMethodContainingString(this Type instance, string s)
        {
            return instance.GetMethods((BindingFlags)(-1)).Any(m => m.Parse().Any(i => i.OpCode == OpCodes.Ldstr && i.GetArgument<string>() == s));
        }

        private static MethodInfo GetFirstMethodContainingString(this Type instance, string s)
        {
            return instance.GetMethods((BindingFlags)(-1)).First(m => m.Parse().Any(i => i.OpCode == OpCodes.Ldstr && i.GetArgument<string>() == s));
        }

        [DllImport("LibOVRPlatform64_1", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ovr_User_GetUserProof")]
        public static extern ulong ovr_User_GetAccessToken();
    }



}
