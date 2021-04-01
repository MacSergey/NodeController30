using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace NodeController.Patches.NetNodePatches
{
    using KianCommons;
    using KianCommons.Patches;
    using NodeController30;

    [HarmonyPatch()]
    public static class RenderInstance
    {
        static void Log(string m) => Mod.Logger.Debug("NetNode_RenderInstance Transpiler: " + m);

        static MethodInfo Target => typeof(global::NetNode).GetMethod("RenderInstance", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodBase TargetMethod()
        {
            var ret = Target;
            Assertion.Assert(ret != null, "did not manage to find original function to patch");
            Log("aquired method " + ret);
            return ret;
        }

        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var codes = TranspilerUtils.ToCodeList(instructions);
                CalculateMaterialCommons.PatchCheckFlags(codes, occurance: 2, Target);

                Log("successfully patched NetNode.RenderInstance");
                return codes;
            }
            catch (Exception e)
            {
                Mod.Logger.Error(e);
                throw e;
            }
        }
    }
}