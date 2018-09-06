using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRC.Core;
using VRCModLoader;

namespace VRCTools
{
    public static class CurrentUserUtils
    {
        private static Type userType;
        private static bool userTypeChecked = false;


        public static Type GetUser_Type()
        {
            if (!userTypeChecked)
            {
                userTypeChecked = true;
                VRCModLogger.Log("[GetGetUserType()] Looking for types");
                IEnumerable<Type> types = Assembly.GetAssembly(typeof(QuickMenu)).GetTypes().Where((t) => t.IsSubclassOf(typeof(APIUser)));
                VRCModLogger.Log("[GetGetUserType()] Found " + types.ToList().Count + " type :");
                foreach (Type type in types)
                {
                    if (userType == null) userType = type;
                    VRCModLogger.Log("[GetGetUserType()] " + type);
                }
            }
            return userType;
        }

        public static FieldInfo GetGetCurrentUser()
        {
            if (GetUser_Type() == null) return null;
            FieldInfo[] fields = GetUser_Type().GetFields(BindingFlags.Static | BindingFlags.NonPublic);
            if (fields.Length == 0) return null;
            return fields.First((f) => f.FieldType == GetUser_Type());
        }

        public static object GetCurrentUser()
        {
            FieldInfo currentuserfield = GetGetCurrentUser();
            if (currentuserfield == null) return null;
            return currentuserfield.GetValue(null);
        }

        public static ApiAvatar GetApiAvatar()
        {
            if (userType == null) return null;
            if (GetGetCurrentUser().GetValue(null) == null) return null;
            FieldInfo[] fields = GetUser_Type().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            if (fields.Length == 0) return null;

            return fields.First((f) => f.FieldType == typeof(ApiAvatar)) // get CurrentUser
                .GetValue(GetGetCurrentUser().GetValue(null)) as ApiAvatar; // get ApiAvatar
        }
    }
}
