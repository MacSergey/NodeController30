using HarmonyLib;
using NodeController;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace KianCommons.Patches
{
    public static class TranspilerUtilities
    {
        public static bool IsSameInstruction(this CodeInstruction a, CodeInstruction b)
        {
            if (a.opcode == b.opcode)
                return a.operand == b.operand || (a.operand is byte aByte && b.operand is byte bByte && aByte == bByte);
            else
                return false;
        }

        public class InstructionNotFoundException : Exception
        {
            public InstructionNotFoundException(string message) : base(message) { }
        }
        [Obsolete]
        public static int SearchInstruction(List<CodeInstruction> codes, CodeInstruction instruction, int index, int dir = 1, int counter = 1)
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

        public static int SearchGeneric(List<CodeInstruction> codes, Func<int, bool> predicate, int index, int dir = 1, int counter = 1, bool throwOnError = true)
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
        public static void InsertInstructions(List<CodeInstruction> codes, CodeInstruction[] insertion, int index)
        {
            var labels = codes[index].labels;
            insertion[0].labels.AddRange(labels);
            labels.Clear();

            codes.InsertRange(index, insertion);
        }
    }
}
