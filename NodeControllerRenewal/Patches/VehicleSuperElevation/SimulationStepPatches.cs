namespace NodeController.Patches.VehicleSuperElevation
{
    using ColossalFramework;
    using HarmonyLib;
    using KianCommons;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;
    using static SuperElevationCommons;

    [HarmonyPatch]
    static class CarAI_SimulationStepPatch
    {
        internal static MethodBase TargetMethod() => TargetMethod<CarAI>();

        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) =>
            OnRotationUpdatedTranspiler(instructions, TargetMethod() as MethodInfo);

        internal static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData)
        {
            if (!RotationUpdated) return;
            RotationUpdated = false;
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
        }
    }

    [HarmonyPatch]
    static class CarTrailerAI_SimulationStepPatch
    {
        internal static MethodBase TargetMethod() => TargetMethod<CarTrailerAI>();

        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) =>
            OnRotationUpdatedTranspiler(instructions, TargetMethod() as MethodInfo);

        static Vehicle[] VehicleBuffer = VehicleManager.instance.m_vehicles.m_buffer;
        internal static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData)
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
    }

    [HarmonyPatch]
    static class TrainAI_SimulationStepPatch
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            yield return TargetMethod<TrainAI>();
            var tmpeTarget = TargetTMPEMethod<TrainAI>();
            if (tmpeTarget != null)
                yield return tmpeTarget; //old TMPE
        }

        internal static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) =>
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
    }

    [HarmonyPatch]
    static class TramBaseAI_SimulationStepPatch
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            yield return TargetMethod<TramBaseAI>();
            var tmpeTarget = TargetTMPEMethod<TramBaseAI>();
            if (tmpeTarget != null)
                yield return tmpeTarget; //old TMPE
        }

        internal static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) => SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
    }
}

