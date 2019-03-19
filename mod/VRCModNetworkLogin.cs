using Harmony;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.Core.BestHTTP;
using VRCModLoader;
using VRCModNetwork;
using static UnityEngine.UI.Button;

namespace VRCTools
{
    internal class VRCModNetworkLogin
    {

        private static bool vrcmnwDoLogin = true;
        internal static bool VrcmnwDoLogin { get => vrcmnwDoLogin; private set => vrcmnwDoLogin = value; }
        private static bool vrcmnwConnected = false;
        internal static bool VrcmnwConnected { get => vrcmnwConnected; private set => vrcmnwConnected = value; }
        private static ApiContainer vrcmnwLoginCallbackContainer = null;
        private static Action<ApiContainer> vrcmnwLoginCallback = null;
        private static GameObject vrcmnwLoginPageGO = null;

        private static UiInputField vrcmnwUsernameField;
        private static UiInputField vrcmnwPasswordField;

        private static Action popupCompleteCallback;


        private static HarmonyMethod GetPatch(string name) => new HarmonyMethod(typeof(VRCModNetworkLogin).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
        
        internal static void SetupVRCModNetworkLoginPage()
        {
            //Duplicate UI Page
            VRCModLogger.Log("[VRCTools] Creating VRCMNWLoginPage");
            GameObject vrchatLoginScreen = GameObject.Find("UserInterface/MenuContent/Screens/Authentication/LoginUserPass");
            vrcmnwLoginPageGO = GameObject.Instantiate(vrchatLoginScreen, vrchatLoginScreen.transform.parent, false);
            if (vrcmnwLoginPageGO != null)
            {
                vrcmnwLoginPageGO.name = "VRCMNWLoginPage";

                UiInputField[] fields = vrcmnwLoginPageGO.GetComponentsInChildren<UiInputField>();
                vrcmnwUsernameField = fields[0];
                vrcmnwPasswordField = fields[1];

                //Overwrite Back Button to "Skip"
                VRCModLogger.Log("[VRCTools] Overwriting Back Button");
                Button buttonBack = vrcmnwLoginPageGO.transform.Find("ButtonBack (1)")?.GetComponent<Button>();
                if (buttonBack != null)
                {
                    buttonBack.transform.GetComponentInChildren<Text>().text = "Skip";
                    buttonBack.onClick = new ButtonClickedEvent();
                    buttonBack.onClick.AddListener(() =>
                    {
                        vrcmnwDoLogin = false;
                        if (vrcmnwLoginCallback != null && vrcmnwLoginCallbackContainer != null)
                            try
                            {
                                VRCUiPopupManagerUtils.ShowPopup("VRChat", "Logging in...");
                                FinishLogin();
                            }
                            catch (Exception e)
                            {
                                VRCModLogger.Log("An error occured while calling login callback: " + e);
                            }
                        else
                            VRCModLogger.LogError("[VRCTools] vrcmnwLoginCallback or vrcmnwLoginCallbackContainer not set ! (" + (vrcmnwLoginCallback != null ? "true" : "false") + " / " + (vrcmnwLoginCallbackContainer != null ? "true" : "false") + ")");
                    });

                }
                else
                    VRCModLogger.LogError("[VRCTools] Unable to find ButtonDone (1){UnityEngine.UI.Text}");

                //Overwrite Done Button
                VRCModLogger.Log("[VRCTools] Overwriting Done Button");
                Button buttonDone = vrcmnwLoginPageGO.transform.Find("ButtonDone (1)")?.GetComponent<Button>();
                if (buttonDone != null)
                {
                    buttonDone.onClick = new ButtonClickedEvent();
                    buttonDone.onClick.AddListener(() =>
                    {
                        VRCModLogger.Log("Validating form");
                        if (InputFieldValidator.IsFormInputValid(vrcmnwLoginPageGO))
                        {
                            VRCModLogger.Log("Fetching form values");
                            FieldInfo textField = typeof(UiInputField).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(f => f.FieldType == typeof(string));
                            string username = (string)textField.GetValue(vrcmnwUsernameField);
                            string password = (string)textField.GetValue(vrcmnwPasswordField);

                            TryLoginToVRCModNetwork(username, password, (error) =>
                            {
                                string errorHR = null;
                                if (error == "INTERNAL_SERVER_ERROR")
                                    errorHR = "Internal server error";
                                else if (error == "INVALID_CREDENTIALS")
                                    errorHR = "Invalid credentials";
                                else if (error.StartsWith("BANNED_ACCOUNT"))
                                    errorHR = "Your account is currently banned. Reason: " + error.Substring("BANNED_ACCOUNT ".Length);
                                else if (error == "INVALID_VRCID")
                                    errorHR = "The current VRChat account isn't owned by this VRCModNetwork account";
                                else
                                    errorHR = error;
                                VRCUiPopupManagerUtils.ShowPopup("Login Failed", "Unable to login to the VRCModNetwork: " + errorHR, "Close", () => VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup());
                            });
                        }
                        else
                            VRCUiPopupManagerUtils.ShowPopup("Cannot Login", "Please fill out valid data for each input.", "Close", () => VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup());
                    });

                }
                else
                    VRCModLogger.LogError("[VRCTools] Unable to find ButtonDone (1){UnityEngine.UI.Text}");

                //Change "Login" title to "VRCModNetwork Login"
                VRCModLogger.Log("[VRCTools] Overwriting BoxLogin title");
                Text boxTitle = vrcmnwLoginPageGO.transform.Find("BoxLogin/Text").GetComponent<Text>();
                if (boxTitle != null)
                {
                    boxTitle.GetComponent<Localize>().enabled = false;
                    boxTitle.text = "VRCModNetwork Login";
                }
                else
                    VRCModLogger.LogError("[VRCTools] Unable to find BoxLogin/Text{UnityEngine.UI.Text}");

                VRCModLogger.Log("[VRCTools] Overwriting TextWelcome Text");
                Text textWelcome = vrcmnwLoginPageGO.transform.Find("TextWelcome").GetComponent<Text>();
                if (textWelcome != null)
                    textWelcome.text = "Welcome VRCTools User !";
                else
                    VRCModLogger.LogError("[VRCTools] Unable to find BoxLogin/Text{UnityEngine.UI.Text}");

                //Add "Register" panel
                VRCModLogger.Log("[VRCTools] Adding register panel");
                GameObject vrchatLoginCreateScreen = GameObject.Find("UserInterface/MenuContent/Screens/Authentication/LoginCreateFromWebsite");
                GameObject vrcmnwLoginCreatePageGO = GameObject.Instantiate(vrchatLoginCreateScreen, vrchatLoginCreateScreen.transform.parent, false);
                if (vrcmnwLoginCreatePageGO != null)
                {
                    vrcmnwLoginCreatePageGO.GetComponent<LaunchVRChatWebsiteRegistration>().enabled = false;
                    vrcmnwLoginCreatePageGO.name = "VRCMNLoginCreate";
                    vrcmnwLoginCreatePageGO.transform.Find("ButtonAboutUs").gameObject.SetActive(false);
                    Button buttonLogin = vrcmnwLoginCreatePageGO.transform.Find("ButtonLogin").GetComponent<Button>();
                    if (buttonLogin != null)
                    {
                        buttonLogin.onClick.RemoveAllListeners();
                        buttonLogin.onClick.AddListener(() =>
                        {
                            ShowVRCMNWLoginMenu(false);
                        });

                    }
                    else
                        VRCModLogger.LogError("[VRCTools] Unable to find ButtonLogin{UnityEngine.UI.Text}");
                }
                else
                    VRCModLogger.LogError("[VRCTools] Unable to find UserInterface/MenuContent/Screens/Authentication/LoginCreateFromWebsite");

                //Add "Register" button
                VRCModLogger.Log("[VRCTools] Adding register button");
                GameObject aboutusButton = vrcmnwLoginPageGO.transform.Find("ButtonAboutUs").gameObject;

                GameObject registerButtonGO = GameObject.Instantiate(aboutusButton, vrcmnwLoginPageGO.transform, false);
                if (registerButtonGO != null)
                {
                    Button registerButton = registerButtonGO.GetComponent<Button>();
                    registerButtonGO.GetComponentInChildren<Localize>().enabled = false;
                    registerButtonGO.GetComponentInChildren<Text>().text = "Register";
                    RectTransform rt = registerButtonGO.GetComponent<RectTransform>();
                    rt.localPosition = new Vector3(0, -270, 0);
                    rt.sizeDelta -= new Vector2(0, 30);
                    registerButton.onClick = new ButtonClickedEvent();
                    registerButton.onClick.AddListener(() =>
                    {
                        Application.OpenURL("https://vrchat.survival-machines.fr/register");
                        VRCUiManagerUtils.GetVRCUiManager().ShowScreen("UserInterface/MenuContent/Screens/Authentication/VRCMNLoginCreate");
                    });
                }
                else
                    VRCModLogger.LogError("[VRCTools] Unable to find ButtonAboutUs");


                //Remove "About Us" Button
                VRCModLogger.Log("[VRCTools] Removing ButtonAboutUs");
                vrcmnwLoginPageGO.transform.Find("ButtonAboutUs").gameObject.SetActive(false);

                //Remove VRChat Logo
                VRCModLogger.Log("[VRCTools] Removing VRChat Logo");
                vrcmnwLoginPageGO.transform.Find("VRChat_LOGO (1)").gameObject.SetActive(false);

                RectTransform box = vrcmnwLoginPageGO.transform.Find("BoxLogin").GetComponent<RectTransform>();
                box.localPosition += new Vector3(0, 20, 0);



                Text welc = vrcmnwLoginPageGO.transform.Find("TextWelcome").GetComponent<Text>();

                Text patreon = GameObject.Instantiate(welc, welc.transform.parent);
                patreon.color = new Color(0.98f, 0.41f, 0.33f);
                patreon.text = "patreon.com/Slaynash";
                patreon.GetComponent<RectTransform>().localPosition = new Vector3(300, -460);
                patreon.fontSize /= 2;

                Text discord = GameObject.Instantiate(welc, welc.transform.parent);
                discord.color = new Color(0.44f, 0.54f, 0.85f);
                discord.text = "discord.gg/rCqKSvR";
                discord.GetComponent<RectTransform>().localPosition = new Vector3(-300, -460);
                discord.fontSize /= 2;
            }
            else
            {
                VRCModLogger.LogError("[VRCTools] Unable to find UserInterface/MenuContent/Screens/Authentication/LoginUserPass");
                vrcmnwDoLogin = false;
            }
        }

        private static void TryLoginToVRCModNetwork(string username, string password, Action<string> onError)
        {
            APIUser user = vrcmnwLoginCallbackContainer.Model as APIUser;
            VRCModLogger.Log("Invoking auth (uuid: " + (user.id ?? "null") + ")");

            VRCUiPopupManagerUtils.ShowPopup("Login", "Logging in to VRCModNework");

            VRCModNetworkManager.Auth(username, password, user.id, () =>
            {
                SecurePlayerPrefs.SetString("vrcmnw_un_" + user.id, username, "vl9u1grTnvXA");
                SecurePlayerPrefs.SetString("vrcmnw_pw_" + user.id, password, "vl9u1grTnvXA");

                FinishLogin();
            }, onError);
        }

        internal static void InjectVRCModNetworkLoginPage()
        {
            try
            {
                HarmonyInstance harmonyInstance = HarmonyInstance.Create("slaynash.vrcmnwlogin");
                harmonyInstance.Patch(typeof(APIUser).GetMethod("Login", (BindingFlags)(-1)), null, null, GetPatch("LoginPatch"));
                harmonyInstance.Patch(typeof(APIUser).GetMethod("ThirdPartyLogin", (BindingFlags)(-1)), null, null, GetPatch("ThirdPartyLoginPatch"));
                harmonyInstance.Patch(typeof(APIUser).GetMethod("FetchCurrentUser", (BindingFlags)(-1)), null, null, GetPatch("FetchCurrentUserPatch"));
            }
            catch (Exception arg)
            {
                VRCModLogger.Log("[VRCMNWLoginInjectTest] Error while patching client {0}", arg);
            }
        }

        #region VRCModNetwork Login Page Patch

        private static IEnumerable<CodeInstruction> LoginPatch(ILGenerator ilg, IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] newInst = new CodeInstruction[instructions.Count()];

            int cnt = 0;

            for (int i = 0; i < newInst.Length; i++)
            {
                CodeInstruction instruction = instructions.ElementAt(i);
                if (instruction.opcode == OpCodes.Call && (++cnt) == 2)
                {
                    newInst[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(VRCModNetworkLogin), "SendGetRequestLoginPatch1"));
                }
                else
                    newInst[i] = instruction;
            }

            return newInst.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> ThirdPartyLoginPatch(ILGenerator ilg, IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] newInst = new CodeInstruction[instructions.Count()];

            int cnt = 0;

            for (int i = 0; i < instructions.Count(); i++)
            {
                CodeInstruction instruction = instructions.ElementAt(i);
                if (instruction.opcode == OpCodes.Call && (++cnt) == 4)
                {
                    newInst[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(VRCModNetworkLogin), "SendRequestLoginPatch2"));
                }
                else
                    newInst[i] = instruction;
            }

            return newInst.AsEnumerable();
        }

        private static IEnumerable<CodeInstruction> FetchCurrentUserPatch(ILGenerator ilg, IEnumerable<CodeInstruction> instructions)
        {
            CodeInstruction[] newInst = new CodeInstruction[instructions.Count()];

            int cnt = 0;

            for (int i = 0; i < instructions.Count(); i++)
            {
                CodeInstruction instruction = instructions.ElementAt(i);
                if (instruction.opcode == OpCodes.Call && (++cnt) == 3)
                {
                    newInst[i] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(VRCModNetworkLogin), "SendGetRequestLoginPatch1"));
                }
                else
                {
                    newInst[i] = instruction;
                }
            }

            return newInst.AsEnumerable();
        }

        public static void SendGetRequestLoginPatch1(string target, ApiContainer responseContainer = null, Dictionary<string, object> requestParams = null, bool disableCache = false, float cacheLifetime = 3600f, API.CredentialsBundle credentials = null)
        {
            VRCModLogger.Log("SendGetRequestLoginPatch1 - ResponseContainer Type: " + responseContainer.GetType());
            SendRequestLoginPatch(target, HTTPMethods.Get, responseContainer, requestParams, true, disableCache, cacheLifetime, 2, credentials);
        }

        public static void SendRequestLoginPatch2(string endpoint, HTTPMethods method, ApiContainer responseContainer = null, Dictionary<string, object> requestParams = null, bool authenticationRequired = true, bool disableCache = false, float cacheLifetime = 3600f, int retryCount = 2, API.CredentialsBundle credentials = null)
        {
            VRCModLogger.Log("SendRequestLoginPatch2 - ResponseContainer Type: " + responseContainer.GetType());
            SendRequestLoginPatch(endpoint, method, responseContainer, requestParams, authenticationRequired, disableCache, cacheLifetime, retryCount, credentials);
        }


        private static void SendRequestLoginPatch(string endpoint, HTTPMethods method, ApiContainer responseContainer = null, Dictionary<string, object> requestParams = null, bool authenticationRequired = true, bool disableCache = false, float cacheLifetime = 3600f, int retryCount = 2, API.CredentialsBundle credentials = null)
        {

            ApiModelContainer<APIUser> responseContainerExt = new ApiModelContainer<APIUser>
            {
                OnSuccess = (c) =>
                {
                    if (!vrcmnwDoLogin || APIUser.IsLoggedIn)
                    {
                        responseContainer.OnSuccess(c);
                    }
                    else
                    {
                        vrcmnwLoginCallbackContainer = c;
                        vrcmnwLoginCallback = responseContainer.OnSuccess;

                        try
                        {
                            FieldInfo popupCompleteCallbackField = typeof(VRCUiPopup).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(f => f.FieldType == typeof(Action)).First();
                            popupCompleteCallback = popupCompleteCallbackField.GetValue(VRCUiManagerUtils.GetVRCUiManager().currentPopup) as Action;
                            VRCUiPopupManagerUtils.GetVRCUiPopupManager().HideCurrentPopup();
                            APIUser currentUser = vrcmnwLoginCallbackContainer.Model as APIUser;
                            if (SecurePlayerPrefs.HasKey("vrcmnw_un_" + currentUser.id) && SecurePlayerPrefs.HasKey("vrcmnw_pw_" + currentUser.id))
                            {
                                string username = SecurePlayerPrefs.GetString("vrcmnw_un_" + currentUser.id, "vl9u1grTnvXA");
                                string password = SecurePlayerPrefs.GetString("vrcmnw_pw_" + currentUser.id, "vl9u1grTnvXA");
                                TryLoginToVRCModNetwork(username, password, (error) => ShowVRCMNWLoginMenu(true));
                            }
                            else
                                ShowVRCMNWLoginMenu(true);
                        }
                        catch (Exception e)
                        {
                            VRCModLogger.LogError("SendGetRequestLoginPatch - Unable to show popup: " + e);
                            responseContainer.OnSuccess(c);
                        }
                    }
                },
                OnError = responseContainer.OnError
            };

            API.SendRequest(endpoint, method, responseContainerExt, requestParams, authenticationRequired, disableCache, cacheLifetime, retryCount, credentials);
        }

        #endregion



        internal static void TryConnectToVRCModNetwork()
        {
            ModManager.StartCoroutine(TryConnectToVRCModNetworkCoroutine());
        }

        private static IEnumerator TryConnectToVRCModNetworkCoroutine()
        {
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Connecting to the VRCModNetwork...");
            VRCModNetworkManager.ConnectAsync(() =>
            {
                vrcmnwConnected = true;
            }, error =>
            {
                VRCUiPopupManagerUtils.ShowPopup("VRCTools", "Unable to connect to the VRCModNetwork", "Retry", TryConnectToVRCModNetwork, "Ignore", () =>
                {
                    vrcmnwDoLogin = false;
                });
            });
        }

        private static void ShowVRCMNWLoginMenu(bool pause)
        {
            if (pause)
            {
                VRCUiManagerUtils.GetVRCUiManager().ShowUi(false, true);
                ModManager.StartCoroutine(QuickMenuUtils.PlaceUiAfterPause());
            }
            VRCUiManagerUtils.GetVRCUiManager().ShowScreen("UserInterface/MenuContent/Screens/Authentication/VRCMNWLoginPage");
        }





        private static void FinishLogin()
        {
            vrcmnwLoginCallback(vrcmnwLoginCallbackContainer);
            popupCompleteCallback?.Invoke();
        }
    }
}
