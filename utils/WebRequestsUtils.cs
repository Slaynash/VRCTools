using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRCModLoader;

namespace VRCTools
{
    public class WebRequestsUtils
    {
        public static int GetResponseCode(WWW request)
        {
            int ret = 0;
            if (request.responseHeaders == null)
            {
                VRCModLogger.LogError("no response headers.");
            }
            else
            {
                if (!request.responseHeaders.ContainsKey("STATUS"))
                {
                    VRCModLogger.LogError("response headers has no STATUS.");
                }
                else
                {
                    ret = ParseResponseCode(request.responseHeaders["STATUS"]);
                }
            }

            return ret;
        }

        private static int ParseResponseCode(string statusLine)
        {
            int ret = 0;

            string[] components = statusLine.Split(' ');
            if (components.Length < 3)
            {
                VRCModLogger.LogError("invalid response status: " + statusLine);
            }
            else
            {
                if (!int.TryParse(components[1], out ret))
                {
                    VRCModLogger.LogError("invalid response code: " + components[1]);
                }
            }

            return ret;
        }
    }
}
