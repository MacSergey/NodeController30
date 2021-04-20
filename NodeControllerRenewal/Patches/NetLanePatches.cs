using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using ColossalFramework;
using NodeController;
using ModsCommon;
using ModsCommon.Utilities;
using System.Linq;

namespace NodeController.Patches
{
    public static class NetLanePatches
    {
        public static IEnumerable<CodeInstruction> NetLaneTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var bezierPosition = AccessTools.Method(typeof(Bezier3), nameof(Bezier3.Position), new Type[] { typeof(float) });
            var propPosition = AccessTools.Field(typeof(NetLaneProps.Prop), nameof(NetLaneProps.Prop.m_position));

            var tInstruction = default(CodeInstruction);
            var prev = default(CodeInstruction);
            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (instruction.opcode == OpCodes.Call && instruction.operand == bezierPosition)
                    tInstruction = prev;
                else if (tInstruction != null && instruction.opcode == OpCodes.Ldflda && instruction.operand == propPosition)
                {
                    yield return tInstruction;
                    yield return original.GetLDArg("laneID");
                    yield return original.GetLDArg("laneInfo");
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetLanePatches), nameof(NetLanePatches.FixPropPosition)));
                }

                prev = instruction;
            }
        }
        public static Vector3 FixPropPosition(ref Vector3 pos0, float t, uint laneId, NetInfo.Lane laneInfo)
        {
            var position = pos0;
            var segmentId = laneId.GetLane().m_segment;
            var backward = (laneInfo.m_finalDirection & NetInfo.Direction.Both) == NetInfo.Direction.Backward || (laneInfo.m_finalDirection & NetInfo.Direction.AvoidBoth) == NetInfo.Direction.AvoidForward;
            bool reverse = backward ^ segmentId.GetSegment().IsInvert();

            Manager.Instance.GetSegmentData(segmentId, out var start, out var end);

            var twistStart = start?.TwistAngle ?? 0;
            var twistEnd = end?.TwistAngle ?? 0;
            var twist = Mathf.Lerp(twistStart, twistEnd, t) * Mathf.Deg2Rad;

            position.y += (reverse ? 1 : -1) * position.x * Mathf.Sin(twist);
            position.x *= Mathf.Cos(twist);
            return position;
        }
    }
}
