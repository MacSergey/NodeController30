namespace NodeController.Patches
{
    using HarmonyLib;
    using JetBrains.Annotations;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;
    using static KianCommons.ReflectionHelpers;

    [UsedImplicitly]
    [HarmonyPatch]
    static class FindDirectionPatch
    {
        [UsedImplicitly]
        static MethodBase TargetMethod() =>
            GetMethod(typeof(NetSegment), nameof(NetSegment.FindDirection));

        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return FlatJunctionsCommons.ModifyFlatJunctionsTranspiler(instructions, original);
        }
    }
}
