using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRCModLoader;

namespace VRCTools
{
    internal static class SteamUtils
    {
        private static MethodInfo m_getAuthSessionTicket = null;
        private static HAuthTicket SteamUser_GetAuthSessionTicket(byte[] buffer, int bufferSize, out uint newSize)
        {
            newSize = 0;
            if (m_getAuthSessionTicket == null)
            {
                Type[] types = typeof(HAuthTicket).Assembly.GetTypes();
                foreach (Type t in types)
                {
                    MethodInfo getAuthSessionTicket = null;
                    MethodInfo getSteamId = null;
                    MethodInfo[] methods = t.GetMethods(BindingFlags.Static | BindingFlags.Public);
                    foreach (MethodInfo m in methods)
                    {
                        if (m.ReturnType == typeof(HAuthTicket) && m.GetParameters().Length == 3)
                            getAuthSessionTicket = m;
                        if (m.ReturnType == typeof(HSteamUser))
                            getSteamId = m;

                        if (getAuthSessionTicket != null && getSteamId != null)
                        {
                            m_getAuthSessionTicket = getAuthSessionTicket;
                            break;
                        }
                    }
                    if (m_getAuthSessionTicket != null)
                        break;
                }
            }

            if (m_getAuthSessionTicket == null)
            {
                VRCModLogger.Log("[VRCModNetwork] Unable to find SteamUser_GetAuthSessionTicket");
                return default;
            }
            return (HAuthTicket)m_getAuthSessionTicket.Invoke(null, new object[] { buffer, bufferSize, newSize });
        }

        internal static string GetSteamTicket()
        {
            byte[] array = new byte[1024];
            uint newSize;
            SteamUser_GetAuthSessionTicket(array, 1024, out newSize);
            return BitConverter.ToString(array).Replace("-", string.Empty);
        }
    }
}
