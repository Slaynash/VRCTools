using BestHTTP.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using VRC.Core;
using VRCModLoader;
using VRCTools.networking.commands;

namespace VRCTools.networking
{
    public class VRCModNetworkManager : IConnectionListener
    {
        private static readonly bool DEV = false;

        private static readonly string SERVER_ADDRESS = "vrchat.survival-machines.fr";
        private static readonly int SERVER_PORT = !DEV ? 26342 : 26345;
        private static readonly string VRCMODNW_VERSION = "1.0";

        private static Client client = null;
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
                    VRCTools.UpdateNetworkStatus();
                    if (value) OnAuthenticated?.Invoke();
                    else OnLogout?.Invoke();
                }
            }
        }

        private static bool authenticated = false;

        public static event Action OnConnected;
        public static event Action<string> OnDisconnected;
        public static event Action OnAuthenticated;
        public static event Action OnLogout;

        private static Dictionary<string, Action<string, string>> rpcListeners = new Dictionary<string, Action<string, string>>();
        private static VRCModNetworkManager instance = null;
        private static object userDatasLock = new object();

        private static string userUuid = "";
        private static string userInstanceId = "";
        private static List<ModDesc> modlist = new List<ModDesc>();

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
            if (!ModPrefs.GetBool("vrctools", "remoteauthcheck"))
                VRCModLogger.Log("[VRCMOD NWManager] Trying to connect to server, but client doesn't allow auth");
            else if (State != ConnectionState.DISCONNECTED)
                VRCModLogger.Log("[VRCMOD NWManager] Trying to connect to server, but client is not disconnected");
            else if (client != null && client.autoReconnect)
                VRCModLogger.Log("[VRCMOD NWManager] Trying to connect to server, but client already exist and is tagged as auto-reconnecting");
            else
            {
                if (client == null)
                {
                    client = new Client(SERVER_ADDRESS, SERVER_PORT, VRCMODNW_VERSION);
                    if (instance == null) instance = new VRCModNetworkManager();
                    client.SetConnectionListener(instance);
                    Thread modsCheckerThread = new Thread(() => ModCheckThread());
                    modsCheckerThread.Name = "Mod Check Thread";
                    modsCheckerThread.IsBackground = true;
                    modsCheckerThread.Start();
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
            else
            {
                RPCCommand rpccommand = CommandManager.CreateInstance("RPC", client) as RPCCommand;
                rpccommand.SendCommand(rpcId, rpcData, onSuccess, onError);
            }
        }

        /// <summary>
        /// Send a RPC to the VRCMod Network dispatcher.
        /// <para>Only works with RPCs of type: SERVER_TO_CLIENT, CLIENT_TO_CLIENT</para>
        /// </summary>
        public static void SendRPCToTarget(string rpcId, string targetId, string rpcData = "", Action onSuccess = null, Action<string> onError = null)
        {
            if (rpcData.Contains("\n") || rpcData.Contains("\r")) throw new ArgumentException("Invalid rpcData. It have to be a one-line rpc");
            else
            {
                RPCCommand rpccommand = CommandManager.CreateInstance("RPC", client) as RPCCommand;
                rpccommand.SendCommand(rpcId, targetId, rpcData, onSuccess, onError);
            }
        }

        public static void SetRPCListener(string rpcId, Action<string, string> listener) => rpcListeners[rpcId] = listener;
        public static void ClearRPCListener(string rpcId) => rpcListeners.Remove(rpcId);


        

        internal static void HandleRpc(string sender, string rpcId, string data)
        {
            if(rpcListeners.TryGetValue(rpcId, out Action<string, string> listener))
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

        }




        public void ConnectionStarted()
        {
            State = ConnectionState.CONNECTING;
            VRCTools.UpdateNetworkStatus();
        }

        public void WaitingForConnection() => State = ConnectionState.CONNECTION_ETABLISHED;
        public void Connecting() => State = ConnectionState.CONNECTION_ETABLISHED;

        public void ConnectionFailed(string error) {
            State = ConnectionState.DISCONNECTED;
            VRCTools.UpdateNetworkStatus();
        }
        public void Connected()
        {
            client.autoReconnect = true;
            VRCModLogger.Log("Client autoReconnect set to true");
            State = ConnectionState.CONNECTED;
            VRCTools.UpdateNetworkStatus();
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
            if (State == ConnectionState.CONNECTED)
            {
                lock (userDatasLock)
                {
                    // Check if user changed
                    string uuid = APIUser.CurrentUser == null ? "" : APIUser.CurrentUser.id ?? "";
                    string displayName = APIUser.CurrentUser == null ? "" : APIUser.CurrentUser.displayName ?? "";
                    string authToken = ApiCredentials.GetAuthToken() ?? "";

                    Credentials c = ApiCredentials.GetWebCredentials() as Credentials;

                    if (!uuid.Equals(userUuid))
                    {
                        VRCModLogger.Log("new UUID: " + uuid);
                        DiscordManager.UserChanged(displayName);

                        if (!uuid.Equals("") && "".Equals(authToken))
                        {
                            string password = typeof(ApiCredentials).GetField("password", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as string;
                            authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(ApiCredentials.GetUsername() + ":" + password));
                        }

                        if (uuid.Equals(""))
                        {
                            userUuid = uuid;
                            VRCModLogger.Log("Resetting data");
                            ResetDatas();
                            VRCModLogger.Log("Logging out");
                            LogoutCommand logoutCommand = CommandManager.CreateInstance("LOGOUT", client) as LogoutCommand;
                            logoutCommand.LogOut();
                            VRCModLogger.Log("Done");
                        }
                        else
                        {
                            userUuid = uuid;
                            VRCModLogger.Log("Getting current instanceId");
                            if (RoomManager.currentRoom != null && RoomManager.currentRoom.id != null && RoomManager.currentRoom.currentInstanceIdOnly != null)
                                userInstanceId = RoomManager.currentRoom.id + ":" + RoomManager.currentRoom.currentInstanceIdOnly;
                            VRCModLogger.Log("Getting current modList");
                            modlist = ModDesc.GetAllMods();
                            VRCModLogger.Log("Getting current environment");
                            ApiServerEnvironment env = VRCApplicationSetup._instance.ServerEnvironment;
                            string stringEnv = "";
                            if (env == ApiServerEnvironment.Dev) stringEnv = "dev";
                            if (env == ApiServerEnvironment.Beta) stringEnv = "beta";
                            if (env == ApiServerEnvironment.Release) stringEnv = "release";
                            VRCModLogger.Log("Env: " + env);
                            VRCModLogger.Log("Authenticating");
                            AuthCommand authCommand = CommandManager.CreateInstance("AUTH", client) as AuthCommand;
                            authCommand.Auth(authToken, stringEnv, userInstanceId, modlist);
                            VRCModLogger.Log("Done");
                        }
                    }

                    if (IsAuthenticated)
                    {
                        string roomId = "";
                        if(RoomManager.currentRoom != null && RoomManager.currentRoom.id != null && RoomManager.currentRoom.currentInstanceIdOnly != null)
                        {
                            roomId = RoomManager.currentRoom.id + ":" + RoomManager.currentRoom.currentInstanceIdOnly;
                        }
                        if (!userInstanceId.Equals(roomId))
                        {
                            VRCModLogger.Log("Updating instance id");
                            userInstanceId = roomId;
                            DiscordManager.RoomChanged(RoomManager.currentRoom.name, roomId, RoomManager.currentRoom.currentInstanceAccess, RoomManager.currentRoom.capacity);
                            ((InstanceChangedCommand)CommandManager.CreateInstance("INSTANCECHANGED", client)).Send(userInstanceId);
                            VRCModLogger.Log("Done");
                        }
                    }
                }
            }
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
    }

    public enum ConnectionState
    {
        DISCONNECTED,
        CONNECTION_ETABLISHED,
        CONNECTING,
        CONNECTED
    }
}
