namespace NodeController.Patches
{
    using ColossalFramework;
    using HarmonyLib;
    using KianCommons;
    using ModsCommon;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;


    public static class SimulationStepPatches
    {
        internal static bool RotationUpdated { get; set; } = false;
        internal static void OnRotationUpdated() => RotationUpdated = true;
        static PathUnit[] PathUnitBuffer => Singleton<PathManager>.instance.m_pathUnits.m_buffer;

        public static IEnumerable<CodeInstruction> TranspilerBase(IEnumerable<CodeInstruction> instructions, MethodInfo targetMethod)
        {
            var call_OnRotationUpdated = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SimulationStepPatches), nameof(OnRotationUpdated)));
            var rotationField = AccessTools.Field(typeof(Vehicle.Frame), nameof(Vehicle.Frame.m_rotation));

            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Stfld && instruction.operand == rotationField)
                    yield return call_OnRotationUpdated;
            }
            yield break;
        }

        private static void PostfixBase(ref Vehicle vehicleData, ref Vehicle.Frame frameData)
        {
            if (!vehicleData.GetCurrentPathPos(out var pathPos))
                return;
            try
            {
                float se = GetCurrentSE(pathPos, vehicleData.m_lastPathOffset * (1f / 255f), ref vehicleData);

                var rot = Quaternion.Euler(0, 0f, se);
                frameData.m_rotation *= rot;
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error(pathPos.ToSTR(), error);
            }
        }

        public static void CarAISimulationStepPostfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData)
        {
            if (RotationUpdated)
            {
                RotationUpdated = false;
                PostfixBase(ref vehicleData, ref frameData);
            }
        }
        public static IEnumerable<CodeInstruction> CarAISimulationStepTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase method) => TranspilerBase(instructions, method as MethodInfo);

        static Vehicle[] VehicleBuffer { get; } = VehicleManager.instance.m_vehicles.m_buffer;
        public static void CarTrailerAISimulationStepPostfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData)
        {
            if (vehicleData.Info.m_leanMultiplier < 0)
                return; // motor cycle.

            if (!RotationUpdated)
                return;

            RotationUpdated = false;

            ref Vehicle leadingVehicle = ref VehicleBuffer[vehicleData.m_leadingVehicle];
            VehicleInfo leadningInfo = leadingVehicle.Info;
            if (!leadingVehicle.GetCurrentPathPos(out var pathPos))
                return;

            uint laneID = PathManager.GetLaneID(pathPos);

            // Calculate trailer lane offset based on how far the trailer is from the car its attached to.
            bool inverted = leadingVehicle.m_flags.IsFlagSet(Vehicle.Flags.Inverted);
            float deltaPos = inverted ? leadningInfo.m_attachOffsetBack : leadningInfo.m_attachOffsetFront;
            float deltaOffset = deltaPos / laneID.ToLane().m_length;
            float offset = leadingVehicle.m_lastPathOffset * (1f / 255f) - deltaOffset;
            offset = Mathf.Clamp(offset, 0, 1);
            if (float.IsNaN(offset))
                return;

            float se = GetCurrentSE(pathPos, offset, ref vehicleData);
            var rot = Quaternion.Euler(0, 0f, se);
            frameData.m_rotation *= rot;
        }
        public static IEnumerable<CodeInstruction> CarTrailerAISimulationStepTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase method) => TranspilerBase(instructions, method as MethodInfo);

        public static void TrainAISimulationStepPostfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) => PostfixBase(ref vehicleData, ref frameData);
        public static void TramBaseAISimulationStepPostfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) => PostfixBase(ref vehicleData, ref frameData);

        static string ToSTR(this ref PathUnit.Position pathPos)
        {
            var info = pathPos.m_segment.ToSegment().Info;
            return $"segment:{pathPos.m_segment} info:{info} nLanes={info.m_lanes.Length} laneIndex={pathPos.m_lane}";
        }

        internal static bool GetCurrentPathPos(this ref Vehicle vehicleData, out PathUnit.Position pathPos)
        {
            byte pathIndex = vehicleData.m_pathPositionIndex;
            if (pathIndex == 255)
                pathIndex = 0;
            return PathUnitBuffer[vehicleData.m_path].GetPosition(pathIndex >> 1, out pathPos);
        }

        internal static NetInfo.Lane GetLaneInfo(this ref PathUnit.Position pathPos) => pathPos.m_segment.ToSegment().Info.m_lanes[pathPos.m_lane];

        internal static float GetCurrentSE(PathUnit.Position pathPos, float offset, ref Vehicle vehicleData)
        {
            if (float.IsNaN(offset) || float.IsInfinity(offset))
                return 0;

            SegmentEndData segStart = SegmentEndManager.Instance[pathPos.m_segment, true];
            SegmentEndData segEnd = SegmentEndManager.Instance[pathPos.m_segment, false];
            float startSE = segStart == null ? 0f : segStart.CachedSuperElevationDeg;
            float endSE = segEnd == null ? 0f : -segEnd.CachedSuperElevationDeg;
            float se = startSE * (1 - offset) + endSE * offset;

            if (pathPos.GetLaneInfo() is not NetInfo.Lane lane)
                return 0;

            bool invert = pathPos.m_segment.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            bool backward = lane.m_finalDirection == NetInfo.Direction.Backward;
            bool reversed = vehicleData.m_flags.IsFlagSet(Vehicle.Flags.Reversed);

            bool avoidForward = lane.m_finalDirection == NetInfo.Direction.AvoidForward;
            bool avoidBackward = lane.m_finalDirection == NetInfo.Direction.AvoidBackward;
            bool avoid = avoidForward | avoidBackward;

            if (invert)
                se = -se;
            if (backward)
                se = -se;
            if (reversed & !avoid)
                se = -se;

            return se;
        }

        #region ROTATION UPDATED


        #endregion
    }
}

