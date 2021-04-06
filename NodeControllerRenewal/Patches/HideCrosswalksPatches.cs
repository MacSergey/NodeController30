namespace NodeController.Patches
{
    using KianCommons;

    public static class HideCrosswalksPatches
    {
        public static bool ShouldHideCrossingPrefix(ushort nodeID, ushort segmentID, ref bool __result)
        {
            var data = SegmentEndManager.Instance.GetAt(segmentID, nodeID);
            return HelpersExtensions.HandleNullBool(data?.ShouldHideCrossingTexture, ref __result);
        }
    }
}