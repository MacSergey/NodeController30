namespace NodeController.Patches
{
    using NodeController;
    using KianCommons.Patches;

    public static class HideCrosswalksPatches
    {
        public static bool ShouldHideCrossingPrefix(ushort nodeID, ushort segmentID, ref bool __result)
        {
            var data = SegmentEndManager.Instance.GetAt(segmentID, nodeID);
            return PrefixUtils.HandleTernaryBool(data?.ShouldHideCrossingTexture(), ref __result);
        }
    }
}