using ColossalFramework.Math;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static KianCommons.Patches.TranspilerUtils;
using static KianCommons.Assertion;
using KianCommons;
using ColossalFramework;
using NodeController30;

namespace NodeController.Patches.NetLanePatches
{
    public static class PropDisplacementCommons
    {
        public static Vector3 CalculatePropPos(ref Vector3 pos0, float t, uint laneID, NetInfo.Lane laneInfo)
        {
            Vector3 pos = pos0;
            ushort segmentID = laneID.ToLane().m_segment;
            bool backward = (laneInfo.m_finalDirection & NetInfo.Direction.Both) == NetInfo.Direction.Backward ||
                            (laneInfo.m_finalDirection & NetInfo.Direction.AvoidBoth) == NetInfo.Direction.AvoidForward;
            bool segmentInvert = segmentID.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            bool reverse = backward != segmentInvert;

            var start = SegmentEndManager.Instance.GetAt(segmentID, true);
            var end = SegmentEndManager.Instance.GetAt(segmentID, false);

            float stretchStart = start?.Stretch ?? 0;
            float stretchEnd = end?.Stretch ?? 0;
            float stretch = Mathf.Lerp(stretchStart, stretchEnd, t);
            stretch = 1 + stretch * 0.01f; // convert delta-percent to ratio
            pos.x *= stretch;

            float embankStart = start?.EmbankmentPercent ?? 0;
            float embankEnd = start?.EmbankmentPercent ?? 0;
            float embankment = Mathf.Lerp(embankStart, embankEnd, t);
            embankment *= 0.01f; //convert percent to ratio.
            float deltaY = pos.x * embankment;


            if (reverse)
                pos.y += deltaY;
            else
                pos.y -= deltaY;
            return pos;
        }

        static MethodInfo mCalculatePropPos = typeof(PropDisplacementCommons).GetMethod(nameof(CalculatePropPos)) ??
            throw new Exception("mCalculatePropPos is null");

        static FieldInfo fPosition = typeof(NetLaneProps.Prop).
                              GetField(nameof(NetLaneProps.Prop.m_position)) ??
                              throw new Exception("fPosition is null");
        static FieldInfo fX = typeof(Vector3).GetField("x") ?? throw new Exception("fX is null");
        static FieldInfo fY = typeof(Vector3).GetField("y") ?? throw new Exception("fY is null");

        static MethodInfo mPosition =
            typeof(Bezier3)
            .GetMethod(nameof(Bezier3.Position), BindingFlags.Public | BindingFlags.Instance)
            ?? throw new Exception("mPosition is null");

        public static IEnumerable<CodeInstruction> Patch(IEnumerable<CodeInstruction> instructions, MethodInfo method)
        {
            try
            {
                var codes = instructions.ToCodeList();
                bool predicate(int i)
                {
                    if (i + 2 >= codes.Count) return false;
                    var c0 = codes[i];
                    var c1 = codes[i + 1];
                    var c2 = codes[i + 2];
                    bool ret = c0.operand == fPosition;
                    ret &= c1.operand == fX || c1.operand == fY;
                    ret &= c2.opcode == OpCodes.Mul || c2.opcode == OpCodes.Add; // ignore if(pos.x != 0)
                    return ret;
                }

                int index = 0;
                int nInsertions = 0;
                for (int watchdog = 0; ; ++watchdog)
                {
                    Assert(watchdog < 20, "watchdog");
                    int c = 1;//watchdog == 0 ? 1 : 2; // skip over m_position from previous loop.
                    index = SearchGeneric(codes, predicate, index, throwOnError: false, counter: c);
                    if (index < 0) break; // not found
                    index++; // insert after
                    bool inserted = InsertCall(codes, index, method);
                    if (inserted) nInsertions++;
                }
                return codes;
            }
            catch (Exception e)
            {
                Mod.Logger.Error(e);
                return instructions;
            }
        }

        public static bool InsertCall(List<CodeInstruction> codes, int index, MethodInfo method)
        {
            CodeInstruction LDLaneID = GetLDArg(method, "laneID");
            CodeInstruction LDLaneInfo = GetLDArg(method, "laneInfo");
            CodeInstruction LDOffset = GetLDOffset(codes, index);

            if (LDOffset == null)
                return false; // silently return if no offset could be found.

            var insertion = new CodeInstruction[] {
                LDOffset,
                LDLaneID,
                LDLaneInfo,
                new CodeInstruction(OpCodes.Call, mCalculatePropPos)
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
            // find bezier.Position(offset);
            index = SearchGeneric(codes, i => codes[i].operand == mPosition, index: index, dir: -1, throwOnError: false);
            index--; // previous instructions should put offset into stack

            if (index < 0) // not found
                return null;

            // assuming offset is stored somewhere as opposed to calcualted on the spot.
            Assert(codes[index].IsLdloc(), $"{codes[index]}.IsLdloc()");
            return new CodeInstruction(codes[index].opcode, codes[index].operand);
        }
    }
}
