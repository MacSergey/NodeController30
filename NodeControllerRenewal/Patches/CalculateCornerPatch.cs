namespace NodeController.Patches
{
    using HarmonyLib;
    using JetBrains.Annotations;
    using KianCommons;
    using NodeController.Util;
    using NodeController;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using static KianCommons.Patches.TranspilerUtils;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCornerPatch
    {
        /// <param name="leftSide">left side going away from the junction</param>
        static float GetMinCornerOffset(float cornerOffset0, ushort nodeID, ushort segmentID, bool leftSide)
        {
            var segmentData = SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: nodeID);
            return segmentData == null ? cornerOffset0 : segmentData.Corner(leftSide).Offset;
        }

        [UsedImplicitly]
        static MethodBase TargetMethod() => typeof(NetSegment).GetMethod(nameof(NetSegment.CalculateCorner), BindingFlags.Public | BindingFlags.Static) ?? throw new Exception("CalculateCornerPatch Could not find target method.");

        static FieldInfo f_minCornerOffset = typeof(NetInfo).GetField(nameof(NetInfo.m_minCornerOffset)) ?? throw new Exception("f_minCornerOffset is null");
        static MethodInfo mGetMinCornerOffset = ReflectionHelpers.GetMethod(typeof(CalculateCornerPatch), nameof(GetMinCornerOffset));

        [HarmonyBefore(CSURUtil.HARMONY_ID)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            // apply the flat junctions traspiler
            instructions = FlatJunctionsCommons.Transpiler(instructions, original);

            CodeInstruction ldarg_startNodeID = GetLDArg(original, "startNodeID"); // push startNodeID into stack,
            CodeInstruction ldarg_segmentID = GetLDArg(original, "ignoreSegmentID");
            CodeInstruction ldarg_leftSide = GetLDArg(original, "leftSide");
            CodeInstruction call_GetMinCornerOffset = new CodeInstruction(OpCodes.Call, mGetMinCornerOffset);

            int n = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                bool is_ldfld_minCornerOffset =
                    instruction.opcode == OpCodes.Ldfld && instruction.operand == f_minCornerOffset;
                if (is_ldfld_minCornerOffset)
                {
                    n++;
                    yield return ldarg_startNodeID;
                    yield return ldarg_segmentID;
                    yield return ldarg_leftSide;
                    yield return call_GetMinCornerOffset;
                }
            }

            Mod.Logger.Debug($"TRANSPILER CalculateCornerPatch: Successfully patched NetSegment.CalculateCorner(). found {n} instances of Ldfld NetInfo.m_minCornerOffset");
            yield break;
        }
    }
}
