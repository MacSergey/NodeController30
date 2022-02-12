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
    public static class CitizenAIPatches
    {
        public static IEnumerable<CodeInstruction> GetPathTargetPositionTranspilar(IEnumerable<CodeInstruction> instructions)
        {
            foreach(var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value && value == 64)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, 4);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CitizenAIPatches), nameof(CitizenAIPatches.GetGap)));
                }
                else
                    yield return instruction;
            }
        }

        private static float GetGap(PathUnit.Position pathPos)
        {
            ref var segment = ref pathPos.m_segment.GetSegment();
            var nodeId = pathPos.m_offset == 0 ? segment.m_startNode : segment.m_endNode;
            return SingletonManager<Manager>.Instance.GetNodeData(nodeId, out var data) ? data.Gap : Mathf.Max(64f, segment.Info.m_halfWidth * 2f);
        }
    }
}
