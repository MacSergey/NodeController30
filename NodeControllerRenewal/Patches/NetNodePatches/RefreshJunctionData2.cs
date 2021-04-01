using KianCommons;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;
using KianCommons.Patches;
using static KianCommons.Patches.TranspilerUtils;
using System.Linq;
using ColossalFramework;
using System.Collections.Generic;
using System.Reflection.Emit;


namespace NodeController.Patches
{
    [UsedImplicitly]
    [HarmonyPatch]
    class RefreshJunctionData2
    {
        delegate void dRefreshJunctionData(ushort nodeID, NetInfo info, uint instanceIndex);
        static MethodInfo mGetSegment = GetMethod(typeof(NetNode), "GetSegment");
        static FieldInfo f_minCornerOffset = typeof(NetInfo).GetField(nameof(NetInfo.m_minCornerOffset)) ?? throw new Exception("f_minCornerOffset is null");
        static MethodInfo mGetMinCornerOffset = GetMethod(typeof(RefreshJunctionData2), nameof(GetMinCornerOffset));

        [UsedImplicitly]
        static MethodBase TargetMethod() =>
            DeclaredMethod<dRefreshJunctionData>(typeof(NetNode), "RefreshJunctionData");

        public static float GetMinCornerOffset(float cornerOffset0, ushort nodeID, ushort segmentID)
        {
            var segmentData = SegmentEndManager.Instance.
                GetAt(segmentID: segmentID, nodeID: nodeID);
            if (segmentData == null)
                return cornerOffset0;
            return (segmentData.Corner(true).Offset + segmentData.Corner(false).Offset) * 0.5f;
        }

        [UsedImplicitly]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var codes = instructions.ToCodeList();

            CodeInstruction ldarg_nodeID = GetLDArg(original, "nodeID"); // push nodeID into stack,
            CodeInstruction ldarg_segmentID = BuildSegmentLDLocFromSTLoc(codes);
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
                    yield return ldarg_nodeID;
                    yield return ldarg_segmentID;
                    yield return call_GetMinCornerOffset;
                }
            }
        }

        public static CodeInstruction BuildSegmentLDLocFromSTLoc(
            List<CodeInstruction> codes, int startIndex = 0, int count = 1)
        {
            Assertion.Assert(mGetSegment != null, "mGetSegment!=null");
            int index = codes.Search(c => c.Calls(mGetSegment), startIndex: startIndex, count: count);
            index = codes.Search(c => c.IsStloc(), startIndex: index);
            return codes[index].BuildLdLocFromStLoc();
        }
    }
}