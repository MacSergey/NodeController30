using ColossalFramework;
using HarmonyLib;
using JetBrains.Annotations;
using ModsCommon;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NodeController.Patches
{
    [UsedImplicitly]
    public static class NetSegmentPatches
    {
        public static bool CalculateCornerPrefix(NetInfo extraInfo1, NetInfo extraInfo2, ushort ignoreSegmentID, ushort startNodeID, bool heightOffset, bool leftSide, ref Vector3 cornerPos, ref Vector3 cornerDirection, ref bool smooth)
        {
            if (extraInfo1 != null || extraInfo2 != null || !SingletonManager<Manager>.Instance.TryGetSegmentData(startNodeID, ignoreSegmentID, out var data))
                return true;
            else
            {
                smooth = data.NodeId.GetNode().m_flags.IsFlagSet(NetNode.Flags.Middle);
                data.GetCorner(leftSide ? SideType.Left : SideType.Right, out cornerPos, out cornerDirection);

                if (heightOffset && startNodeID != 0)
                    cornerPos.y += startNodeID.GetNode().m_heightOffset / 64f;

                return false;
            }
        }

        public static IEnumerable<CodeInstruction> FindDirectionTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var flatJunctionsField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_flatJunctions));
            var followTerrainField = AccessTools.Field(typeof(NetInfo), nameof(NetInfo.m_followTerrain));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && instruction.operand == followTerrainField)
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                }
                else
                {
                    yield return instruction;
                    if (instruction.opcode == OpCodes.Ldfld && instruction.operand == flatJunctionsField)
                    {
                        yield return original.GetLDArg("segmentID");
                        yield return original.GetLDArg("nodeID");
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetSegmentPatches), nameof(GetFlatJunctions)));
                    }
                }
            }
            yield break;
        }

        public static bool GetFlatJunctions(bool flatJunctions, ushort segmentId, ushort nodeId)
        {
            if (SingletonManager<Manager>.Instance.TryGetSegmentData(nodeId, segmentId, out var data))
                return !data.IsSlope;
            else
                return flatJunctions;
        }
    }
}
