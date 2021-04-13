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
        public static bool CalculateCornerPrefix(ushort ignoreSegmentID, ushort startNodeID, bool leftSide, ref Vector3 cornerPos, ref Vector3 cornerDirection, ref bool smooth)
        {
            if(Manager.Instance[startNodeID, ignoreSegmentID] is not SegmentEndData data)
                return true;

            smooth = (data.Node.m_flags & NetNode.Flags.Middle) != 0;

            //var middle = data.SegmentBezier;
            //var side = leftSide ? data.LeftSideBezier : data.RightSideBezier;

            //var t = middle.Travel(0f, data.Offset);
            //var position = middle.Position(t);
            //var direction = middle.Tangent(t).Turn90(true).TurnDeg(data.RotateAngle, true);

            //var line = new StraightTrajectory(position, position + direction, false);
            //var intersection = Intersection.CalculateSingle(side, line);

            //float sideT;
            //if (intersection.IsIntersect)
            //    sideT = intersection.FirstT;
            //else if (data.RotateAngle == 0f)
            //    sideT = t <= 0.5f ? 0f : 1f;
            //else
            //    sideT = leftSide ^ data.RotateAngle > 0f ? 0f : 1f;

            //cornerPos = side.Position(sideT);
            //cornerDirection = side.Tangent(sideT);
            if(leftSide)
            {
                cornerPos = data.LeftSidePosition;
                cornerDirection = data.LeftSideDirection;
            }
            else
            {
                cornerPos = data.RightSidePosition;
                cornerDirection = data.RightSideDirection;
            }

            return false;
        }
        public static void CalculateCornerPostfix(ushort segmentID, bool start, bool leftSide, ref Vector3 cornerPos, ref Vector3 cornerDirection)
        {
            var data = Manager.GetSegmentData(segmentID, start);
            if (data == null && !GUI.Settings.GameConfig.UnviversalSlopeFixes)
                return;

            var segment = segmentID.GetSegment();
            var nodeId = segment.GetNode(start);
            var node = nodeId.GetNode();

            var untouchable = node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
            if (!node.m_flags.IsFlagSet(NetNode.Flags.Middle))
            {
                var flatJunctions = !data?.IsSlope ?? untouchable || segment.Info.m_flatJunctions;
                if (!flatJunctions)
                    FixCornerPos(node.m_position, segment.GetDirection(nodeId), ref cornerPos);
                else
                {
                    bool twist;
                    if (data != null)
                        twist = data.CanModifyTwist && data.IsTwist;
                    else
                        twist = !untouchable && segment.Info.m_flatJunctions && SegmentEndData.CanTwist(segmentID, nodeId);

                    if (twist)
                    {
                        var neighbourSegmentId = leftSide ? segment.GetRightSegment(nodeId) : segment.GetLeftSegment(nodeId);
                        var neighbourEndDir = neighbourSegmentId.GetSegment().GetDirection(nodeId);
                        FixCornerPosMinor(node.m_position, neighbourEndDir, ref cornerDirection, ref cornerPos);
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
                var absY = Mathf.Abs(cornerDirection.y);
                if (absY > 2)
                    cornerDirection *= 2 / absY;
            }
        }
        public static void FixCornerPos(Vector3 nodePos, Vector3 segmentEndDir, ref Vector3 cornerPos)
        {
            var d = DotXZ(cornerPos - nodePos, segmentEndDir);
            cornerPos.y = nodePos.y + d * segmentEndDir.y;
        }

        public static void FixCornerPosMinor(Vector3 nodePos, Vector3 neighbourEndDir, ref Vector3 cornerDir, ref Vector3 cornerPos)
        {
            var d = DotXZ(cornerPos - nodePos, neighbourEndDir);
            cornerPos.y = nodePos.y + d * neighbourEndDir.y;

            var acos = DotXZ(cornerDir, neighbourEndDir);
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
                    yield return TranspilerUtilities.GetLDArg(targetMethod, "ignoreSegmentID", throwOnError: false) ?? TranspilerUtilities.GetLDArg(targetMethod, "segmentID");
                    yield return TranspilerUtilities.GetLDArg(targetMethod, "startNodeID", throwOnError: false) ?? TranspilerUtilities.GetLDArg(targetMethod, "nodeID");
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(GetFlatJunctions)));
                }
            }
            yield break;
        }

        public static bool GetFlatJunctions(bool flatJunctions, ushort segmentId, ushort nodeId)
        {
            var data = Manager.Instance[nodeId, segmentId];
            return !data?.IsSlope ?? flatJunctions;
        }

        public static IEnumerable<CodeInstruction> CalculateCornerTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            instructions = FindDirectionTranspiler(instructions, original);

            yield return TranspilerUtilities.GetLDArg(original, "startNodeID");
            yield return TranspilerUtilities.GetLDArg(original, "ignoreSegmentID");
            yield return TranspilerUtilities.GetLDArgRef(original, "startPos");
            yield return TranspilerUtilities.GetLDArgRef(original, "startDir");
            yield return TranspilerUtilities.GetLDArgRef(original, "endPos");
            yield return TranspilerUtilities.GetLDArgRef(original, "endDir");
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(FixSegmentData)));

            var halfWidthField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_halfWidth));
            var halfWidthLocal = generator.DeclareLocal(typeof(float));
            yield return TranspilerUtilities.GetLDArg(original, "info");
            yield return new CodeInstruction(OpCodes.Ldfld, halfWidthField);
            yield return TranspilerUtilities.GetLDArg(original, "startNodeID");
            yield return TranspilerUtilities.GetLDArg(original, "ignoreSegmentID");
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(GetHalfWidth)));
            yield return new CodeInstruction(OpCodes.Stloc_S, halfWidthLocal.LocalIndex);

            var minCornerOffsetField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_minCornerOffset));
            var offsetLocal = generator.DeclareLocal(typeof(float));
            yield return TranspilerUtilities.GetLDArg(original, "info");
            yield return new CodeInstruction(OpCodes.Ldfld, minCornerOffsetField);
            yield return new CodeInstruction(OpCodes.Ldloc_S, halfWidthLocal.LocalIndex);
            yield return TranspilerUtilities.GetLDArg(original, "startNodeID");
            yield return TranspilerUtilities.GetLDArg(original, "ignoreSegmentID");
            yield return TranspilerUtilities.GetLDArg(original, "leftSide");
            yield return TranspilerUtilities.GetLDArg(original, "startPos");
            yield return TranspilerUtilities.GetLDArg(original, "startDir");
            yield return TranspilerUtilities.GetLDArg(original, "endPos");
            yield return TranspilerUtilities.GetLDArg(original, "endDir");
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
        private static void FixSegmentData(ushort nodeId, ushort segmentId, ref Vector3 startPos, ref Vector3 startDir, ref Vector3 endPos, ref Vector3 endDir)
        {
            if (Manager.Instance[nodeId, segmentId] is SegmentEndData segmentEnd)
            {
                startPos = segmentEnd.SegmentBezier.StartPosition;
                startDir = segmentEnd.SegmentBezier.StartDirection;
                endPos = segmentEnd.SegmentBezier.EndPosition;
                endDir = segmentEnd.SegmentBezier.EndDirection;
            }
        }

        static float GetHalfWidth(float halfWidth, ushort nodeId, ushort segmentId)
        {
            if (Manager.Instance[nodeId, segmentId] is SegmentEndData segmentData)
                return halfWidth * Mathf.Cos(segmentData.TwistAngle * Mathf.Deg2Rad);
            else
                return halfWidth;
        }
        static float GetMinCornerOffset(float cornerOffset, float halfWidth, ushort nodeId, ushort segmentId, bool leftSide, Vector3 startPos, Vector3 startDir, Vector3 endPos, Vector3 endDir)
        {
            if (Manager.Instance[nodeId, segmentId] is not SegmentEndData segmentData)
                return cornerOffset;

            var middle = segmentData.SegmentBezier;
            var side = leftSide ? segmentData.LeftSideBezier : segmentData.RightSideBezier;

            var t = middle.Travel(0f, segmentData.Offset);
            var position = middle.Position(t);
            var direction = middle.Tangent(t).Turn90(true).TurnDeg(segmentData.RotateAngle, true);

            var line = new StraightTrajectory(position, position + direction, false);

            var intersection = Intersection.CalculateSingle(side, line);
            if (intersection.IsIntersect)
            {
                side = side.Cut(0f, intersection.FirstT);
                return side.Length;
            }

            if (segmentData.RotateAngle == 0f)
                return t <= 0.5f ? 0f : side.Length;
            else
                return leftSide ^ segmentData.RotateAngle > 0f ? 0f : side.Length;
        }

        public static void CalculateSegmentPrefix(ushort segmentID)
        {
            SegmentEndData.UpdateSegmentBezier(segmentID);
        }

        public static IEnumerable<CodeInstruction> UpdateBoundsTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ret)
                {
                    yield return TranspilerUtilities.GetLDArg(original, "segmentID");
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 5);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 6);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 7);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 10);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 11);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(NetSegmentPatches.UpdateBoundsPostfix)));
                }

                yield return instruction;
            }
        }
        private static void UpdateBoundsPostfix(ushort segmentId, Vector3 startLeftPos, Vector3 startRightPos, Vector3 endRightPos, Vector3 endLeftPos, Vector3 startLeftDir, Vector3 startRightDir, Vector3 endRightDir, Vector3 endLeftDir)
        {
            if (!segmentId.GetSegment().IsValid())
                return;

            Manager.GetSegmentData(segmentId, out var start, out var end);
            start?.AfterSegmentCalculate(new SegmentCorner(startLeftPos, startLeftDir), new SegmentCorner(startRightPos, startRightDir));
            end?.AfterSegmentCalculate(new SegmentCorner(endLeftPos, endLeftDir), new SegmentCorner(endRightPos, endRightDir));
        }
    }
}
