using ColossalFramework;
using HarmonyLib;
using JetBrains.Annotations;
using KianCommons;
using KianCommons.Patches;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeController.Patches
{
    [UsedImplicitly]
    public static class NetSegmentPatches
    {
        public static void CalculateCornerPostfix(ushort segmentID, bool start, bool leftSide, ref Vector3 cornerPos, ref Vector3 cornerDirection)
        {
            SegmentEndData data = SegmentEndManager.Instance.GetAt(segmentID, start);
            if (data == null && !GUI.Settings.GameConfig.UnviversalSlopeFixes)
                return;

            ushort nodeID = segmentID.ToSegment().GetNode(start);
            bool middle = nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle);
            bool untouchable = nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Untouchable);
            if (!middle)
            {
                bool flatJunctions = data?.FlatJunctions ?? untouchable || segmentID.ToSegment().Info.m_flatJunctions;
                bool slope = !flatJunctions;
                if (slope)
                    FixCornerPos(nodeID.ToNode().m_position, segmentID.ToSegment().GetDirection(nodeID), ref cornerPos);

                else
                {
                    // left segment going away from the node is right segment going toward the node.
                    ushort neighbourSegmentID = leftSide ? segmentID.ToSegment().GetRightSegment(nodeID) : segmentID.ToSegment().GetLeftSegment(nodeID);

                    bool twist;
                    if (data != null)
                        twist = data.CanModifyTwist() && data.Twist;
                    else
                    {
                        twist = !untouchable && segmentID.ToSegment().Info.m_flatJunctions;
                        twist = twist && SegmentEndData.CanTwist(segmentID: segmentID, nodeID: nodeID);
                    }

                    if (twist)
                    {
                        Vector3 nodePos = nodeID.ToNode().m_position;
                        Vector3 neighbourEndDir = neighbourSegmentID.ToSegment().GetDirection(nodeID);

                        FixCornerPosMinor(nodePos, neighbourEndDir, ref cornerDirection, ref cornerPos);
                    }
                }
            }
            if (data != null)
                data.ApplyCornerAdjustments(ref cornerPos, ref cornerDirection, leftSide);
            else
            {
                float absY = Mathf.Abs(cornerDirection.y);
                if (absY > 2)
                    cornerDirection *= 2 / absY;
            }
        }
        public static void FixCornerPos(Vector3 nodePos, Vector3 segmentEndDir, ref Vector3 cornerPos)
        {
            float d = DotXZ(cornerPos - nodePos, segmentEndDir);
            cornerPos.y = nodePos.y + d * segmentEndDir.y;
        }

        public static void FixCornerPosMinor(Vector3 nodePos, Vector3 neighbourEndDir, ref Vector3 cornerDir, ref Vector3 cornerPos)
        {
            float d = DotXZ(cornerPos - nodePos, neighbourEndDir);
            cornerPos.y = nodePos.y + d * neighbourEndDir.y;

            float acos = DotXZ(cornerDir, neighbourEndDir);
            cornerDir.y = neighbourEndDir.y * acos;
        }

        public static IEnumerable<CodeInstruction> FindDirectionTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase targetMethod)
        {
            var flatJunctionsField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_flatJunctions));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == flatJunctionsField)
                {
                    yield return TranspilerUtils.GetLDArg(targetMethod, "ignoreSegmentID", throwOnError: false) ?? TranspilerUtils.GetLDArg(targetMethod, "segmentID");
                    yield return TranspilerUtils.GetLDArg(targetMethod, "startNodeID", throwOnError: false) ?? TranspilerUtils.GetLDArg(targetMethod, "nodeID");
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(GetFlatJunctions)));
                }
            }
            yield break;
        }

        public static bool GetFlatJunctions(bool flatJunctions0, ushort segmentID, ushort nodeID)
        {
            var data = SegmentEndManager.Instance.GetAt(segmentID, nodeID);
            return data?.FlatJunctions ?? flatJunctions0;
        }

        public static void CalculateSegmentPostfix(ushort segmentID)
        {
            if (!NetUtil.IsSegmentValid(segmentID))
                return;

            SegmentEndData segStart = SegmentEndManager.Instance.GetAt(segmentID, true);
            SegmentEndData segEnd = SegmentEndManager.Instance.GetAt(segmentID, false);
            segStart?.OnAfterCalculate();
            segEnd?.OnAfterCalculate();
        }

        public static IEnumerable<CodeInstruction> RenderInstanceTranspiler(ILGenerator il, IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            // apply the flat junctions traspiler
            instructions = FindDirectionTranspiler(instructions, method);

            var ldarg_segmentID = TranspilerUtils.GetLDArg(method, "segmentID"); // push startNodeID into stack,
            var call_CalculateNameMatrix = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(CalculateNameMatrix)));
            var dataMatrix2Field = AccessTools.Field(typeof(RenderManager.Instance), nameof(RenderManager.Instance.m_dataMatrix2));

            // TODO complete transpiler.
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Stfld && instruction.operand == dataMatrix2Field)
                {
                    yield return ldarg_segmentID;
                    yield return call_CalculateNameMatrix;
                }
            }

            yield break;
        }
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

        public static IEnumerable<CodeInstruction> CalculateCornerTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            instructions = FindDirectionTranspiler(instructions, original);

            var ldarg_startNodeID = TranspilerUtils.GetLDArg(original, "startNodeID");
            var ldarg_segmentID = TranspilerUtils.GetLDArg(original, "ignoreSegmentID");
            var ldarg_leftSide = TranspilerUtils.GetLDArg(original, "leftSide");
            var call_GetMinCornerOffset = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(GetMinCornerOffset)));
            var minCornerOffsetField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_minCornerOffset));

            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == minCornerOffsetField)
                {
                    yield return ldarg_startNodeID;
                    yield return ldarg_segmentID;
                    yield return ldarg_leftSide;
                    yield return call_GetMinCornerOffset;
                }
            }
            yield break;
        }
        static float GetMinCornerOffset(float cornerOffset0, ushort nodeID, ushort segmentID, bool leftSide)
        {
            var segmentData = SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: nodeID);
            return segmentData == null ? cornerOffset0 : segmentData.Corner(leftSide).Offset;
        }
    }
}
