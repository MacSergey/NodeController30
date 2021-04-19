using ColossalFramework;
using ColossalFramework.Math;
using HarmonyLib;
using JetBrains.Annotations;
using KianCommons.Patches;
using ModsCommon.Utilities;
using NodeController.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NodeController.Patches
{
    [UsedImplicitly]
    public static class NetSegmentPatches
    {
        public static bool CalculateCornerPrefix(NetInfo extraInfo1, NetInfo extraInfo2, ushort ignoreSegmentID, ushort startNodeID, bool leftSide, ref Vector3 cornerPos, ref Vector3 cornerDirection, ref bool smooth)
        {
            if (extraInfo1 != null || extraInfo2 != null || Manager.Instance[startNodeID, ignoreSegmentID] is not SegmentEndData data)
                return true;

            smooth = data.Node.m_flags.IsFlagSet(NetNode.Flags.Middle);
            data.GetCorner(leftSide, out cornerPos, out cornerDirection);

            //var segment = ignoreSegmentID.GetSegment();
            //var isStart = segment.IsStartNode(startNodeID);
            //var startPos = (isStart ? segment.m_startNode : segment.m_endNode).GetNode().m_position;
            //var startDir = isStart ? segment.m_startDirection : segment.m_endDirection;
            //var endPos = (isStart ? segment.m_endNode : segment.m_startNode).GetNode().m_position;
            //var endDir = isStart ? segment.m_endDirection : segment.m_startDirection;
            //CornerSource.CalculateCorner(segment.Info, startPos, endPos, startDir, endDir, ignoreSegmentID, startNodeID, true, leftSide, out var cp, out var cd, out _);
            //cornerPos = cp;
            //cornerDirection = cd;

            return false;
        }

        public static IEnumerable<CodeInstruction> FindDirectionTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase targetMethod)
        {
            var flatJunctionsField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_flatJunctions));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == flatJunctionsField)
                {
                    yield return TranspilerUtilities.GetLDArg(targetMethod, "segmentID");
                    yield return TranspilerUtilities.GetLDArg(targetMethod, "nodeID");
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
