using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using VRCModLoader;

namespace VRCTools.utils
{
    public static class Texture2DUtils
    {
        private static MethodInfo loadimageMethod = null;

        public static bool LoadImage(Texture2D texture, byte[] data)
        {
            if (Application.unityVersion.Equals("2017.4.15f1") || Application.unityVersion.Equals("2017.4.28f1"))
            {
                if (loadimageMethod == null)
                {
                    VRCModLogger.Log("Looking for UnityEngine.ImageConversion");
                    foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        Type t = a.GetType("UnityEngine.ImageConversion");
                        if (t != null)
                        {
                            VRCModLogger.Log("Found UnityEngine.ImageConversion in " + a.GetName());
                            VRCModLogger.Log("Looking for UnityEngine.ImageConversion::LoadImage");
                            MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);
                            foreach (MethodInfo m in methods)
                            {
                                if (m.Name.Equals("LoadImage") && m.GetParameters().Count() == 2)
                                {
                                    VRCModLogger.Log("Found UnityEngine.ImageConversion::LoadImage");
                                    loadimageMethod = m;
                                    break;
                                }
                            }

                            break;
                        }
                    }
                }

                if (loadimageMethod != null)
                    return (bool)loadimageMethod.Invoke(null, new object[] { texture, data });
                else
                    VRCModLogger.Log("UnityEngine.ImageConversion::LoadImage not found !");
            }
            else
            {
                if (loadimageMethod == null)
                {
                    VRCModLogger.Log("Looking for UnityEngine.Texture2D::LoadImage");
                    MethodInfo[] methods = typeof(Texture2D).GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    foreach (MethodInfo m in methods)
                    {
                        if (m.Name.Equals("LoadImage") && m.GetParameters().Count() == 1)
                        {
                            VRCModLogger.Log("Found UnityEngine.Texture2D::LoadImage");
                            loadimageMethod = m;
                            break;
                        }
                    }
                }

                if (loadimageMethod != null)
                    return (bool)loadimageMethod.Invoke(texture, new object[] { data });
                else
                    VRCModLogger.Log("UnityEngine.Texture2D::LoadImage not found !");
            }
            return false;
        }
    }
}
