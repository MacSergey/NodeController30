using HarmonyLib;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace NodeController.Patches
{
    public static class NetManagerPatches
    {
        public static IEnumerable<CodeInstruction> UpdateSegmentTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            var index = list.FindIndex(i => i.opcode == OpCodes.Ldc_I4_0);
            if (index != -1 && list[index - 1].opcode == OpCodes.Ldarg_3 && list[index + 1].opcode == OpCodes.Bgt)
                list[index].opcode = OpCodes.Ldc_I4_2;

            return list;
        }
    }
}
