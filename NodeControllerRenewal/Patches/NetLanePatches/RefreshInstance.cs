namespace NodeController.Patches.NetLanePatches
{
    using HarmonyLib;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    [HarmonyPatch]
    public static class RefreshInstance
    {
        static MethodInfo Target = typeof(NetLane).GetMethod(nameof(NetLane.RefreshInstance), BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() => Target;

        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) => PropDisplacementCommons.Patch(instructions, Target);
    }
}