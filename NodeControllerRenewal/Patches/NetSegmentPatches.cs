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
        public static bool CalculateCornerPrefix(NetInfo extraInfo1, NetInfo extraInfo2, ushort ignoreSegmentID, ushort startNodeID, bool leftSide, ref Vector3 cornerPos, ref Vector3 cornerDirection, ref bool smooth)
        {
            if (extraInfo1 != null || extraInfo2 != null || Manager.Instance[startNodeID, ignoreSegmentID] is not SegmentEndData data)
                return true;

            var isMiddle = data.Node.m_flags.IsFlagSet(NetNode.Flags.Middle);
            smooth = isMiddle;

            if (leftSide)
            {
                cornerPos = data.LeftSide.Position;
                cornerDirection = data.LeftSide.Direction;
            }
            else
            {
                cornerPos = data.RightSide.Position;
                cornerDirection = data.RightSide.Direction;
            }

            if(!isMiddle)
            {

            }

            return false;
        }
        public static void CalculateCornerPostfix(ushort segmentID, bool start, bool leftSide, ref Vector3 cornerPos, ref Vector3 cornerDirection)
        {
            if (Manager.Instance.GetSegmentData(segmentID, start) is not SegmentEndData data)
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

            var quaternion = Quaternion.FromToRotation(Vector3.forward, cornerDirection);
            var result = quaternion * Quaternion.Euler(data.SlopeAngle, 0, 0);
            cornerDirection = result * Vector3.forward;

            cornerPos.y += (leftSide ? -1 : 1) * data.Info.m_halfWidth * Mathf.Sin(data.TwistAngle * Mathf.Deg2Rad);

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
    }
}
