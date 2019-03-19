using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using VRCModLoader;

namespace VRCTools
{
    public static class DebugUtils
    {
        public static void PrintHierarchy(Transform transform, int depth)
        {
            String s = "";
            for (int i = 0; i < depth; i++) s += "\t";
            s += transform.name + " [";

            Component[] mbs = transform.GetComponents<Component>();
            for (int i = 0; i < mbs.Length; i++)
            {
                if (mbs[i] == null) continue;
                if (i == 0)
                    s += mbs[i].GetType();
                else
                    s += ", " + mbs[i].GetType();
            }

            s += "]";
            VRCModLogger.Log(s);
            foreach (Transform t in transform)
            {
                if (t != null) PrintHierarchy(t, depth + 1);
            }
        }

        public static void CreateDebugCube(Transform parent, int size)
        {

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "DebugCube";
            cube.transform.SetParent(parent);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = new Vector3(size, size, size);
        }
    }
}
