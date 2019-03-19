using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace VRCTools.IL
{
    public static class ILParser
    {

        private static readonly OpCode[] OpCodes = new OpCode[256];
        private static readonly OpCode[] MultiOpCodes = new OpCode[31];

        static ILParser()
        {
            FieldInfo[] fields = typeof(OpCodes).GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                OpCode opCode = (OpCode)fields[i].GetValue(null);
                if (opCode.Size == 1)
                {
                    OpCodes[(int)opCode.Value] = opCode;
                }
                else
                {
                    MultiOpCodes[(int)(opCode.Value & 255)] = opCode;
                }
            }
        }
        
        public static ILInstruction[] Parse(this MethodInfo method)
        {
            return Parse(method.GetMethodBody().GetILAsByteArray(), method.DeclaringType.Assembly.ManifestModule);
        }
        
        public static ILInstruction[] Parse(this MethodBase methodBase)
        {
            return Parse(methodBase.GetMethodBody().GetILAsByteArray(), methodBase.Module);
        }
        
        public static ILInstruction[] Parse(this MethodBody methodBody, Module manifest)
        {
            return Parse(methodBody.GetILAsByteArray(), manifest);
        }
        
        private static ILInstruction[] Parse(byte[] ilCode, Module manifest)
        {
            ArrayList arrayList = new ArrayList();
            for (int i = 0; i < ilCode.Length; i++)
            {
                ILInstruction ilinstruction = new ILInstruction((ilCode[i] == 254) ? MultiOpCodes[(int)ilCode[i + 1]] : OpCodes[(int)ilCode[i]], ilCode, i, manifest);
                arrayList.Add(ilinstruction);
                i += ilinstruction.Length - 1;
            }
            return (ILInstruction[])arrayList.ToArray(typeof(ILInstruction));
        }
    }
}
