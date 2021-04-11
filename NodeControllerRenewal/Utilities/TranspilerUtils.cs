using HarmonyLib;
using NodeController;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace KianCommons.Patches
{
    public static class TranspilerUtils
    {
        public static List<CodeInstruction> ToCodeList(this IEnumerable<CodeInstruction> instructions)
        {
            var originalCodes = new List<CodeInstruction>(instructions);
            var codes = new List<CodeInstruction>(originalCodes); // is this redundant?
            return codes;
        }

        public static CodeInstruction GetLDArg(MethodBase method, string argName, bool throwOnError = true)
        {
            if (!throwOnError && !HasParameter(method, argName))
                return null;

            byte idx = method.GetArgLoc(argName);
            if (idx == 0)
                return new CodeInstruction(OpCodes.Ldarg_0);
            else if (idx == 1)
                return new CodeInstruction(OpCodes.Ldarg_1);
            else if (idx == 2)
                return new CodeInstruction(OpCodes.Ldarg_2);
            else if (idx == 3)
                return new CodeInstruction(OpCodes.Ldarg_3);
            else
                return new CodeInstruction(OpCodes.Ldarg_S, idx);
        }
        public static CodeInstruction GetLDArgRef(MethodBase method, string argName, bool throwOnError = true)
        {
            if (!throwOnError && !HasParameter(method, argName))
                return null;

            byte idx = method.GetArgLoc(argName);
            return new CodeInstruction(OpCodes.Ldarga_S, idx);
        }

        public static byte GetArgLoc(this MethodBase method, string argName)
        {
            byte idx = GetParameterLoc(method, argName);
            if (!method.IsStatic)
                idx++;
            return idx;
        }

        public static byte GetParameterLoc(MethodBase method, string name)
        {
            var parameters = method.GetParameters();
            for (byte i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i].Name == name)
                    return i;
            }
            throw new Exception($"did not found parameter with name:<{name}>");
        }

        public static bool HasParameter(MethodBase method, string name) => method.GetParameters().Any(p => p.Name == name);

        public static bool IsSameInstruction(this CodeInstruction a, CodeInstruction b)
        {
            if (a.opcode == b.opcode)
            {
                if (a.operand == b.operand)
                {
                    return true;
                }

                // This special code is needed for some reason because the == operator doesn't work on System.Byte
                return (a.operand is byte aByte && b.operand is byte bByte && aByte == bByte);
            }
            else
            {
                return false;
            }
        }
        public static CodeInstruction BuildLdLocFromStLoc(this CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Stloc_0)
                return new CodeInstruction(OpCodes.Ldloc_0);
            else if (instruction.opcode == OpCodes.Stloc_1)
                return new CodeInstruction(OpCodes.Ldloc_1);
            else if (instruction.opcode == OpCodes.Stloc_2)
                return new CodeInstruction(OpCodes.Ldloc_2);
            else if (instruction.opcode == OpCodes.Stloc_3)
                return new CodeInstruction(OpCodes.Ldloc_3);
            else if (instruction.opcode == OpCodes.Stloc_S)
                return new CodeInstruction(OpCodes.Ldloc_S, instruction.operand);
            else if (instruction.opcode == OpCodes.Stloc)
                return new CodeInstruction(OpCodes.Ldloc, instruction.operand);
            else
                throw new Exception("instruction is not stloc! : " + instruction);
        }

        public static CodeInstruction BuildStLocFromLdLoc(this CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldloc_0)
                return new CodeInstruction(OpCodes.Stloc_0);
            else if (instruction.opcode == OpCodes.Ldloc_1)
                return new CodeInstruction(OpCodes.Stloc_1);
            else if (instruction.opcode == OpCodes.Ldloc_2)
                return new CodeInstruction(OpCodes.Stloc_2);
            else if (instruction.opcode == OpCodes.Ldloc_3)
                return new CodeInstruction(OpCodes.Stloc_3);
            else if (instruction.opcode == OpCodes.Ldloc_S)
                return new CodeInstruction(OpCodes.Stloc_S, instruction.operand);
            else if (instruction.opcode == OpCodes.Ldloc)
                return new CodeInstruction(OpCodes.Stloc, instruction.operand);
            else
                throw new Exception($"instruction is not ldloc! : {instruction}");
        }

        internal static string IL2STR(this IEnumerable<CodeInstruction> instructions)
        {
            string ret = "";
            foreach (var code in instructions)
                ret += code + "\n";
            return ret;
        }

        public class InstructionNotFoundException : Exception
        {
            public InstructionNotFoundException() : base() { }
            public InstructionNotFoundException(string m) : base(m) { }
        }

        public static int Search(this List<CodeInstruction> codes, Func<CodeInstruction, bool> predicate, int startIndex = 0, int count = 1, bool throwOnError = true)
        {
            return codes.Search(
                (int i) => predicate(codes[i]),
                startIndex: startIndex,
                count: count,
                throwOnError: throwOnError);

        }

        public static int Search(this List<CodeInstruction> codes, Func<int, bool> predicate, int startIndex = 0, int count = 1, bool throwOnError = true)
        {
            if (count == 0)
                throw new ArgumentOutOfRangeException("count can't be zero");

            int dir = count > 0 ? 1 : -1;
            int counter = Math.Abs(count);
            int n = 0;
            int index = startIndex;

            for (; 0 <= index && index < codes.Count; index += dir)
            {
                if (predicate(index))
                {
                    if (++n == counter)
                        break;
                }
            }

            return n == counter ? index : (!throwOnError ? -1 : throw new InstructionNotFoundException($"count: found={n} requested={count}"));
        }

        [Obsolete]
        public static int SearchInstruction(List<CodeInstruction> codes, CodeInstruction instruction, int index, int dir = +1, int counter = 1)
        {
            try
            {
                return SearchGeneric(codes, idx => IsSameInstruction(codes[idx], instruction), index, dir, counter);
            }
            catch (InstructionNotFoundException)
            {
                throw new InstructionNotFoundException(" Did not found instruction: " + instruction);
            }
        }

        public static int SearchGeneric(List<CodeInstruction> codes, Func<int, bool> predicate, int index, int dir = +1, int counter = 1, bool throwOnError = true)
        {
            int count = 0;
            for (; 0 <= index && index < codes.Count; index += dir)
            {
                if (predicate(index))
                {
                    if (++count == counter)
                        break;
                }
            }
            return count == counter ? index : (!throwOnError ? -1 : throw new InstructionNotFoundException(" Did not found instruction[s]."));
        }
        public static void MoveLabels(CodeInstruction source, CodeInstruction target)
        {
            // move labels
            var labels = source.labels;
            target.labels.AddRange(labels);
            labels.Clear();
        }
        public static void InsertInstructions(List<CodeInstruction> codes, CodeInstruction[] insertion, int index, bool moveLabels = true)
        {
            foreach (var code in insertion)
            {
                if (code == null)
                    throw new Exception("Bad Instructions:\n" + insertion.IL2STR());
            }

            MoveLabels(codes[index], insertion[0]);
            codes.InsertRange(index, insertion);

        }
    }
}
