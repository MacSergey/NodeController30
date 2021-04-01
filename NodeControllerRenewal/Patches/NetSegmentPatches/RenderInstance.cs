namespace NodeController.Patches
{
    using HarmonyLib;
    using KianCommons;
    using JetBrains.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using static KianCommons.Patches.TranspilerUtils;
    using NodeController.Util;
    using UnityEngine;
    using NodeController;

    [HarmonyPatch]
    class RenderInstance
    {
        /// <param name="leftSide">left side going away from the junction</param>
        static Matrix4x4 CalculateNameMatrix(Matrix4x4 mat, ushort segmentID)
        {
            ref NetSegment segment = ref segmentID.ToSegment();
            var segStart = SegmentEndManager.Instance.GetAt(segmentID, true);
            var segEnd = SegmentEndManager.Instance.GetAt(segmentID, false);

            Vector3 startPos, endPos, startDir, endDir;
            if (segStart != null)
            {
                startPos = segStart.LeftCorner.Pos;
                startDir = segStart.LeftCorner.Dir;
            }
            else
            {
                startPos = segment.m_startNode.ToNode().m_position;
                startDir = segment.m_startDirection;
            }

            if (segEnd != null)
            {
                endPos = segEnd.LeftCorner.Pos;
                endDir = segEnd.LeftCorner.Dir;
            }
            else
            {
                endPos = segment.m_endNode.ToNode().m_position;
                endDir = segment.m_endDirection;
            }


            NetSegment.CalculateMiddlePoints(
                startPos, startDir, endPos, endDir, true, true, out var b, out var c);
            return NetSegment.CalculateControlMatrix(
                startPos, b, c, endPos, (startPos + endPos) * 0.5f, 1f);
        }

        static void CalculateNameMatrix2(ushort segmentID, ref Vector3 startPos, ref Vector3 startDir, ref Vector3 endPos, ref Vector3 endDir)
        {
            ref NetSegment segment = ref segmentID.ToSegment();
            var segStart = SegmentEndManager.Instance.GetAt(segmentID, true);
            var segEnd = SegmentEndManager.Instance.GetAt(segmentID, false);

            if (segStart != null)
            {
                startPos = segStart.LeftCorner.Pos;
                startDir = segStart.LeftCorner.Dir;
            }
            if (segEnd != null)
            {
                endPos = segEnd.LeftCorner.Pos;
                endDir = segEnd.LeftCorner.Dir;
            }
        }

        [UsedImplicitly]
        static MethodBase TargetMethod() => typeof(NetSegment).GetMethod(nameof(NetSegment.RenderInstance), BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new Exception("RenderInstance Could not find target method.");

        static FieldInfo f_dataMatrix2 = typeof(RenderManager.Instance).GetField(nameof(RenderManager.Instance.m_dataMatrix2)) ?? throw new Exception("f_dataMatrix2 is null");

        static MethodInfo mCalculateNameMatrix = AccessTools.DeclaredMethod(typeof(CalculateCornerPatch), nameof(CalculateNameMatrix)) ?? throw new Exception("mCalculateNameMatrix is null");

        static MethodInfo targetMethod_ = TargetMethod() as MethodInfo;

        [HarmonyBefore(CSURUtil.HARMONY_ID)]
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions)
        {
            // apply the flat junctions traspiler
            instructions = FlatJunctionsCommons.Transpiler(instructions, targetMethod_);

            CodeInstruction ldarg_segmentID = GetLDArg(targetMethod_, "segmentID"); // push startNodeID into stack,
            CodeInstruction call_CalculateNameMatrix = new CodeInstruction(OpCodes.Call, mCalculateNameMatrix);

            // TODO complete transpiler.
            int n = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Stfld && instruction.operand == f_dataMatrix2)
                {
                    n++;
                    yield return ldarg_segmentID;
                    yield return call_CalculateNameMatrix;
                }
            }

            Mod.Logger.Debug($"TRANSPILER CalculateCornerPatch: Successfully patched NetSegment.CalculateCorner(). found {n} instances of Ldfld NetInfo.m_minCornerOffset");
            yield break;
        }
    }
}
