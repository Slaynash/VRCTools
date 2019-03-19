using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace VRCTools.IL
{
    public struct ILInstruction
    {

        public readonly OpCode OpCode;
        public readonly object Argument;
        public readonly bool HasArgument;
        public readonly bool HasSingleByteArgument;
        public readonly int Length;
        
        public ILInstruction(OpCode opCode, byte[] ilCode, int index, Module manifest)
        {
            this.OpCode = opCode;
            this.HasArgument = (opCode.OperandType != OperandType.InlineNone);
            this.HasSingleByteArgument = OpCodes.TakesSingleByteArgument(opCode);
            this.Length = opCode.Size + (this.HasArgument ? (this.HasSingleByteArgument ? 1 : 4) : 0);
            if (this.HasArgument)
            {
                if (this.HasSingleByteArgument)
                {
                    this.Argument = ilCode[index + opCode.Size];
                }
                else
                {
                    this.Argument = BitConverter.ToInt32(ilCode, index + opCode.Size);
                }
                if (this.OpCode == OpCodes.Ldstr)
                {
                    this.Argument = manifest.ResolveString((int)this.Argument);
                    return;
                }
                if (this.OpCode == OpCodes.Call || this.OpCode == OpCodes.Callvirt)
                {
                    this.Argument = manifest.ResolveMethod((int)this.Argument);
                    return;
                }
                if (this.OpCode == OpCodes.Box)
                {
                    this.Argument = manifest.ResolveType((int)this.Argument);
                    return;
                }
                if (this.OpCode == OpCodes.Ldfld || this.OpCode == OpCodes.Ldflda)
                {
                    this.Argument = manifest.ResolveField((int)this.Argument);
                    return;
                }
            }
            else
            {
                this.Argument = null;
            }
        }

        public T GetArgument<T>()
        {
            return (T)((object)this.Argument);
        }
        
        public override string ToString()
        {
            string arg = string.Empty;
            if (this.HasArgument)
            {
                if (this.Argument is int || this.Argument is byte)
                {
                    arg = string.Format(" 0x{0:X}", this.Argument);
                }
                else if (this.Argument is string)
                {
                    arg = " \"" + this.Argument.ToString() + '"';
                }
                else
                {
                    arg = " " + this.Argument.ToString();
                }
            }
            return string.Format("{0}{1}", this.OpCode.Name, arg);
        }
    }
}
