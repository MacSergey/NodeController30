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
            if (vehicleData.GetCurrentPathPos(out var pathPos))
                return GetCurrentTwist(pathPos, vehicleData.m_lastPathOffset * (1f / 255f), ref vehicleData);
            else
                return Quaternion.identity;
        }
        private static Quaternion GetTrailerTwist(ref Vehicle vehicleData)
        {
            if (vehicleData.Info.m_leanMultiplier < 0)
                return Quaternion.identity; // motor cycle.

            ref var leadingVehicle = ref VehicleManager.instance.m_vehicles.m_buffer[vehicleData.m_leadingVehicle];
            var leadningInfo = leadingVehicle.Info;
            if (!leadingVehicle.GetCurrentPathPos(out var pathPos))
                return Quaternion.identity;

            var laneID = PathManager.GetLaneID(pathPos);

            // Calculate trailer lane offset based on how far the trailer is from the car its attached to.
            var inverted = leadingVehicle.m_flags.IsFlagSet(Vehicle.Flags.Inverted);
            var deltaPos = inverted ? leadningInfo.m_attachOffsetBack : leadningInfo.m_attachOffsetFront;
            var deltaOffset = deltaPos / laneID.GetLane().m_length;
            var offset = leadingVehicle.m_lastPathOffset * (1f / 255f) - deltaOffset;
            offset = Mathf.Clamp(offset, 0, 1);
            if (float.IsNaN(offset))
                return Quaternion.identity;

            return GetCurrentTwist(pathPos, offset, ref vehicleData);
        }

        private static bool GetCurrentPathPos(this ref Vehicle vehicleData, out PathUnit.Position pathPos)
        {
            var pathIndex = vehicleData.m_pathPositionIndex;
            if (pathIndex == 255)
                pathIndex = 0;

            return Singleton<PathManager>.instance.m_pathUnits.m_buffer[vehicleData.m_path].GetPosition(pathIndex >> 1, out pathPos);
        }

        private static Quaternion GetCurrentTwist(PathUnit.Position pathPos, float offset, ref Vehicle vehicleData)
        {
            if (float.IsNaN(offset) || float.IsInfinity(offset))
                return Quaternion.identity;

            var segment = pathPos.m_segment.GetSegment();
            if (segment.Info.m_lanes[pathPos.m_lane] is not NetInfo.Lane lane)
                return Quaternion.identity;

            SingletonManager<Manager>.Instance.GetSegmentData(pathPos.m_segment, out var start, out var end);
            var startTwist = start?.VehicleTwist ?? 0f;
            var endTwist = end?.VehicleTwist ?? 0f;
            var twist = startTwist * (1 - offset) - endTwist * offset;

            var isInvert = segment.IsInvert();
            var isBackward = lane.m_finalDirection == NetInfo.Direction.Backward;
            var isReversed = vehicleData.m_flags.IsFlagSet(Vehicle.Flags.Reversed);
            var isAvoid = lane.m_finalDirection == NetInfo.Direction.AvoidForward | lane.m_finalDirection == NetInfo.Direction.AvoidBackward;

            if (isInvert ^ isBackward ^ (isReversed & !isAvoid))
                twist = -twist;

            return Quaternion.Euler(0, 0f, twist);
        }
    }
}

