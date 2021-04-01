namespace NodeController.Patches.HideCrosswalksMod
{
    using System.Reflection;
    using NodeController;
    using KianCommons.Patches;
    using HarmonyLib;
    using KianCommons;
    using KianCommons.Plugins;

    [HarmonyPatch]
    public static class ShouldHideCrossing
    {
        static bool Prepare() => PluginUtil.GetHideCrossings().IsActive();

        public static MethodBase TargetMethod()
        {
            return typeof(HideCrosswalks.Patches.CalculateMaterialCommons).GetMethod(nameof(HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing));
        }

        public static bool Prefix(ushort nodeID, ushort segmentID, ref bool __result)
        {
            var data = SegmentEndManager.Instance.GetAt(segmentID, nodeID);
            return PrefixUtils.HandleTernaryBool(data?.ShouldHideCrossingTexture(), ref __result);
        }
    }
}