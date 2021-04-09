using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static KianCommons.Patches.TranspilerUtils;
using KianCommons;
using ColossalFramework;
using NodeController;
using ModsCommon;
using ModsCommon.Utilities;

namespace NodeController.Patches
{
    public static class NetLanePatches
    {
        static FieldInfo PositionField { get; } = AccessTools.Field(typeof(NetLaneProps.Prop), nameof(NetLaneProps.Prop.m_position));
        static FieldInfo XField { get; } = AccessTools.Field(typeof(Vector3), nameof(Vector3.x));
        static FieldInfo YField { get; } = AccessTools.Field(typeof(Vector3), nameof(Vector3.y));
        static MethodInfo PositionMethod { get; } = AccessTools.Method(typeof(Bezier3), nameof(Bezier3.Position), new Type[] { typeof(float) });

        public static Vector3 CalculatePropPos(ref Vector3 pos0, float t, uint laneID, NetInfo.Lane laneInfo)
        {
            Vector3 pos = pos0;
            ushort segmentID = laneID.GetLane().m_segment;
            bool backward = (laneInfo.m_finalDirection & NetInfo.Direction.Both) == NetInfo.Direction.Backward || (laneInfo.m_finalDirection & NetInfo.Direction.AvoidBoth) == NetInfo.Direction.AvoidForward;
            bool segmentInvert = segmentID.GetSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            bool reverse = backward != segmentInvert;

            var start = SegmentEndManager.Instance[segmentID, true];
            var end = SegmentEndManager.Instance[segmentID, false];

            float stretchStart = start?.Stretch ?? 0;
            float stretchEnd = end?.Stretch ?? 0;
            var stretch = 1 + Mathf.Lerp(stretchStart, stretchEnd, t) * 0.01f; // convert delta-percent to ratio
            pos.x *= stretch;

            float embankStart = start?.TwistAngle ?? 0;
            float embankEnd = start?.TwistAngle ?? 0;
            float deltaY = pos.x * Mathf.Sin(Mathf.Lerp(embankStart, embankEnd, t));


            if (reverse)
                pos.y += deltaY;
            else
                pos.y -= deltaY;

            return pos;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            try
            {
                var codes = instructions.ToCodeList();
                bool predicate(int i)
                {
                    if (i + 2 >= codes.Count)
                        return false;
                    var c0 = codes[i];
                    var c1 = codes[i + 1];
                    var c2 = codes[i + 2];
                    bool ret = c0.operand == PositionField;
                    ret &= c1.operand == XField || c1.operand == YField;
                    ret &= c2.opcode == OpCodes.Mul || c2.opcode == OpCodes.Add; // ignore if(pos.x != 0)
                    return ret;
                }

                int index = 0;
                int nInsertions = 0;
                for (int watchdog = 0; ; ++watchdog)
                {
                    int c = 1;//watchdog == 0 ? 1 : 2; // skip over m_position from previous loop.
                    index = SearchGeneric(codes, predicate, index, throwOnError: false, counter: c);
                    if (index < 0)
                        break; // not found
                    index++; // insert after
                    bool inserted = InsertCall(codes, index, method);
                    if (inserted)
                        nInsertions++;
                }
                return codes;
            }
            catch (Exception e)
            {
                SingletonMod<Mod>.Logger.Error(e);
                return instructions;
            }
        }

        public static bool InsertCall(List<CodeInstruction> codes, int index, MethodBase method)
        {
            if (GetLDOffset(codes, index) is not CodeInstruction LDOffset)
                return false; // silently return if no offset could be found.

            var insertion = new CodeInstruction[]
            {
                LDOffset,
                GetLDArg(method, "laneID"),
                 GetLDArg(method, "laneInfo"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NetLanePatches), nameof(CalculatePropPos)))
            };

            // insert after ldflda prop.m_posion
            InsertInstructions(codes, insertion, index);
            return true;
        }

        /// <summary>
        /// finds the previous call to Bezier.Position(laneOffset)
        /// and returns a duplicate to the instruction that loads laneOffset.
        /// </summary>
        public static CodeInstruction GetLDOffset(List<CodeInstruction> codes, int index)
        {
            index = SearchGeneric(codes, i => codes[i].operand == PositionMethod, index: index, dir: -1, throwOnError: false);
            index--; // previous instructions should put offset into stack

            if (index < 0) // not found
                return null;

            // assuming offset is stored somewhere as opposed to calcualted on the spot.
            return new CodeInstruction(codes[index].opcode, codes[index].operand);
        }
    }
}
