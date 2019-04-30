using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace VRCTools.utils
{
    class SteamAPI
    {
        [DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamAPI_ISteamUser_GetAuthSessionTicket")]
        internal static extern uint SteamAPI_ISteamUser_GetAuthSessionTicket(IntPtr c_instancePtr, IntPtr c_pTicket, int c_cbMaxTicket, ref uint c_pcbTicket);
    }
}
