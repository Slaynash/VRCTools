using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRC.Core;
using VRCModLoader;

namespace VRCTools
{
    internal static class DiscordManager
    {
        private static DiscordRpc.RichPresence presence;
        private static bool running = false;

        public static void Init()
        {
            DiscordRpc.EventHandlers eh = new DiscordRpc.EventHandlers();

            presence.state = "Not in a world";
            presence.details = "Not logged in" + " (" + (VRCTrackingManager.IsInVRMode() ? "VR" : "Desktop") + ")";
            presence.largeImageKey = "logo";
            presence.partySize = 0;
            presence.partyMax = 0;
            presence.partyId = "";
            try
            {
                string steamId = null;
                if (VRCApplicationSetup._instance.ServerEnvironment == ApiServerEnvironment.Release) steamId = "438100";
                if (VRCApplicationSetup._instance.ServerEnvironment == ApiServerEnvironment.Beta) steamId = "744530";
                if (VRCApplicationSetup._instance.ServerEnvironment == ApiServerEnvironment.Dev) steamId = "326100";
                
                DiscordRpc.Initialize("404400696171954177", ref eh, true, steamId);
                DiscordRpc.UpdatePresence(ref presence);

                running = true;
                VRCModLogger.Log("[DiscordManager] RichPresence Initialised");
            }
            catch(Exception e)
            {
                VRCModLogger.Log("[DiscordManager] Unable to init discord RichPresence:");
                VRCModLogger.Log("[DiscordManager] " + e);
            }
        }

        public static void RoomChanged(string worldName, string roomId, ApiWorldInstance.AccessType accessType, int maxPlayers)
        {
            if (!running) return;
            if (!roomId.Equals(""))
            {
                if (accessType == ApiWorldInstance.AccessType.InviteOnly || accessType == ApiWorldInstance.AccessType.InvitePlus)
                {
                    presence.state = "In a private world";
                    presence.partyId = "";
                }
                else
                {
                    string accessString = " [Unknown]";
                    if (accessType == ApiWorldInstance.AccessType.FriendsOfGuests) accessString = " [Friends+]";
                    else if (accessType == ApiWorldInstance.AccessType.FriendsOnly) accessString = " [Friends]";
                    else if (accessType == ApiWorldInstance.AccessType.Public) accessString = "";

                    presence.state = "in " + worldName + accessString;
                    presence.partyId = roomId;
                    presence.partyMax = maxPlayers;
                }
            }
            else
            {
                presence.state = "Not in a world";
                presence.partyId = "";
                presence.partyMax = 0;
            }

            DiscordRpc.UpdatePresence(ref presence);
        }

        public static void UserChanged(string displayName)
        {
            if (!running) return;
            if (!displayName.Equals(""))
            {
                presence.details = "as " + displayName + " (" + (VRCTrackingManager.IsInVRMode() ? "VR" : "Desktop") + ")";
                DiscordRpc.UpdatePresence(ref presence);
            }
            else
            {
                presence.details = "Not logged in" + " (" + (VRCTrackingManager.IsInVRMode() ? "VR" : "Desktop") + ")";
                RoomChanged("", "", 0, 0);
            }
        }

        public static void UserCountChanged(int usercount)
        {
            if (!running) return;
            presence.partySize = usercount;

            DiscordRpc.UpdatePresence(ref presence);
        }

        public static void Update()
        {
            
        }

        public static void OnApplicationQuit()
        {
            if (!running) return;
            DiscordRpc.Shutdown();
        }
    }
}
