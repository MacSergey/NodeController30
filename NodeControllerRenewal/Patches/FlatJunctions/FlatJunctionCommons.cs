using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using static KianCommons.Patches.TranspilerUtils;

namespace NodeController.Patches
{
    static class FlatJunctionsCommons
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase targetMethod)
        {
            var flatJunctionsField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_flatJunctions));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == flatJunctionsField)
                {
                    yield return GetLDArg(targetMethod, "ignoreSegmentID", throwOnError: false) ?? GetLDArg(targetMethod, "segmentID");
                    yield return GetLDArg(targetMethod, "startNodeID", throwOnError: false) ?? GetLDArg(targetMethod, "nodeID");
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FlatJunctionsCommons), nameof(GetFlatJunctions)));
                }
            }
            yield break;
        }

        public static bool GetFlatJunctions(bool flatJunctions0, ushort segmentID, ushort nodeID)
        {
            var data = SegmentEndManager.Instance.GetAt(segmentID, nodeID);
            return data?.FlatJunctions ?? flatJunctions0;
        }

    }
}
