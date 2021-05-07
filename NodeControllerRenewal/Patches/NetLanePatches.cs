using ColossalFramework.Math;
using HarmonyLib;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NodeController.Patches
{
    public static class NetLanePatches
    {
        public static IEnumerable<CodeInstruction> NetLanePopulateGroupDataTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original) => NetLaneTranspilerBase(generator, instructions, original, 6);
        public static IEnumerable<CodeInstruction> NetLaneRefreshInstanceTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original) => NetLaneTranspilerBase(generator, instructions, original, 5);
        public static IEnumerable<CodeInstruction> NetLaneRenderInstanceTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original) => NetLaneTranspilerBase(generator, instructions, original, 12);
        public static IEnumerable<CodeInstruction> NetLaneRenderDestroyedInstanceTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original) => NetLaneTranspilerBase(generator, instructions, original, 6, 29);

        public static IEnumerable<CodeInstruction> NetLaneTranspilerBase(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original, params int[] propLocals)
        {
            var bezierPosition = AccessTools.Method(typeof(Bezier3), nameof(Bezier3.Position), new Type[] { typeof(float) });
            var propPosition = AccessTools.Field(typeof(NetLaneProps.Prop), nameof(NetLaneProps.Prop.m_position));

            var prevPrev = default(CodeInstruction);
            var prev = default(CodeInstruction);
            var positionLocal = default(LocalBuilder);
            var index = 0;

            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (prev != null && prev.opcode == OpCodes.Call && prev.operand == bezierPosition)
                {
                    positionLocal = generator.DeclareLocal(typeof(Vector3));
                    yield return new CodeInstruction(OpCodes.Ldloc_S, propLocals[index]);
                    yield return new CodeInstruction(OpCodes.Ldflda, propPosition);
                    yield return prevPrev;
                    yield return original.GetLDArg("laneID");
                    yield return original.GetLDArg("laneInfo");
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetLanePatches), nameof(NetLanePatches.FixPropPosition)));
                    yield return new CodeInstruction(OpCodes.Stloc_S, positionLocal);

                    index += 1;
                }
                else if (positionLocal != null && instruction.opcode == OpCodes.Ldflda && instruction.operand == propPosition)
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, positionLocal);
                }

                prevPrev = prev;
                prev = instruction;
            }
        }
        public static Vector3 FixPropPosition(ref Vector3 pos0, float t, uint laneId, NetInfo.Lane laneInfo)
        {
            var position = pos0;
            var segmentId = laneId.GetLane().m_segment;
            SingletonManager<Manager>.Instance.GetSegmentData(segmentId, out var start, out var end);
            if (start == null && end == null)
                return position;

            var reverse = segmentId.GetSegment().IsInvert();

            var startWidthRatio = start?.WidthRatio ?? 1f;
            var endWidthRatio = end?.WidthRatio ?? 1f;
            var widthRatio = Mathf.Lerp(startWidthRatio, endWidthRatio, t);

            var startHeightRatio = start?.HeightRatio ?? 0f;
            var endHeightRatio = end?.HeightRatio ?? 0f;
            var heightRatio = Mathf.Lerp(startHeightRatio, endHeightRatio, t);

            position.x *= widthRatio;
            position.y += (reverse ? 1 : -1) * position.x * heightRatio;

            return position;
        }
    }
}
