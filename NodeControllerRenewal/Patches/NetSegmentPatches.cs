using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using JetBrains.Annotations;
using KianCommons;
using KianCommons.Patches;
using ModsCommon.Utilities;
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
                if (!flatJunctions)
                    FixCornerPos(nodeID.ToNode().m_position, segmentID.ToSegment().GetDirection(nodeID), ref cornerPos);
                else
                {
                    // left segment going away from the node is right segment going toward the node.
                    ushort neighbourSegmentID = leftSide ? segmentID.ToSegment().GetRightSegment(nodeID) : segmentID.ToSegment().GetLeftSegment(nodeID);

                    bool twist;
                    if (data != null)
                        twist = data.CanModifyTwist && data.Twist;
                    else
                    {
                        twist = !untouchable && segmentID.ToSegment().Info.m_flatJunctions;
                        twist = twist && SegmentEndData.CanTwist(segmentId: segmentID, nodeId: nodeID);
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
            {
                var quaternion = Quaternion.FromToRotation(Vector3.forward, cornerDirection);
                var result = quaternion * Quaternion.Euler(data.SlopeAngle, 0, 0);
                cornerDirection = result * Vector3.forward;

                cornerPos.y += (leftSide ? -1 : 1) * data.Info.m_halfWidth * Mathf.Sin(data.TwistAngle * Mathf.Deg2Rad);
            }
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

        public static IEnumerable<CodeInstruction> CalculateCornerTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            instructions = FindDirectionTranspiler(instructions, original);

            yield return TranspilerUtils.GetLDArg(original, "startNodeID");
            yield return TranspilerUtils.GetLDArg(original, "ignoreSegmentID");
            yield return TranspilerUtils.GetLDArgRef(original, "startPos");
            yield return TranspilerUtils.GetLDArgRef(original, "startDir");
            yield return TranspilerUtils.GetLDArgRef(original, "endPos");
            yield return TranspilerUtils.GetLDArgRef(original, "endDir");
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(FixPosAndDir)));

            var halfWidthField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_halfWidth));
            var halfWidthLocal = generator.DeclareLocal(typeof(float));
            yield return TranspilerUtils.GetLDArg(original, "info");
            yield return new CodeInstruction(OpCodes.Ldfld, halfWidthField);
            yield return TranspilerUtils.GetLDArg(original, "startNodeID");
            yield return TranspilerUtils.GetLDArg(original, "ignoreSegmentID");
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(GetHalfWidth)));
            yield return new CodeInstruction(OpCodes.Stloc_S, halfWidthLocal.LocalIndex);

            var minCornerOffsetField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_minCornerOffset));
            var offsetLocal = generator.DeclareLocal(typeof(float));
            yield return TranspilerUtils.GetLDArg(original, "info");
            yield return new CodeInstruction(OpCodes.Ldfld, minCornerOffsetField);
            yield return new CodeInstruction(OpCodes.Ldloc_S, halfWidthLocal.LocalIndex);
            yield return TranspilerUtils.GetLDArg(original, "startNodeID");
            yield return TranspilerUtils.GetLDArg(original, "ignoreSegmentID");
            yield return TranspilerUtils.GetLDArg(original, "leftSide");
            yield return TranspilerUtils.GetLDArg(original, "startPos");
            yield return TranspilerUtils.GetLDArg(original, "startDir");
            yield return TranspilerUtils.GetLDArg(original, "endPos");
            yield return TranspilerUtils.GetLDArg(original, "endDir");
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(GetMinCornerOffset)));
            yield return new CodeInstruction(OpCodes.Stloc_S, offsetLocal.LocalIndex);


            var wasGetCorrnerOffset = false;

            foreach (var instruction in instructions)
            {
                if (wasGetCorrnerOffset && instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float v && v == 0.01f)
                    instruction.operand = -100f;
                wasGetCorrnerOffset = false;

                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == halfWidthField)
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldloc, halfWidthLocal.LocalIndex);
                }
                else if (instruction.opcode == OpCodes.Ldfld && instruction.operand == minCornerOffsetField)
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldloc, offsetLocal.LocalIndex);
                    wasGetCorrnerOffset = true;
                }
                else
                    yield return instruction;
            }
        }
        public static void FixPosAndDir(ushort firstNodeId, ushort segmentId, ref Vector3 startPos, ref Vector3 startDir, ref Vector3 endPos, ref Vector3 endDir)
        {
            var segment = segmentId.GetSegment();
            var secondNodeId = segment.m_startNode == firstNodeId ? segment.m_endNode : segment.m_startNode;

            var startShift = SegmentEndManager.Instance.GetAt(segmentId, firstNodeId) is SegmentEndData SegmentData1 ? SegmentData1.Shift : 0f;
            var endShift = SegmentEndManager.Instance.GetAt(segmentId, secondNodeId) is SegmentEndData SegmentData2 ? SegmentData2.Shift : 0f;

            if (startShift == 0f && endShift == 0f)
                return;

            var shift = (startShift + endShift) / 2;
            var dir = endPos - startPos;
            var sin = shift / dir.XZ().magnitude;
            var deltaAngle = Mathf.Asin(sin);
            var normal = dir.TurnRad(Mathf.PI / 2 + deltaAngle, true).normalized;

            startPos -= normal * startShift;
            endPos += normal * endShift;
            startDir = startDir.TurnRad(deltaAngle, true);
            endDir = endDir.TurnRad(deltaAngle, true);
        }

        static float GetHalfWidth(float halfWidth, ushort nodeID, ushort segmentID)
        {
            if (SegmentEndManager.Instance.GetAt(segmentID, nodeID) is SegmentEndData segmentData)
                return halfWidth * Mathf.Cos(segmentData.TwistAngle * Mathf.Deg2Rad);
            else
                return halfWidth;
        }
        static float GetMinCornerOffset(float cornerOffset, float halfWidth, ushort nodeId, ushort segmentId, bool leftSide, Vector3 startPos, Vector3 startDir, Vector3 endPos, Vector3 endDir)
        {
            if (SegmentEndManager.Instance.GetAt(segmentId, nodeId) is not SegmentEndData segmentData)
                return cornerOffset;

            var startNormal = startDir.Turn90(false);
            var endNormal = endDir.Turn90(true);

            var bezier = new Bezier3()
            {
                a = startPos,
                d = endPos,
            };
            NetSegment.CalculateMiddlePoints(bezier.a, startDir, bezier.d, endDir, true, true, out bezier.b, out bezier.c);

            var sideBezier = new Bezier3()
            {
                a = bezier.a + (leftSide ? 1 : -1) * startNormal *  halfWidth,
                d = bezier.d + (leftSide ? 1 : -1) * endNormal *  halfWidth,
            };
            NetSegment.CalculateMiddlePoints(sideBezier.a, startDir, sideBezier.d, endDir, true, true, out sideBezier.b, out sideBezier.c);

            var t = Mathf.Clamp01(bezier.Travel(0f, segmentData.Offset));
            var position = bezier.Position(t);
            var direction = bezier.Tangent(t).Turn90(true).TurnDeg(segmentData.RotateAngle, true);

            var line = new StraightTrajectory(position, position + direction, false);
            var side = new BezierTrajectory(sideBezier);

            var intersection = Intersection.CalculateSingle(side, line);
            if (intersection.IsIntersect)
            {
                side = side.Cut(0f, intersection.FirstT) as BezierTrajectory;
                return side.Length;
            }
            else if (segmentData.RotateAngle == 0f)
                return 0f;
            else if (leftSide ^ segmentData.RotateAngle > 0f)
                return 0f;
            else
                return side.Length;
        }
    }
}
