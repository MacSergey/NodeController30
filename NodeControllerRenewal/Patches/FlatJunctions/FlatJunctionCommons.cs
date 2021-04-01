namespace NodeController.Patches
{
    using HarmonyLib;
    using JetBrains.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using KianCommons;
    using static KianCommons.Assertion;
    using static KianCommons.Patches.TranspilerUtils;
    using NodeController30;

    static class FlatJunctionsCommons
    {
        internal static bool GetFlatJunctions(bool flatJunctions0, ushort segmentID, ushort nodeID)
        {
            var data = SegmentEndManager.Instance.GetAt(segmentID, nodeID);
            return data?.FlatJunctions ?? flatJunctions0;
        }

        static FieldInfo f_flatJunctions =
            typeof(NetInfo).GetField(nameof(NetInfo.m_flatJunctions)) ??
            throw new Exception("f_flatJunctions is null");

        static MethodInfo mGetFlatJunctions = ReflectionHelpers.GetMethod(
            typeof(FlatJunctionsCommons), nameof(GetFlatJunctions));

        public static IEnumerable<CodeInstruction> ModifyFlatJunctionsTranspiler(
            IEnumerable<CodeInstruction> instructions,
            MethodBase targetMethod)
        {
            AssertNotNull(targetMethod, "targetMethod");
            CodeInstruction ldarg_nodeID =
                GetLDArg(targetMethod, "startNodeID", throwOnError: false) // CalculateCorner
                ?? GetLDArg(targetMethod, "nodeID"); // FindDirection
            CodeInstruction ldarg_segmentID =
                GetLDArg(targetMethod, "ignoreSegmentID", throwOnError: false) // CalculateCorner
                ?? GetLDArg(targetMethod, "segmentID"); // FindDirection
            AssertNotNull(ldarg_nodeID, "ldarg_nodeID");

            CodeInstruction call_GetFlatJunctions = new CodeInstruction(OpCodes.Call, mGetFlatJunctions);
            Mod.Logger.Debug("ldarg_nodeID=" + ldarg_nodeID);

            int n = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                bool is_ldfld_flatJunctions =
                    instruction.opcode == OpCodes.Ldfld && instruction.operand == f_flatJunctions;
                if (is_ldfld_flatJunctions)
                {
                    n++;
                    yield return ldarg_segmentID;
                    yield return ldarg_nodeID;
                    yield return call_GetFlatJunctions;
                }
            }

            Mod.Logger.Debug($"TRANSPILER FlatJunctionsCommons: Successfully patched {targetMethod}. " +
                $"found {n} instances of Ldfld NetInfo.m_flatJunctions");
            yield break;
        }
    }
}
