using BestHTTP.Authentication;
using CCom;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using VRC.Core;
using VRCModLoader;
using VRCModNetwork.commands;
using VRCTools;

namespace VRCModNetwork
{
    public class VRCModNetworkManager : IConnectionListener
    {

        private static readonly string SERVER_ADDRESS = "vrchat.survival-machines.fr";
        private static readonly int SERVER_PORT = Environment.CommandLine.Contains("--vrctools.dev") ? 26345 : 26342;
        private static readonly string VRCMODNW_VERSION = "1.1";

        private static Client client;
        public static ConnectionState State { private set; get; }
        public static bool IsAuthenticated
        {
            get
            {
                return authenticated;
            }
            internal set
            {
                if (value != authenticated)
                {
                    authenticated = value;
                    VRCModLogger.Log("[VRCModNetwork] IsAuthenticated: " + authenticated);
                    if (value) OnAuthenticated?.Invoke();
                    else OnLogout?.Invoke();
                }
            }
        }

        private static bool authenticated;

        public static event Action OnConnected;
        public static event Action<string> OnDisconnected;
        public static event Action OnAuthenticated;
        public static event Action OnLogout;

        private static Dictionary<string, Action<string, string>> rpcListeners = new Dictionary<string, Action<string, string>>();
        private static VRCModNetworkManager instance;
        private static readonly object userDatasLock = new object();

        private static string userUuid = "";
        private static string userInstanceId = "";
        private static string roomSecret = "";
        private static List<ModDesc> modlist = new List<ModDesc>();
        private static string credentials = "";

        private static Thread modsCheckerThread;
        private static List<Action> sheduled = new List<Action>();

        private VRCModNetworkManager()
        {
            client.SetConnectionListener(this);
            CommandManager.RegisterCommand("RPC", typeof(RPCCommand));
            CommandManager.RegisterCommand("AUTH", typeof(AuthCommand));
            CommandManager.RegisterCommand("LOGOUT", typeof(LogoutCommand));
            CommandManager.RegisterCommand("INSTANCECHANGED", typeof(InstanceChangedCommand));
            CommandManager.RegisterCommand("MODLISTCHANGED", typeof(ModListChangedCommand));
        }

        internal static void ConnectAsync()
        {
            if (State != ConnectionState.DISCONNECTED)
                VRCModLogger.Log("[VRCModNetworkManager] Trying to connect to server, but client is not disconnected");
            else if (client != null && client.autoReconnect)
                VRCModLogger.Log("[VRCModNetworkManager] Trying to connect to server, but client already exist and is tagged as auto-reconnecting");
            else
            {
                if (client == null)
                {
                    client = new Client(SERVER_ADDRESS, SERVER_PORT, VRCMODNW_VERSION);
                    if (instance == null) instance = new VRCModNetworkManager();
                    client.SetConnectionListener(instance);
                    if (modsCheckerThread == null)
                    {
                        modsCheckerThread = new Thread(ModCheckThread)
                        {
                            Name = "Mod Check Thread",
                            IsBackground = true
                        };
                        modsCheckerThread.Start();
                    }
                }
                State = ConnectionState.CONNECTING;
                client.StartConnection();
            }
        }

        /// <summary>
        /// Send a RPC to the VRCMod Network dispatcher.
        /// <para>Only works with RPCs of type: CLIENT_TO_SERVER, SERVER_TO_ALL_CLIENTS, CLIENT_TO_CLIENTROOM</para>
        /// </summary>
        public static void SendRPC(string rpcId, string rpcData = "", Action onSuccess = null, Action<string> onError = null)
        {
            if (rpcData.Contains("\n") || rpcData.Contains("\r")) throw new ArgumentException("Invalid rpcData. It have to be a one-line rpc");
            RPCCommand rpccommand = CommandManager.CreateInstance("RPC", client, true) as RPCCommand;
            rpccommand.SendCommand(rpcId, rpcData, onSuccess, onError);
        }

        /// <summary>
        /// Send a RPC to the VRCMod Network dispatcher.
        /// <para>Only works with RPCs of type: CLIENT_TO_SERVER, SERVER_TO_ALL_CLIENTS, CLIENT_TO_CLIENTROOM</para>
        /// </summary>
        public static void SendRPCNoLog(string rpcId, string rpcData = "", Action onSuccess = null, Action<string> onError = null)
        {
            if (rpcData.Contains("\n") || rpcData.Contains("\r")) throw new ArgumentException("Invalid rpcData. It have to be a one-line rpc");
            RPCCommand rpccommand = CommandManager.CreateInstance("RPC", client, false) as RPCCommand;
            rpccommand.SendCommand(rpcId, rpcData, onSuccess, onError);
        }

        /// <summary>
        /// Send a RPC to the VRCMod Network dispatcher.
        /// <para>Only works with RPCs of type: SERVER_TO_CLIENT, CLIENT_TO_CLIENT</para>
        /// </summary>
        public static void SendRPCToTarget(string rpcId, string targetId, string rpcData = "", Action onSuccess = null, Action<string> onError = null)
        {
            if (rpcData.Contains("\n") || rpcData.Contains("\r")) throw new ArgumentException("Invalid rpcData. It have to be a one-line rpc");
            RPCCommand rpccommand = CommandManager.CreateInstance("RPC", client, true) as RPCCommand;
            rpccommand.SendCommand(rpcId, targetId, rpcData, onSuccess, onError);
        }

        /// <summary>
        /// Send a RPC to the VRCMod Network dispatcher.
        /// <para>Only works with RPCs of type: SERVER_TO_CLIENT, CLIENT_TO_CLIENT</para>
        /// </summary>
        public static void SendRPCToTargetNoLog(string rpcId, string targetId, string rpcData = "", Action onSuccess = null, Action<string> onError = null)
        {
            if (rpcData.Contains("\n") || rpcData.Contains("\r")) throw new ArgumentException("Invalid rpcData. It have to be a one-line rpc");
            RPCCommand rpccommand = CommandManager.CreateInstance("RPC", client, false) as RPCCommand;
            rpccommand.SendCommand(rpcId, targetId, rpcData, onSuccess, onError);
        }

        public static void SetRPCListener(string rpcId, Action<string, string> listener) => rpcListeners[rpcId] = listener;
        public static void ClearRPCListener(string rpcId) => rpcListeners.Remove(rpcId);


        

        internal static void HandleRpc(string sender, string rpcId, string data)
        {
            SheduleForMainThread(() =>
            {
                if (rpcListeners.TryGetValue(rpcId, out Action<string, string> listener))
                {
                    try
                    {
                        listener(sender, data);
                    }
                    catch (Exception e)
                    {
                        VRCModLogger.LogError("Error while handling rpc " + rpcId + ": " + e);
                    }
                }
            });

        }




        public void ConnectionStarted() => State = ConnectionState.CONNECTING;
        public void WaitingForConnection() => State = ConnectionState.CONNECTION_ETABLISHED;
        public void Connecting() => State = ConnectionState.CONNECTION_ETABLISHED;
        public void ConnectionFailed(string error) => State = ConnectionState.DISCONNECTED;
        public void Connected()
        {
            client.autoReconnect = true;
            VRCModLogger.Log("[VRCModNetworkManager] Client autoReconnect set to true");
            State = ConnectionState.CONNECTED;
            OnConnected?.Invoke();
        }
        public void Disconnected(string error)
        {
            State = ConnectionState.DISCONNECTED;
            ResetDatas();
            OnDisconnected?.Invoke(error);
        }

        private static void ResetDatas()
        {
            lock (userDatasLock)
            {
                userUuid = "";
                userInstanceId = "";
                modlist.Clear();
                IsAuthenticated = false;
            }
        }

        internal static void Update()
        {
            lock (sheduled)
            {
                foreach(Action a in sheduled)
                {
                    try
                    {
                        a?.Invoke();
                    }
                    catch(Exception e)
                    {
                        VRCModLogger.LogError("[VRCModNetwork] An error occured while running sheduled action: " + e);
                    }
                }
                sheduled.Clear();
            }

            if (State == ConnectionState.CONNECTED)
            {
                lock (userDatasLock)
                {
                    // Check if user changed
                    string uuid = APIUser.CurrentUser?.id ?? "";
                    string displayName = APIUser.CurrentUser?.displayName ?? "";

                    if (!uuid.Equals(userUuid))
                    {
                        VRCModLogger.Log("[VRCModNetwork] new UUID: " + uuid);
                        DiscordManager.UserChanged(displayName);

                        userUuid = uuid; // use it as a lock to avoid spamming
                        if (uuid.Equals(""))
                        {
                            VRCModLogger.Log("[VRCModNetwork] Resetting data");
                            ResetDatas();
                            VRCModLogger.Log("[VRCModNetwork] Logging out");
                            LogoutCommand logoutCommand = CommandManager.CreateInstance("LOGOUT", client) as LogoutCommand;
                            logoutCommand.LogOut();
                            VRCModLogger.Log("[VRCModNetwork] Done");
                        }
                        else
                        {
                            if (SecurePlayerPrefs.HasKey("vrcmnw_token_" + uuid))
                            {
                                TryAuthenticate("vrcmnw " + uuid + " " + SecurePlayerPrefs.GetString("vrcmnw_token_" + uuid, "vl9u1grTnvXA"));
                            }
                            else if (ApiCredentials.GetAuthTokenProvider() == "steam")
                                TryAuthenticate("st_" + SteamUtils.GetSteamTicket());
                            else if (ApiCredentials.GetAuthTokenProvider() == "oculus")
                                OculusUtils.GetAccessToken(
                                    ticket => TryAuthenticate($"oculus {ticket} {OculusUtils.OculusId} {OculusUtils.OculusName}"),
                                    error => VRCModLogger.LogError("[VRCModNetwork] Error while fetching oculus ticket: " + error));
                            else if (!string.IsNullOrEmpty(credentials))
                            {
                                TryAuthenticate("login " + Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
                                credentials = "";
                                VRCTools.ModPrefs.SetBool("vrctools", "hasvrcmnwtoken", true);
                            }
                            else
                            {
                                VRCModLogger.LogError("[VRCModNetwork] Unable to auth: Required auth datas are missing");
                                return;
                            }
                        }
                    }

                    if (IsAuthenticated)
                    {
                        string roomId = "";
                        if(RoomManagerBase.currentRoom?.currentInstanceIdOnly != null)
                        {
                            roomId = RoomManagerBase.currentRoom.id + ":" + RoomManagerBase.currentRoom.currentInstanceIdWithTags;
                        }
                        if (!userInstanceId.Equals(roomId))
                        {
                            VRCModLogger.Log("[VRCModNetwork] Updating instance id. Current room: " + roomId);
                            userInstanceId = roomId;
                            roomSecret = "";
                            if(roomId != "")
                                roomSecret = DiscordManager.RoomChanged(RoomManagerBase.currentRoom.name, RoomManagerBase.currentRoom.id + ":" + RoomManagerBase.currentRoom.currentInstanceIdOnly, RoomManagerBase.currentRoom.currentInstanceIdWithTags, RoomManager.currentRoom.currentInstanceAccess, RoomManager.currentRoom.capacity);
                            else DiscordManager.RoomChanged("", "", "", ApiWorldInstance.AccessType.InviteOnly, 0);
                            ((InstanceChangedCommand)CommandManager.CreateInstance("INSTANCECHANGED", client)).Send(roomId, roomSecret);
                            VRCModLogger.Log("[VRCModNetwork] Done");
                        }
                    }
                }
            }
        }

        private static void TryAuthenticate(string authData)
        {
            VRCModLogger.Log("[VRCModNetwork] Getting current instanceId");
            if (RoomManagerBase.currentRoom != null && RoomManagerBase.currentRoom.id != null && RoomManagerBase.currentRoom.currentInstanceIdWithTags != null)
                userInstanceId = RoomManagerBase.currentRoom.id + ":" + RoomManagerBase.currentRoom.currentInstanceIdWithTags;
            VRCModLogger.Log("[VRCModNetwork] Getting current modList");
            modlist = ModDesc.GetAllMods();
            VRCModLogger.Log("[VRCModNetwork] Getting current environment");
            ApiServerEnvironment env = VRCApplicationSetup._instance.ServerEnvironment;
            string stringEnv = "";
            if (env == ApiServerEnvironment.Dev) stringEnv = "dev";
            if (env == ApiServerEnvironment.Beta) stringEnv = "beta";
            if (env == ApiServerEnvironment.Release) stringEnv = "release";
            VRCModLogger.Log("[VRCModNetwork] Env: " + env);
            VRCModLogger.Log("[VRCModNetwork] Authenticating");
            AuthCommand authCommand = CommandManager.CreateInstance("AUTH", client, false) as AuthCommand;
            authCommand.Auth(userUuid, authData, stringEnv, userInstanceId, roomSecret, modlist);
            VRCModLogger.Log("[VRCModNetwork] Done");
        }

        internal static void SetCredentials(string credentials_)
        {
            credentials = credentials_;
        }

        internal static void SheduleForMainThread(Action a)
        {
            lock (sheduled) sheduled.Add(a);
        }

        private static void ModCheckThread()
        {
            while (true)
            {
                lock (userDatasLock)
                {
                    if (IsAuthenticated)
                    {
                        List<ModDesc> newModlist = ModDesc.GetAllMods();
                        bool identical = true;
                        if (newModlist.Count != modlist.Count)
                        {
                            identical = false;
                        }
                        else
                        {
                            foreach (ModDesc mod in newModlist)
                            {
                                bool found = false;
                                foreach (ModDesc mod2 in modlist)
                                {
                                    if (
                                        mod2.name.Equals(mod.name) &&
                                        mod2.version.Equals(mod.version) &&
                                        mod2.author.Equals(mod.author) &&
                                        ((mod2.downloadLink == null && mod.downloadLink == null) || (mod2.downloadLink != null && mod2.downloadLink.Equals(mod.downloadLink))) &&
                                        mod2.baseClass.Equals(mod.baseClass))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    identical = false;
                                    break;
                                }
                            }
                        }
                        if (!identical)
                        {
                            modlist = newModlist;
                            ((ModListChangedCommand)CommandManager.CreateInstance("MODLISTCHANGED", client)).Send("{\"modlist\":[" + ModDesc.CreateModlistJson(modlist) + "]}");
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public enum ConnectionState
        {
            DISCONNECTED,
            CONNECTION_ETABLISHED,
            CONNECTING,
            CONNECTED
        }
    }
}
