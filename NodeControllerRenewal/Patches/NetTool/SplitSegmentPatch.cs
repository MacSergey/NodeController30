namespace NodeController.Patches._NetTool
{
    using HarmonyLib;
    using NodeController.LifeCycle;
    using static KianCommons.HelpersExtensions;
    using System;
    using NodeController30;

    [HarmonyPatch(typeof(global::NetTool), "SplitSegment")]
    public class SplitSegmentPatch
    {
        internal static MoveItSegmentData SegmentData3 { get; set; } // by move middle node
        internal static MoveItSegmentData SegmentData2 { get; set; } // by move middle node
        internal static MoveItSegmentData SegmentData { get; private set; }
        internal static bool CopyData => SegmentData != null || SegmentData2 != null || SegmentData3 != null;

        public static void Prefix(ushort segment)
        {
            if (!InSimulationThread()) 
                return;

            Mod.Logger.Debug($"SplitSegment.Prefix() segment:{segment}");
            SegmentData = MoveItIntegration.CopySegment(segment);
        }

        public static void Postfix()
        {
            if (!InSimulationThread()) 
                return;

            Mod.Logger.Debug($"SplitSegment.Postfix()");
            SegmentData = SegmentData2 = SegmentData3 = null;
        }
    }
}
