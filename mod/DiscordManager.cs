using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.VR;
using VRC.Core;
using VRCModLoader;

namespace VRCTools
{
    internal static class DiscordManager
    {
        //private static readonly string UUID_PATTERN = "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";

        private static DiscordRpc.RichPresence presence;
        private static DiscordRpc.EventHandlers eventHandlers;
        private static bool running = false;


        public static void Init()
        {
            eventHandlers = new DiscordRpc.EventHandlers();
            eventHandlers.errorCallback = (code, message) => VRCModLogger.LogError("[VRCTools] [Discord] (E" + code + ") " + message);

            presence.state = "Not in a world";
            presence.details = "Not logged in" + " (" + (VRCTrackingManager.IsInVRMode() ? "VR" : "PC") + ")";
            presence.largeImageKey = "logo";
            presence.partySize = 0;
            presence.partyMax = 0;
            presence.partyId = "";
            presence.largeImageText = "VRChat";
            DeviceChanged();
            try
            {
                string steamId = null;
                switch (VRCApplicationSetup._instance.ServerEnvironment)
                {
                    case ApiServerEnvironment.Release:
                        steamId = "438100"; presence.largeImageText += " Release";
                        break;
                    case ApiServerEnvironment.Beta:
                        steamId = "744530"; presence.largeImageText += " Beta";
                        break;
                    case ApiServerEnvironment.Dev:
                        steamId = "326100"; presence.largeImageText += " Dev";
                        break;
                    default:
                        break;
                }
                
                DiscordRpc.Initialize("404400696171954177", ref eventHandlers, true, steamId);
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

        public static void DeviceChanged() {
            var isInVR = VRCTrackingManager.IsInVRMode();
            var model = UnityEngine.XR.XRDevice.model;
            if (isInVR) {
                if (model.ToLower().Contains("oculus") || model.ToLower().Contains("rift")) {
                    presence.smallImageKey = "headset_rift";
                    presence.smallImageText = "Oculus Rift";
                } else if (model.ToLower().Contains("htc") || model.ToLower().Contains("vive")) {
                    presence.smallImageKey = "headset_vive";
                    presence.smallImageText = "HTC Vive";
                } else {
                    presence.smallImageKey = "headset_generic";
                    presence.smallImageText = "VR Headset";
                }
            } else {
                    presence.smallImageKey = "desktop";
                    presence.smallImageText = "Desktop";
            }
            VRCModLogger.Log("[DiscordManager.DeviceChanged] isInVR: " + isInVR + " Model: " + model);
        }

        public static string RoomChanged(string worldName, string worldAndRoomId, string roomIdWithTags, ApiWorldInstance.AccessType accessType, int maxPlayers)
        {
            if (!running) return null;
            if (!worldAndRoomId.Equals(""))
            {
                if (accessType == ApiWorldInstance.AccessType.InviteOnly || accessType == ApiWorldInstance.AccessType.InvitePlus)
                {
                    presence.state = "In a private world";
                    presence.partyId = "";
                    if(ModPrefs.GetBool("vrctools", "allowdiscordjoinrequests") && (accessType == ApiWorldInstance.AccessType.InvitePlus))
                        presence.joinSecret = GenerateRandomString(127);
                }
                else
                {
                    string accessString = " [Unknown]";
                    if (accessType == ApiWorldInstance.AccessType.FriendsOfGuests) accessString = " [Friends+]";
                    else if (accessType == ApiWorldInstance.AccessType.FriendsOnly) accessString = " [Friends]";
                    else if (accessType == ApiWorldInstance.AccessType.Public) accessString = "";

                    presence.state = "in " + worldName + accessString;
                    presence.partyId = worldAndRoomId;
                    presence.partyMax = maxPlayers;
                    presence.startTimestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                    if(ModPrefs.GetBool("vrctools", "allowdiscordjoinrequests"))
                        presence.joinSecret = GenerateRandomString(127);
                }
            }
            else
            {
                presence.state = "Not in a world";
                presence.partyId = "";
                presence.partyMax = 0;
                presence.startTimestamp = 0;
                presence.joinSecret = "";
            }

            DiscordRpc.UpdatePresence(ref presence);
            return presence.joinSecret;
        }

        public static void UserChanged(string displayName)
        {
            if (!running) return;
            if (!displayName.Equals(""))
            {
                if (ModPrefs.GetBool("vrctools", "hidenameondiscord")) {
                    presence.details = "Logged in" + " (" + (VRCTrackingManager.IsInVRMode() ? "VR" : "PC") + ")";
                } else {
                    presence.details = "as " + displayName + " (" + (VRCTrackingManager.IsInVRMode() ? "VR" : "PC") + ")";
                }
                DiscordRpc.UpdatePresence(ref presence);
            }
            else
            {
                presence.details = "Not logged in" + " (" + (VRCTrackingManager.IsInVRMode() ? "VR" : "PC") + ")";
                RoomChanged("", "", "", 0, 0);
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



        public static string GenerateRandomString(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
        }
    }
}
