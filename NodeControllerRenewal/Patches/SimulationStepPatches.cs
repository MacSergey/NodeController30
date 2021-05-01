namespace NodeController.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using ModsCommon;
    using ModsCommon.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;


    public static class SimulationStepPatches
    {
        private delegate Quaternion GetDelegate(ref Vehicle vehicleData);

        public static IEnumerable<CodeInstruction> SimulationStepTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original) => SimulationStepTranspilerBase(instructions, original, GetTwist);
        public static IEnumerable<CodeInstruction> SimulationStepTrailerTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original) => SimulationStepTranspilerBase(instructions, original, GetTrailerTwist);
        private static IEnumerable<CodeInstruction> SimulationStepTranspilerBase(IEnumerable<CodeInstruction> instructions, MethodBase original, GetDelegate getDelegate)
        {
            var rotationField = AccessTools.Field(typeof(Vehicle.Frame), nameof(Vehicle.Frame.m_rotation));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Stfld && instruction.operand == rotationField)
                {
                    yield return new CodeInstruction(original.GetLDArg("vehicleData"));
                    yield return new CodeInstruction(OpCodes.Call, getDelegate.Method);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Quaternion), "op_Multiply", new Type[] { typeof(Quaternion), typeof(Quaternion) }));
                }
                yield return instruction;
            }
        }

        private static Quaternion GetTwist(ref Vehicle vehicleData)
        {
            if (vehicleData.GetPathPosition(out var prevPos, out var nextPos))
                return vehicleData.GetCurrentTwist(prevPos, nextPos, vehicleData.m_lastPathOffset / 255f);
            else
                return Quaternion.identity;
        }
        private static Quaternion GetTrailerTwist(ref Vehicle vehicleData)
        {
            if (vehicleData.Info.m_leanMultiplier < 0)
                return Quaternion.identity; // motor cycle.

            var leadingVehicle = VehicleManager.instance.m_vehicles.m_buffer[vehicleData.m_leadingVehicle];
            var leadningInfo = leadingVehicle.Info;
            leadingVehicle.GetPathPosition(out var prevPos, out var nextPos);

            var laneID = PathManager.GetLaneID(prevPos);

            var deltaPos = leadingVehicle.m_flags.IsFlagSet(Vehicle.Flags.Inverted) ? leadningInfo.m_attachOffsetBack : leadningInfo.m_attachOffsetFront;
            var deltaOffset = deltaPos / laneID.GetLane().m_length;
            var offset = leadingVehicle.m_lastPathOffset / 255f + (prevPos.m_offset == 0 ? 1 : -1) * deltaOffset;
            offset = Mathf.Clamp01(offset);

            return leadingVehicle.GetCurrentTwist(prevPos, nextPos, offset);
        }

        private static bool GetPathPosition(this ref Vehicle vehicleData, out PathUnit.Position prevPos, out PathUnit.Position nextPos)
        {
            var buffer = Singleton<PathManager>.instance.m_pathUnits.m_buffer;
            var result = true;
            result &= buffer[vehicleData.m_path].GetPosition(vehicleData.m_pathPositionIndex >> 1, out prevPos);
            result &= buffer[vehicleData.m_path].GetNextPosition(vehicleData.m_pathPositionIndex >> 1, out nextPos);
            return result;
        }
        private static Quaternion GetCurrentTwist(this ref Vehicle vehicleData, PathUnit.Position prevPos, PathUnit.Position nextPos, float t)
        {
            if (float.IsNaN(t) || float.IsInfinity(t))
                return Quaternion.identity;

            if (vehicleData.m_pathPositionIndex % 2 == 0)
            {
                SingletonManager<Manager>.Instance.GetSegmentData(prevPos.m_segment, out var start, out var end);
                var startTwist = start?.VehicleTwist ?? 0f;
                var endTwist = end?.VehicleTwist ?? 0f;
                var twist = Mathf.Lerp(startTwist, -endTwist, t);

                return Quaternion.Euler(0, 0f, prevPos.m_offset == byte.MaxValue ? twist : -twist);
            }
            else
            {
                SingletonManager<Manager>.Instance.GetSegmentData(prevPos.m_segment, out var prevStart, out var prevEnd);
                SingletonManager<Manager>.Instance.GetSegmentData(nextPos.m_segment, out var nextStart, out var nextEnd);

                var startTwist = (prevPos.m_offset == byte.MinValue ? prevStart : prevEnd)?.VehicleTwist ?? 0f;
                var endTwist = (nextPos.m_offset == byte.MaxValue ? nextStart : nextEnd)?.VehicleTwist ?? 0f;
                var twist = Mathf.Lerp(-startTwist, endTwist, t);

                return Quaternion.Euler(0, 0f, twist);
            }
        }
    }
}

