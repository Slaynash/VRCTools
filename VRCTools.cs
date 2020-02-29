using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using VRCModLoader;
using VRCModNetwork;
using static UnityEngine.UI.Button;

using UnityEngine.SceneManagement;
using Harmony;
using Harmony.ILCopying;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.IO;
using VRCTools.IL;

namespace VRCTools
{
    [VRCModInfo("VRCTools", "0.10.3", "Slaynash")]
    public class VRCTools : VRCMod
    {
        private bool usingVRCMenuUtils = false;
        private bool initializing = false;

        public static bool Initialized { get; private set; }


        private void OnApplicationStart() {
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                string lp = "";
                bool first = true;
                foreach (var lp2 in Environment.GetCommandLineArgs())
                {
                    if (first) first = false;
                    else lp += " " + lp2;
                }
                VRCModLogger.Log("[VRCTools] Launch parameters:" + lp);
            }

            ModPrefs.RegisterCategory("vrctools", "VRCTools");
            ModPrefs.RegisterPrefBool("vrctools", "enabledebugconsole", false, "Enable Debug Console");

            Type vrcMenuUtilsAPI = null;
            usingVRCMenuUtils = AppDomain.CurrentDomain.GetAssemblies().Any(a =>
            {
                vrcMenuUtilsAPI = a.GetType("VRCMenuUtils.VRCMenuUtilsAPI");
                return vrcMenuUtilsAPI != null;
            });

            VRCModLogger.Log("[VRCTools] Using VRCMenuUtils: " + usingVRCMenuUtils);

            if (usingVRCMenuUtils)
            {
                vrcMenuUtilsAPI.GetMethod("RunBeforeFlowManager").Invoke(null, new object[] { VRCToolsSetup() });
            }

            if (HarmonyLoaded())
            {
                VRCModLogger.Log("[VRCTools] Patching analytics");
                HarmonyInstance harmony = HarmonyInstance.Create("vrctools.analyticspatch");
                harmony.Patch(
                    typeof(AppDomain).GetMethod("GetAssemblies", BindingFlags.Public | BindingFlags.Instance),
                    postfix: new HarmonyMethod(typeof(VRCTools).GetMethod("GetAssembliesPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
                harmony.Patch(
                    typeof(Analytics).GetMethods(BindingFlags.NonPublic | BindingFlags.Static).FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string) && (string)m.Parse().First( i => i.OpCode == OpCodes.Ldstr).Argument == "-"),
                    prefix: new HarmonyMethod(typeof(VRCTools).GetMethod("GetMD5FromFilePrefix", BindingFlags.Static | BindingFlags.NonPublic)));
                VRCModLogger.Log("[VRCTools] Analytics patched.");
            }
        }

        private static void GetAssembliesPostfix(ref Assembly[] __result)
        {
            System.Diagnostics.StackFrame[] stackFrames = new System.Diagnostics.StackTrace().GetFrames();
            Type callingType = stackFrames[stackFrames.Length - 2].GetMethod().DeclaringType.DeclaringType;
            if (callingType != typeof(Analytics))
                return;

            VRCModLogger.Log("[VRCTools | Analytics patch] Processing assemblies");

            List<Assembly> assemblies = new List<Assembly>();
            foreach (Assembly assembly in __result)
            {
                if (assembly.GetName().Name == "HarmonySharedState" || assembly.GetName().Name == "VRCModLoader" || assembly.GetName().Name == "VRChat_Enhancer" || assembly.GetName().Name == "RubyLoader" || assembly.GetName().Name == "RubyCore" || !File.Exists(assembly.Location))
                {
                    Console.WriteLine("[VRCTools | Analytics patch] Discarding assembly " + assembly.GetName().Name);
                    continue;
                }

                assemblies.Add(assembly);
            }
            __result = assemblies.ToArray();
        }

        private static bool GetMD5FromFilePrefix(string __0, ref string __result)
        {
            if (__0 == typeof(UnityEngine.Debug).Assembly.Location)
            {
                VRCModLogger.Log("[VRCTools | Analytics patch] Faked UnityEngine.CoreModule hash");
                __result = "41810f2e5d5ee1b3eb78866bde797de9";
                return false;
            }
            return true;
        }
        /*
        private static IEnumerable<CodeInstruction> AnalyticsPatch(ILGenerator ilg, IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] newInst = new CodeInstruction[instructions.Count()];

            int cnt = 0;

            for (int i = 0; i < newInst.Length; i++)
            {
                CodeInstruction instruction = instructions.ElementAt(i);
                if (instruction.opcode == OpCodes.Call && (++cnt) == 1)
                {
                    newInst[i] = new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(VRCTools), "GetAssembliesPatched"));
                }
                else
                    newInst[i] = instruction;
            }

            return newInst.AsEnumerable();
        }

        public static Assembly[] GetAssembliesPatched()
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "HarmonySharedState" || assembly.GetName().Name == "VRCModLoader" || assembly.GetName().Name == "VRChat_Enhancer" || assembly.GetName().Name == "RubyLoader" || assembly.GetName().Name == "RubyCore" || !File.Exists(assembly.Location))
                {
                    Console.WriteLine("[VRChat | Analytics] Discarding assembly " + assembly.GetName().Name + ".");
                    continue;
                }

                if(assembly.GetName().Name == "UnityEngine.CoreModule")
                {
                    assembly.
                }

                assemblies.Add(assembly);
            }
        }
        */
        private void OnLevelWasLoaded(int level)
        {
            if (!usingVRCMenuUtils && level == (Application.platform == RuntimePlatform.WindowsPlayer ? 0 : 2) && !Initialized && !initializing)
            {
                VRCFlowManagerUtils.DisableVRCFlowManager();
                ModManager.StartCoroutine(VRCToolsSetup());
            }
        }

        private IEnumerator VRCToolsSetup()
        {
            VRCModLogger.Log("[VRCTools] Initialising VRCTools");
            VRCModLogger.Log("[VRCTools] Current scene: " + SceneManager.GetActiveScene().name + "(index: " + SceneManager.GetActiveScene().buildIndex + ", path: " + SceneManager.GetActiveScene().path + ")");
            VRCModLogger.Log("[VRCTools] ModComponent Sibling index: " + ModComponent.Instance.transform.GetSiblingIndex());
            VRCModLogger.Log("[VRCTools] Root gameobjects:");
            foreach(GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
                VRCModLogger.Log(" - " + g);
            VRCModLogger.Log("[VRCTools] Call trace - THIS IS NOT AN ERROR:");
            VRCModLogger.Log(new System.Diagnostics.StackTrace().ToString());
            initializing = true;

            yield return VRCUiManagerUtils.WaitForUiManagerInit();

            if(!HarmonyLoaded())
            {
                bool waitforpopup = true;
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Missing library: Harmony. Please install it using the VRChat Mod Manager (see #how-to on discord.gg/rCqKSvR)", "Close game", () => Application.Quit(), "Ignore", () => waitforpopup = false);
                while (waitforpopup)
                    yield return null;

                Initialized = true;
                if (!usingVRCMenuUtils)
                    VRCFlowManagerUtils.EnableVRCFlowManager();

                yield break;
            }
            
            VRCModLogger.Log("[VRCTools] Overwriting login button event");
            VRCUiPageAuthentication loginPage = Resources.FindObjectsOfTypeAll<VRCUiPageAuthentication>().FirstOrDefault((page) => page.gameObject.name == "LoginUserPass");
            if (loginPage != null)
            {
                Button loginButton = loginPage.transform.Find("ButtonDone (1)")?.GetComponent<Button>();
                if (loginButton != null)
                {
                    ButtonClickedEvent bce = loginButton.onClick;
                    loginButton.onClick = new ButtonClickedEvent();
                    loginButton.onClick.AddListener(() => {
                        VRCModNetworkManager.SetCredentials(Uri.EscapeDataString(GetTextFromUiInputField(loginPage.loginUserName)) + ":" + Uri.EscapeDataString(GetTextFromUiInputField(loginPage.loginPassword)));
                        bce?.Invoke();
                    });
                }
                else
                    VRCModLogger.Log("[VRCTools] Unable to find login button in login page");
            }
            else
                VRCModLogger.Log("[VRCTools] Unable to find login page");

            yield return VRCModLoaderUpdater.CheckVRCModLoaderHash();
            yield return VRCToolsAutoUpdater.CheckAndUpdate();

            try
            {
                VRCModNetworkStatus.Setup();
                VRCModNetworkLogin.SetupVRCModNetworkLoginPage();
                ModConfigPage.Setup();
                ModdedUsersManager.Init();
            } catch (Exception ex)
            {
                VRCModLogger.Log("[VRCTools]" + ex.ToString());
            }

            VRCModLogger.Log("[VRCTools] Injecting VRCModNetwork login page");
            VRCModNetworkLogin.InjectVRCModNetworkLoginPage();

            yield return VRCModNetworkManager.ConnectInit();

            VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();

            Initialized = true;
            initializing = false;

            if (!usingVRCMenuUtils)
                VRCFlowManagerUtils.EnableVRCFlowManager();

        }

        private bool HarmonyLoaded()
        {
            return AppDomain.CurrentDomain.GetAssemblies().ToList().Any(a => a.GetName().ToString().StartsWith("0Harmony"));
        }

        private string GetTextFromUiInputField(UiInputField field)
        {
            FieldInfo textField = typeof(UiInputField).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(f => f.FieldType == typeof(string) && f.Name != "placeholderInputText");
            return textField.GetValue(field) as string;
        }

        private void OnUpdate()
        {
            if (!Initialized) return;
            VRCModNetworkManager.Update();
            VRCModNetworkStatus.Update();
            ModdedUsersManager.Update();

            /*
            if(Input.GetKeyDown(KeyCode.F2))
            {
                VRCModLogger.Log(SteamUtils.GetSteamTicket());
            }
            */
        }
    }
}
